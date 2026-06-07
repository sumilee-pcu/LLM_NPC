using UnityEngine;

namespace LLMNpc
{
    // 전체 오케스트레이션 - 13.12 의 5계층을 하나의 루프로 연결한다.
    // 입력(DialogueUI) → 상태(GameState) → 프롬프트(PromptBuilder)
    //   → 생성(LLMClient) → 후처리(ResponseProcessor) → 출력(DialogueUI/PortraitController)
    public class GameManager : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] DialogueUI ui;
        [SerializeField] PortraitController portrait;
        [SerializeField] LLMClient llm;
        [SerializeField] BackgroundController backgroundController;
        [SerializeField] PortraitAnimator portraitAnimator;

        [Header("설정")]
        [SerializeField] string personaResourceName = "Personas/yuna"; // Resources 기준 경로(확장자 제외)
        [SerializeField] int saveSlot = 0;
        [SerializeField] int endingTurn = 10; // 이 턴 수에 도달하면 엔딩 판정

        [Header("월드 상태 (실습: 값 바꾸고 대화해보기)")]
        [SerializeField] string questStage = "퀘스트 시작 전"; // 시작 전 / 진행 중 / 완료
        [SerializeField] string location = "학교 앞";

        LLMConfig config;
        Persona persona;
        GameState state;

        void Start()
        {
            // 1) 설정 로드 (프로바이더 중립)
            config = ConfigLoader.Load();

            // 2) 페르소나 로드 (Assets/Resources/Personas/yuna.json)
            var ta = Resources.Load<TextAsset>(personaResourceName);
            persona = ta != null ? JsonUtility.FromJson<Persona>(ta.text)
                                 : new Persona { id = "npc", name = "NPC" };

            // 3) 이어하기 or 새 게임
            state = SaveSystem.Load(saveSlot) ?? new GameState(persona.id);
            state.QuestStage = questStage; state.Location = location; // 인스펙터 초기값 반영
            if (backgroundController != null) backgroundController.SetByLocation(location);

            // 4) UI 초기화
            if (ui != null)
            {
                ui.OnSubmit += HandlePlayerInput;
                ui.SetAffection(state.Affection);
                ui.ShowNpcLine(persona.name, $"안녕, 난 {persona.name}야.");
            }
            if (portrait != null) portrait.SetEmotion("neutral");
        }

        void HandlePlayerInput(string text)
        {
            if (ui != null) { ui.ShowPlayerLine(text); ui.SetInteractable(false); }

            state.QuestStage = questStage; state.Location = location; // 인스펙터 값 실시간 반영
            if (backgroundController != null) backgroundController.SetByLocation(location); // 장소→배경 자동 전환
            var messages = PromptBuilder.Build(persona, state, text, config.history_turns);
            StartCoroutine(llm.Send(config, messages,
                raw => OnLLMResult(text, raw),
                err => OnLLMError(err)));
        }

        void OnLLMResult(string playerText, string raw)
        {
            NpcResponse npc = ResponseProcessor.Process(raw);
            Debug.Log($"[구조화출력] emotion={npc.emotion}, affection_delta={npc.affection_delta}, action={npc.action}");

            // 상태는 '코드'가 적용 (13.8)
            state.ApplyAffection(npc.affection_delta);
            ExecuteAction(npc.action);              // 13.11 NPC '제안' → 실행은 코드가 결정
            state.RecordTurn(playerText, npc.reply);

            // 출력 (13.12)
            if (portrait != null) portrait.SetEmotion(npc.emotion);
            if (portraitAnimator != null) portraitAnimator.React(); // 대사 시 살짝 팝
            if (ui != null)
            {
                ui.ShowNpcLine(persona.name, npc.reply);
                ui.SetAffection(state.Affection);
                ui.SetInteractable(true);
            }

            // 영속화 (13.7)
            SaveSystem.Save(state, saveSlot);

            // 엔딩 판정 (13.9)
            if (state.TurnCount >= endingTurn)
                Debug.Log($"[Ending] {state.GetEnding()} (호감도 {state.Affection}, {state.TurnCount}턴)");
        }

        // 13.11/13.14 에이전트: NPC가 제안한 action 을 '게임 시스템'이 검증·실행한다.
        void ExecuteAction(string action)
        {
            switch (action)
            {
                case "start_quest":    state.QuestStage = "퀘스트 진행 중"; break;
                case "complete_quest": state.QuestStage = "퀘스트 완료"; state.ApplyAffection(5); break;
                case "give_gift":      state.ApplyAffection(3); break;
                case "leave":          break;
                default:               return; // none → 아무것도 안 함
            }
            questStage = state.QuestStage; // 인스펙터 동기화 (다음 턴 덮어쓰기 방지)
            Debug.Log($"[Action] {action} 실행 → 퀘스트:{state.QuestStage}, 호감도:{state.Affection}");
        }

        void OnLLMError(string err)
        {
            Debug.LogError($"[LLM] {err}");
            if (ui != null)
            {
                // 실패 대비 폴백 (13.11)
                ui.ShowNpcLine(persona.name, "…연결이 잠깐 끊겼어. 다시 말해줄래?");
                ui.SetInteractable(true);
            }
        }
    }
}
