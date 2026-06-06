using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LLMNpc
{
    // 13.6 프롬프트 계층
    // 시스템 프롬프트(페르소나 + 규칙 + 현재 상태) + 최근 N턴 + 이번 입력 을 조립한다.
    // "프롬프트 = NPC 설계서이자 AI를 게임 규칙 안에 묶는 제약 시스템"
    public static class PromptBuilder
    {
        public static ChatMessage[] Build(Persona p, GameState state, string playerInput, int historyTurns)
        {
            var msgs = new List<ChatMessage>();

            // 1) 시스템 프롬프트
            msgs.Add(new ChatMessage("system", BuildSystemPrompt(p, state)));

            // 2) 단기 메모리: 최근 N턴 (1턴 = user + assistant = 2메시지)
            var hist = state.History;
            int start = Mathf.Max(0, hist.Count - historyTurns * 2);
            for (int i = start; i < hist.Count; i++)
                msgs.Add(hist[i]);

            // 3) 이번 플레이어 입력
            msgs.Add(new ChatMessage("user", playerInput));

            return msgs.ToArray();
        }

        static string BuildSystemPrompt(Persona p, GameState state)
        {
            var sb = new StringBuilder();
            // --- 페르소나 (13.5) ---
            sb.AppendLine($"너는 '{p.name}'이다. 역할: {p.role}.");
            sb.AppendLine($"성격: {p.personality}");
            sb.AppendLine($"말투: {p.speech_style}");
            sb.AppendLine($"플레이어와의 관계: {p.relation_to_player}");
            if (p.knows != null && p.knows.Length > 0)
                sb.AppendLine("네가 아는 것: " + string.Join(", ", p.knows));
            if (p.never_reveals != null && p.never_reveals.Length > 0)
                sb.AppendLine("절대 말하면 안 되는 것: " + string.Join(", ", p.never_reveals));

            // --- 장기 상태 주입 (13.7) ---
            sb.AppendLine($"현재 호감도: {state.Affection}/100 (낮을수록 서먹, 높을수록 친밀).");

            // --- 검증된 게임 상태 주입 (13.4) ---
            sb.AppendLine($"현재 장소: {state.Location}. 퀘스트 상태: {state.QuestStage}.");
            sb.AppendLine("퀘스트가 '시작 전'이면 퀘스트 관련 정보를 먼저 꺼내지 않는다. 현재 상태에 맞게 반응하라.");

            // --- 가드레일 + 구조화 출력 강제 (13.4-1, 13.11) ---
            sb.AppendLine("전연령 대화만 한다. 설정에 없는 정보는 지어내지 말고 모른다고 답한다.");
            sb.AppendLine("항상 1~2문장으로 짧게 답한다.");
            sb.AppendLine("상황에 맞으면 행동을 '제안'할 수 있다(실제 실행은 게임이 결정). action 에 다음 중 하나, 없으면 none:");
            sb.AppendLine("  none|start_quest|complete_quest|give_gift|leave");
            sb.AppendLine("반드시 아래 JSON 형식 '하나'로만 답한다. 그 외 텍스트/마크다운 금지:");
            sb.AppendLine("{\"reply\":\"대사\",\"emotion\":\"neutral|happy|sad|angry|shy\",\"affection_delta\":정수(-5~5),\"action\":\"none|start_quest|complete_quest|give_gift|leave\"}");

            return sb.ToString();
        }
    }
}
