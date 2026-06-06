#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LLMNpc.EditorTools
{
    // 씬 자동 구성기 (예쁜 버전).
    // 메뉴: Tools > LLM NPC > Build Demo Scene
    // 그라데이션 배경 + 캐릭터 자리 + 대사창 + 이름표 + 입력/전송 + 호감도 게이지를
    // 생성하고 매니저 컴포넌트까지 전부 연결한다.
    public static class SceneBuilder
    {
        // 테마 색
        static readonly Color Accent   = new Color(0.93f, 0.42f, 0.55f);        // 핑크 포인트
        static readonly Color BoxDark  = new Color(0.07f, 0.07f, 0.12f, 0.72f); // 대사창 배경
        static readonly Color PanelLt  = new Color(1f, 1f, 1f, 0.30f);          // 로그 패널
        static readonly Color GradTop  = new Color(1.00f, 0.86f, 0.80f);        // 위: 복숭아빛
        static readonly Color GradBot  = new Color(0.76f, 0.80f, 0.96f);        // 아래: 라벤더

        [MenuItem("Tools/LLM NPC/Build Demo Scene")]
        public static void BuildScene()
        {
            var res = new DefaultControls.Resources
            {
                standard   = Builtin("UI/Skin/UISprite.psd"),
                background = Builtin("UI/Skin/Background.psd"),
                inputField = Builtin("UI/Skin/InputFieldBackground.psd"),
                knob       = Builtin("UI/Skin/Knob.psd"),
                checkmark  = Builtin("UI/Skin/Checkmark.psd"),
                dropdown   = Builtin("UI/Skin/DropdownArrow.psd"),
                mask       = Builtin("UI/Skin/UIMask.psd"),
            };

            // 재실행 시 중복 방지
            foreach (var n in new[] { "Canvas", "Game", "EventSystem" })
            {
                var old = GameObject.Find(n);
                if (old != null) Object.DestroyImmediate(old);
            }

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var bgSprite = LoadBgOrGradient("bg_campus"); // 기본 배경 = 캠퍼스 (없으면 그라데이션)

            // EventSystem
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem", typeof(EventSystem));
                var newModule = System.Type.GetType(
                    "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (newModule != null) esGO.AddComponent(newModule);
                else esGO.AddComponent<StandaloneInputModule>();
            }

            // Canvas
            var canvasGO = new GameObject("Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // 배경 (그라데이션, 전체 스트레치, 맨 뒤)
            var bg = new GameObject("Background", typeof(Image));
            bg.transform.SetParent(canvasGO.transform, false);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            var bgImg = bg.GetComponent<Image>();
            bgImg.sprite = bgSprite; bgImg.type = Image.Type.Simple;
            bgImg.raycastTarget = false;

            // Portrait (캐릭터 자리) — 미연시처럼 오른쪽 사이드 배치
            var portraitGO = DefaultControls.CreateImage(res);
            portraitGO.name = "Portrait";
            Attach(portraitGO, canvasGO, new Vector2(360, 30), new Vector2(540, 700));
            var portrait = portraitGO.GetComponent<Image>();
            portrait.sprite = res.standard;
            portrait.type = Image.Type.Sliced;
            portrait.color = new Color(1f, 1f, 1f, 0.55f); // 플레이스홀더 (스프라이트 넣으면 흰색으로 바뀜)
            Label(res, canvasGO, font, "캐릭터 이미지\n(여기에 표정 스프라이트)",
                new Vector2(360, 60), new Vector2(420, 100), 24,
                new Color(0.25f, 0.25f, 0.30f), TextAnchor.MiddleCenter);

            // 로그 패널 (왼쪽, 반투명 어둡게 — 이전 대화 표시)
            Panel(res, canvasGO, new Vector2(-560, 80), new Vector2(380, 520), new Color(0f, 0f, 0f, 0.30f));
            var logText = Label(res, canvasGO, font, "", new Vector2(-560, 70), new Vector2(340, 480),
                20, new Color(0.95f, 0.95f, 0.98f), TextAnchor.LowerLeft);
            logText.gameObject.name = "LogText";

            // 호감도 라벨 + 게이지 (상단)
            Label(res, canvasGO, font, "♥ 호감도", new Vector2(-300, 462), new Vector2(180, 44),
                28, new Color(0.55f, 0.12f, 0.25f), TextAnchor.MiddleRight, FontStyle.Bold);
            var sliderGO = DefaultControls.CreateSlider(res);
            sliderGO.name = "AffectionGauge";
            Attach(sliderGO, canvasGO, new Vector2(80, 462), new Vector2(420, 34));
            var slider = sliderGO.GetComponent<Slider>();
            slider.minValue = 0; slider.maxValue = 100; slider.wholeNumbers = true; slider.value = 30;
            TintSliderFill(sliderGO, Accent);

            // 대사창 박스 (하단, 반투명 어두운 배경)
            Panel(res, canvasGO, new Vector2(0, -310), new Vector2(1140, 210), BoxDark);

            // 이름표
            Panel(res, canvasGO, new Vector2(-480, -202), new Vector2(190, 56), Accent);
            Label(res, canvasGO, font, "유나", new Vector2(-480, -202), new Vector2(190, 56),
                28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

            // 대사 텍스트 (흰색, 박스 위)
            var dialogueText = Label(res, canvasGO, font, "...", new Vector2(0, -315), new Vector2(1050, 150),
                30, Color.white, TextAnchor.UpperLeft);
            dialogueText.gameObject.name = "DialogueText";

            // 입력창
            var inputGO = DefaultControls.CreateInputField(res);
            inputGO.name = "InputField";
            Attach(inputGO, canvasGO, new Vector2(-190, -480), new Vector2(800, 84));
            var inputField = inputGO.GetComponent<InputField>();
            inputGO.GetComponent<Image>().color = Color.white;
            if (inputField.textComponent != null)
            { inputField.textComponent.font = font; inputField.textComponent.fontSize = 28;
              inputField.textComponent.color = new Color(0.1f, 0.1f, 0.1f); }
            var ph = inputField.placeholder as Text;
            if (ph != null) { ph.font = font; ph.text = "메시지를 입력하세요..."; ph.fontSize = 26;
              ph.color = new Color(0.5f, 0.5f, 0.5f); ph.fontStyle = FontStyle.Italic; }

            // 전송 버튼 (포인트색 + 흰 글씨)
            var btnGO = DefaultControls.CreateButton(res);
            btnGO.name = "SendButton";
            Attach(btnGO, canvasGO, new Vector2(370, -480), new Vector2(240, 84));
            var sendButton = btnGO.GetComponent<Button>();
            btnGO.GetComponent<Image>().color = Accent;
            var btnLabel = btnGO.GetComponentInChildren<Text>();
            if (btnLabel != null) { btnLabel.font = font; btnLabel.text = "전송";
              btnLabel.fontSize = 32; btnLabel.color = Color.white; btnLabel.fontStyle = FontStyle.Bold; }

            // 매니저
            var gameGO = new GameObject("Game");
            var dialogueUI = gameGO.AddComponent<DialogueUI>();
            var llm = gameGO.AddComponent<LLMClient>();
            var gm = gameGO.AddComponent<GameManager>();
            var portraitController = portraitGO.AddComponent<PortraitController>();
            var bgCtrl = bg.AddComponent<BackgroundController>();

            // 참조 배선
            Wire(dialogueUI, p => {
                p("inputField", inputField); p("sendButton", sendButton);
                p("dialogueText", dialogueText); p("logText", logText);
                p("affectionGauge", slider);
            });
            Wire(portraitController, p => p("portrait", portrait));
            Wire(bgCtrl, p => {
                p("background", bgImg);
                p("campus", LoadBgSprite("bg_campus"));   p("classroom", LoadBgSprite("bg_classroom"));
                p("park", LoadBgSprite("bg_park"));       p("sunset", LoadBgSprite("bg_sunset"));
                p("hallway", LoadBgSprite("bg_hallway")); p("cafe", LoadBgSprite("bg_cafe"));
            });
            Wire(gm, p => { p("ui", dialogueUI); p("portrait", portraitController);
                            p("llm", llm); p("backgroundController", bgCtrl); });

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveOpenScenes();
            Selection.activeGameObject = gameGO;
            Debug.Log("[SceneBuilder] 예쁜 씬 생성 및 저장 완료. ▶ Play 를 누르세요.");
        }

        // ---------- helpers ----------
        static Sprite Builtin(string path) => AssetDatabase.GetBuiltinExtraResource<Sprite>(path);

        static void Attach(GameObject go, GameObject parent, Vector2 pos, Vector2 size)
        {
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        static GameObject Panel(DefaultControls.Resources res, GameObject parent,
                                Vector2 pos, Vector2 size, Color color)
        {
            var go = DefaultControls.CreatePanel(res);
            go.name = "Panel";
            Attach(go, parent, pos, size);
            go.GetComponent<Image>().color = color;
            return go;
        }

        static Text Label(DefaultControls.Resources res, GameObject parent, Font font, string text,
                          Vector2 pos, Vector2 size, int fontSize, Color color,
                          TextAnchor anchor, FontStyle style = FontStyle.Normal)
        {
            var go = DefaultControls.CreateText(res);
            go.name = "Text";
            Attach(go, parent, pos, size);
            var t = go.GetComponent<Text>();
            t.font = font; t.text = text; t.fontSize = fontSize; t.color = color;
            t.alignment = anchor; t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static void TintSliderFill(GameObject sliderGO, Color color)
        {
            var fill = sliderGO.transform.Find("Fill Area/Fill");
            if (fill != null) { var img = fill.GetComponent<Image>(); if (img != null) img.color = color; }
        }

        // Art/Generated 의 배경 PNG 를 Sprite 로 로드 (없으면 null)
        static Sprite LoadBgSprite(string name)
        {
            string path = "Assets/Art/Generated/" + name + ".png";
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return null;
            if (imp.textureType != TextureImporterType.Sprite)
            { imp.textureType = TextureImporterType.Sprite; imp.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // Art/Generated 의 배경 PNG 를 Sprite 로 로드 (없으면 그라데이션 폴백)
        static Sprite LoadBgOrGradient(string name)
        {
            string path = "Assets/Art/Generated/" + name + ".png";
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                if (imp.textureType != TextureImporterType.Sprite)
                { imp.textureType = TextureImporterType.Sprite; imp.SaveAndReimport(); }
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp != null) return sp;
            }
            return MakeGradientSprite(GradTop, GradBot);
        }

        // 세로 그라데이션 PNG 를 만들어 Assets 에 저장하고 Sprite 로 반환
        static Sprite MakeGradientSprite(Color top, Color bottom)
        {
            const string dir = "Assets/Art/Generated";
            const string assetPath = dir + "/bg_gradient.png";
            if (!AssetDatabase.IsValidFolder("Assets/Art"))
                AssetDatabase.CreateFolder("Assets", "Art");
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/Art", "Generated");

            int w = 8, h = 256;
            var tex = new Texture2D(w, h);
            for (int y = 0; y < h; y++)
            {
                var c = Color.Lerp(bottom, top, y / (float)(h - 1));
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
            }
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "Art/Generated/bg_gradient.png"),
                               tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(assetPath);

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        delegate void Setter(string prop, Object value);
        static void Wire(Object target, System.Action<Setter> body)
        {
            var so = new SerializedObject(target);
            body((prop, value) =>
            {
                var p = so.FindProperty(prop);
                if (p != null) p.objectReferenceValue = value;
            });
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
