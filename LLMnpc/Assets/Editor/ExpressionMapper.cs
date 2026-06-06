#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LLMNpc.EditorTools
{
    // 표정 매핑 도구 (에디터 창).
    // 메뉴: Tools > LLM NPC > Expression Mapper
    // 게임 감정 5종(neutral/happy/sad/angry/shy)에 PSD 표정 11종 중 원하는 것을
    // 드롭다운으로 골라 PortraitController 에 적용한다. 코드 수정 불필요.
    public class ExpressionMapper : EditorWindow
    {
        const string ExprDir = "Assets/Art/Characters/yuna/expr";
        static readonly string[] Emotions = { "neutral", "happy", "sad", "angry", "shy" };
        static readonly Dictionary<string, string> Defaults = new Dictionary<string, string>
        {
            { "neutral", "normal" }, { "happy", "Delighted" }, { "sad", "Sad" },
            { "angry", "Angry" }, { "shy", "Smile_2" },
        };

        string[] options;
        int[] selected;

        [MenuItem("Tools/LLM NPC/Expression Mapper")]
        static void Open()
        {
            var w = GetWindow<ExpressionMapper>("표정 매핑");
            w.minSize = new Vector2(560, 360);
        }

        void OnEnable() { Refresh(); }
        void OnFocus() { Refresh(); Repaint(); }   // 창에 포커스될 때마다 현재 상태로 갱신

        void Refresh()
        {
            options = AssetDatabase.FindAssets("t:Sprite", new[] { ExprDir })
                .Select(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)))
                .Distinct().OrderBy(x => x).ToArray();

            selected = new int[Emotions.Length];
            var pc = Object.FindFirstObjectByType<PortraitController>();
            for (int i = 0; i < Emotions.Length; i++)
            {
                string want = Defaults.TryGetValue(Emotions[i], out var d) ? d
                              : (options.Length > 0 ? options[0] : "");
                if (pc != null)
                {
                    var so = new SerializedObject(pc);
                    var p = so.FindProperty(Emotions[i]);
                    if (p != null && p.objectReferenceValue is Sprite sp) want = sp.name;
                }
                int idx = System.Array.IndexOf(options, want);
                selected[i] = idx < 0 ? 0 : idx;
            }
        }

        void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("게임 감정 → PSD 표정 매핑", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("드롭다운으로 표정을 고르고 [씬에 적용]을 누르세요. Play 중 대화하면 emotion 값에 따라 이 매핑대로 표정이 바뀝니다.", MessageType.Info);

            if (options == null || options.Length == 0)
            {
                EditorGUILayout.HelpBox($"표정 스프라이트가 없습니다: {ExprDir}/", MessageType.Warning);
                if (GUILayout.Button("새로고침")) Refresh();
                return;
            }

            EditorGUILayout.Space(4);
            for (int i = 0; i < Emotions.Length; i++)
                selected[i] = EditorGUILayout.Popup(Emotions[i], selected[i], options);

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = new Color(0.6f, 0.85f, 1f);
                if (GUILayout.Button("씬에 적용", GUILayout.Height(30))) Apply();
                GUI.backgroundColor = Color.white;
                if (GUILayout.Button("기본값", GUILayout.Height(30))) ResetDefaults();
                if (GUILayout.Button("새로고침", GUILayout.Height(30))) Refresh();
            }

            // 미리보기
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("미리보기", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < Emotions.Length; i++)
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(100)))
                    {
                        EditorGUILayout.LabelField(Emotions[i], EditorStyles.miniBoldLabel);
                        var r = GUILayoutUtility.GetRect(90, 90);
                        var sp = LoadSprite(options[selected[i]]);
                        if (sp != null)
                        {
                            var tex = AssetPreview.GetAssetPreview(sp);
                            if (tex == null) tex = sp.texture;
                            if (tex != null) GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);
                        }
                    }
                }
            }
        }

        void ResetDefaults()
        {
            for (int i = 0; i < Emotions.Length; i++)
            {
                int idx = System.Array.IndexOf(options, Defaults[Emotions[i]]);
                selected[i] = idx < 0 ? 0 : idx;
            }
        }

        Sprite LoadSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{ExprDir}/{name}.png");

        void Apply()
        {
            var pc = Object.FindFirstObjectByType<PortraitController>();
            if (pc == null)
            {
                Debug.LogError("[ExpressionMapper] PortraitController 없음. Tools > LLM NPC > Build Demo Scene 먼저 실행.");
                return;
            }

            // PNG 들이 Sprite 타입인지 보장
            foreach (var n in options.Distinct())
            {
                var path = $"{ExprDir}/{n}.png";
                if (AssetImporter.GetAtPath(path) is TextureImporter imp && imp.textureType != TextureImporterType.Sprite)
                { imp.textureType = TextureImporterType.Sprite; imp.SaveAndReimport(); }
            }

            var so = new SerializedObject(pc);
            for (int i = 0; i < Emotions.Length; i++)
            {
                var sp = LoadSprite(options[selected[i]]);
                var p = so.FindProperty(Emotions[i]);
                if (p != null) p.objectReferenceValue = sp;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            var img = pc.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = LoadSprite(options[selected[0]]); // neutral
                img.type = Image.Type.Simple; img.preserveAspect = true; img.color = Color.white;
                EditorUtility.SetDirty(img);
            }
            EditorUtility.SetDirty(pc);

            foreach (var t in Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
                if (t.text != null && t.text.Contains("캐릭터 이미지"))
                { t.gameObject.SetActive(false); EditorUtility.SetDirty(t); }

            EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[ExpressionMapper] 표정 매핑 적용 완료. ▶ Play 후 대화로 확인하세요.");
        }
    }
}
#endif
