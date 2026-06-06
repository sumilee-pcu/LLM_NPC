#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LLMNpc.EditorTools
{
    // 캐릭터 표정 5종(neutral/happy/sad/angry/shy)을 PortraitController 에 자동 할당.
    // 메뉴: Tools > LLM NPC > Assign Yuna Portraits
    // 전제: Assets/Art/Characters/yuna/{emotion}.png 가 존재 (Sutemo PSD에서 추출).
    public static class PortraitAssigner
    {
        const string Dir = "Assets/Art/Characters/yuna/";
        static readonly string[] Emotions = { "neutral", "happy", "sad", "angry", "shy" };

        [MenuItem("Tools/LLM NPC/Assign Yuna Portraits")]
        public static void Assign()
        {
            // 1) PNG 들을 Sprite 타입으로 보장
            foreach (var e in Emotions)
            {
                string path = Dir + e + ".png";
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) { Debug.LogError($"[PortraitAssigner] 파일 없음: {path}"); return; }
                if (imp.textureType != TextureImporterType.Sprite)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.SaveAndReimport();
                }
            }

            // 2) 씬의 PortraitController 찾기
            var pc = Object.FindFirstObjectByType<PortraitController>();
            if (pc == null)
            {
                Debug.LogError("[PortraitAssigner] PortraitController 없음. 먼저 Build Demo Scene 실행.");
                return;
            }

            // 3) 표정 슬롯 배선
            var so = new SerializedObject(pc);
            foreach (var e in Emotions)
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(Dir + e + ".png");
                var prop = so.FindProperty(e);
                if (prop != null) prop.objectReferenceValue = sp;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            // 4) Portrait Image: 플레이스홀더 → 실제 캐릭터(neutral, 흰색, 비율 유지)
            var img = pc.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Dir + "neutral.png");
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
                img.color = Color.white;
                EditorUtility.SetDirty(img);
            }
            EditorUtility.SetDirty(pc);

            // 5) 플레이스홀더 라벨 숨기기 (있으면)
            foreach (var t in Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
            {
                if (t.text != null && t.text.Contains("캐릭터 이미지"))
                { t.gameObject.SetActive(false); EditorUtility.SetDirty(t); }
            }

            EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[PortraitAssigner] 유나 표정 5종 할당 완료. ▶ Play 후 대화하면 표정이 바뀝니다.");
        }
    }
}
#endif
