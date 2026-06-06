#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LLMNpc.EditorTools
{
    // 씬 자동 구성기.
    // Unity 상단 메뉴: Tools > LLM NPC > Build Demo Scene 클릭 한 번으로
    // Canvas + UI(Legacy) + 매니저 + 컴포넌트 연결까지 전부 생성/배선한다.
    public static class SceneBuilder
    {
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

            // EventSystem (신규 Input System 이면 InputSystemUIInputModule, 아니면 Standalone)
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

            // Portrait
            var portraitGO = DefaultControls.CreateImage(res);
            portraitGO.name = "Portrait";
            Attach(portraitGO, canvasGO, new Vector2(0, 120), new Vector2(500, 600));
            var portrait = portraitGO.GetComponent<Image>();

            // Dialogue text
            var dialogueGO = DefaultControls.CreateText(res);
            dialogueGO.name = "DialogueText";
            Attach(dialogueGO, canvasGO, new Vector2(0, -260), new Vector2(900, 120));
            var dialogueText = dialogueGO.GetComponent<Text>();
            dialogueText.text = "..."; dialogueText.fontSize = 28;
            dialogueText.color = Color.black; dialogueText.alignment = TextAnchor.UpperLeft;

            // Log text
            var logGO = DefaultControls.CreateText(res);
            logGO.name = "LogText";
            Attach(logGO, canvasGO, new Vector2(-680, 0), new Vector2(500, 700));
            var logText = logGO.GetComponent<Text>();
            logText.text = ""; logText.fontSize = 18;
            logText.color = new Color(0.2f, 0.2f, 0.2f);
            logText.alignment = TextAnchor.LowerLeft;

            // InputField
            var inputGO = DefaultControls.CreateInputField(res);
            inputGO.name = "InputField";
            Attach(inputGO, canvasGO, new Vector2(-120, -420), new Vector2(700, 60));
            var inputField = inputGO.GetComponent<InputField>();

            // Send button
            var btnGO = DefaultControls.CreateButton(res);
            btnGO.name = "SendButton";
            Attach(btnGO, canvasGO, new Vector2(320, -420), new Vector2(160, 60));
            var sendButton = btnGO.GetComponent<Button>();
            var btnLabel = btnGO.GetComponentInChildren<Text>();
            if (btnLabel != null) btnLabel.text = "전송";

            // Affection slider
            var sliderGO = DefaultControls.CreateSlider(res);
            sliderGO.name = "AffectionGauge";
            Attach(sliderGO, canvasGO, new Vector2(0, 440), new Vector2(600, 30));
            var slider = sliderGO.GetComponent<Slider>();
            slider.minValue = 0; slider.maxValue = 100; slider.wholeNumbers = true; slider.value = 30;

            // Managers
            var gameGO = new GameObject("Game");
            var dialogueUI = gameGO.AddComponent<DialogueUI>();
            var llm = gameGO.AddComponent<LLMClient>();
            var gm = gameGO.AddComponent<GameManager>();
            var portraitController = portraitGO.AddComponent<PortraitController>();

            // 참조 배선 (private [SerializeField] 도 SerializedObject 로 설정 가능)
            Wire(dialogueUI, p => {
                p("inputField", inputField); p("sendButton", sendButton);
                p("dialogueText", dialogueText); p("logText", logText);
                p("affectionGauge", slider);
            });
            Wire(portraitController, p => p("portrait", portrait));
            Wire(gm, p => {
                p("ui", dialogueUI); p("portrait", portraitController); p("llm", llm);
            });

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveOpenScenes();
            Selection.activeGameObject = gameGO;
            Debug.Log("[SceneBuilder] 씬 생성 및 저장 완료. ▶ Play 를 누르세요.");
        }

        // --- helpers ---
        static Sprite Builtin(string path) => AssetDatabase.GetBuiltinExtraResource<Sprite>(path);

        static void Attach(GameObject go, GameObject parent, Vector2 pos, Vector2 size)
        {
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
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
