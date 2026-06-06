#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LLMNpc.EditorTools
{
    // 표정 PNG 가 들어오면 자동으로 Sprite 설정 + PortraitController 매핑.
    // 학생 흐름: Sutemo PSD 다운로드 → python tools/extract_yuna.py 실행 →
    //           expr/*.png 생성 → Unity 가 import 하며 이 후처리기가 자동 매핑.
    public class YunaArtPostprocessor : AssetPostprocessor
    {
        const string ExprDir = "Assets/Art/Characters/yuna/expr/";

        // 게임 감정 → PSD 표정(파일명) 기본 매핑
        static readonly (string emo, string file)[] Map =
        {
            ("neutral", "normal"), ("happy", "Delighted"), ("sad", "Sad"),
            ("angry", "Angry"), ("shy", "Smile_2"),
        };

        // 1) expr 폴더로 들어오는 텍스처는 Sprite 로 강제
        void OnPreprocessTexture()
        {
            if (!Norm(assetPath).StartsWith(ExprDir)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
        }

        // 2) import 완료 후, 표정 PNG 가 들어왔고 씬에 PortraitController 가 있으면 자동 매핑
        static void OnPostprocessAllAssets(string[] imported, string[] deleted,
                                           string[] moved, string[] movedFrom)
        {
            bool touched = imported.Any(p => Norm(p).StartsWith(ExprDir) && p.EndsWith(".png"));
            if (!touched) return;

            // 기본 매핑 5종이 모두 존재할 때만 적용
            foreach (var m in Map)
                if (Load(m.file) == null) return;

            var pc = Object.FindFirstObjectByType<PortraitController>();
            if (pc == null) return; // 씬에 없으면 조용히 패스 (Expression Mapper 로 수동 적용 가능)

            var so = new SerializedObject(pc);
            foreach (var m in Map)
            {
                var p = so.FindProperty(m.emo);
                if (p != null) p.objectReferenceValue = Load(m.file);
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            var img = pc.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = Load("normal");
                img.type = Image.Type.Simple; img.preserveAspect = true; img.color = Color.white;
                EditorUtility.SetDirty(img);
            }
            EditorUtility.SetDirty(pc);

            foreach (var t in Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
                if (t.text != null && t.text.Contains("캐릭터 이미지"))
                { t.gameObject.SetActive(false); EditorUtility.SetDirty(t); }

            Debug.Log("[YunaArtPostprocessor] 표정 PNG 감지 → PortraitController 자동 매핑 완료. (씬 저장: Cmd/Ctrl+S)");
        }

        static string Norm(string p) => p.Replace('\\', '/');
        static Sprite Load(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(ExprDir + file + ".png");
    }
}
#endif
