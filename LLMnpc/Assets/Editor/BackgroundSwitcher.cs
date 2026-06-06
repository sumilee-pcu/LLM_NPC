#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LLMNpc.EditorTools
{
    // 배경 전환 도구.  메뉴: Tools > LLM NPC > Background > (Campus/Classroom/Park/Gradient)
    // 씬의 Background 이미지 스프라이트를 교체한다. 배경 PNG는 자체 생성물(Art/Generated).
    public static class BackgroundSwitcher
    {
        const string Dir = "Assets/Art/Generated/";

        [MenuItem("Tools/LLM NPC/Background/Campus (캠퍼스)")]    static void Campus()    => Set("bg_campus");
        [MenuItem("Tools/LLM NPC/Background/Classroom (교실)")]  static void Classroom() => Set("bg_classroom");
        [MenuItem("Tools/LLM NPC/Background/Park (공원)")]       static void Park()      => Set("bg_park");
        [MenuItem("Tools/LLM NPC/Background/Sunset (노을)")]     static void Sunset()    => Set("bg_sunset");
        [MenuItem("Tools/LLM NPC/Background/Hallway (복도)")]    static void Hallway()   => Set("bg_hallway");
        [MenuItem("Tools/LLM NPC/Background/Cafe (카페)")]       static void Cafe()      => Set("bg_cafe");
        [MenuItem("Tools/LLM NPC/Background/Gradient (단색)")]   static void Gradient()  => Set("bg_gradient");

        static void Set(string name)
        {
            string path = Dir + name + ".png";
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { Debug.LogError($"[Background] 파일 없음: {path} (python tools/make_backgrounds.py 실행)"); return; }
            if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; imp.SaveAndReimport(); }

            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var bgGO = GameObject.Find("Canvas/Background");
            if (bgGO == null)
                foreach (var img in Object.FindObjectsByType<Image>(FindObjectsSortMode.None))
                    if (img.gameObject.name == "Background") { bgGO = img.gameObject; break; }
            if (bgGO == null) { Debug.LogError("[Background] Background 오브젝트 없음. Build Demo Scene 먼저 실행."); return; }

            var bg = bgGO.GetComponent<Image>();
            bg.sprite = sp; bg.type = Image.Type.Simple; bg.color = Color.white;
            EditorUtility.SetDirty(bg);
            EditorSceneManager.MarkSceneDirty(bgGO.scene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[Background] {name} 적용 완료.");
        }
    }
}
#endif
