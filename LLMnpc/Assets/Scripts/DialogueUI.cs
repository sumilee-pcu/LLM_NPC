using System;
using UnityEngine;
using UnityEngine.UI;

namespace LLMNpc
{
    // 13.12 출력 계층 - 대사 UI / 입력 / 호감도 게이지
    // 인스펙터에서 각 UI 요소를 할당한다. (Canvas 아래에 InputField/Button/Text/Slider 배치)
    public class DialogueUI : MonoBehaviour
    {
        [Header("입력")]
        [SerializeField] InputField inputField;
        [SerializeField] Button sendButton;

        [Header("출력")]
        [SerializeField] Text dialogueText;   // 현재 NPC 대사
        [SerializeField] Text logText;        // 누적 대화 로그 (선택)
        [SerializeField] Slider affectionGauge; // 0~100

        public event Action<string> OnSubmit;

        void Start()
        {
            if (sendButton != null) sendButton.onClick.AddListener(Submit);
        }

        void Submit()
        {
            if (inputField == null) return;
            string text = inputField.text.Trim();
            if (string.IsNullOrEmpty(text)) return;
            inputField.text = "";
            OnSubmit?.Invoke(text);
        }

        public void ShowNpcLine(string npcName, string line)
        {
            if (dialogueText != null) dialogueText.text = line;
            if (logText != null) logText.text += $"\n{npcName}: {line}";
        }

        public void ShowPlayerLine(string line)
        {
            if (logText != null) logText.text += $"\n나: {line}";
        }

        public void SetAffection(int value)
        {
            if (affectionGauge != null) affectionGauge.value = value;
        }

        // 응답 대기 중 입력 잠그기
        public void SetInteractable(bool on)
        {
            if (sendButton != null) sendButton.interactable = on;
            if (inputField != null) inputField.interactable = on;
        }
    }
}
