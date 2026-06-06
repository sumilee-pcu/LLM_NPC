using UnityEngine;

namespace LLMNpc
{
    // 13.11 응답 후처리 / 가드레일
    // 원시 API 응답 -> NPC 응답(JSON) 추출 -> 검증/클램프. 실패하면 안전한 폴백 대사.
    public static class ResponseProcessor
    {
        // 13.11 화이트리스트: 허용된 행동만 통과 (그 외엔 none)
        static readonly System.Collections.Generic.HashSet<string> AllowedActions =
            new System.Collections.Generic.HashSet<string> { "none", "start_quest", "complete_quest", "give_gift", "leave" };

        public static NpcResponse Process(string rawApiJson)
        {
            try
            {
                var completion = JsonUtility.FromJson<ChatCompletionResponse>(rawApiJson);
                if (completion == null || completion.choices == null || completion.choices.Length == 0)
                    return Fallback("…응답이 비어 있어. 다시 말해줄래?");

                string content = completion.choices[0].message.content;

                // LLM이 ```json ... ``` 이나 잡텍스트를 섞어도 { ... } 부분만 추출
                string clean = ExtractJson(content);
                var npc = JsonUtility.FromJson<NpcResponse>(clean);

                if (npc == null || string.IsNullOrEmpty(npc.reply))
                    return Fallback("음… 뭐라고 했지?");

                // 가드레일: 호감도 변화량 범위 강제
                npc.affection_delta = Mathf.Clamp(npc.affection_delta, -5, 5);
                // 표정 기본값
                if (string.IsNullOrEmpty(npc.emotion)) npc.emotion = "neutral";
                // 길이 제한
                if (npc.reply.Length > 200) npc.reply = npc.reply.Substring(0, 200);
                // 행동 화이트리스트 검증
                if (string.IsNullOrEmpty(npc.action) || !AllowedActions.Contains(npc.action))
                    npc.action = "none";

                return npc;
            }
            catch
            {
                return Fallback("…잠깐 딴 생각했어. 다시 말해줄래?");
            }
        }

        static string ExtractJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "{}";
            int a = s.IndexOf('{');
            int b = s.LastIndexOf('}');
            if (a >= 0 && b > a) return s.Substring(a, b - a + 1);
            return "{}";
        }

        static NpcResponse Fallback(string msg)
            => new NpcResponse { reply = msg, emotion = "neutral", affection_delta = 0, action = "none" };
    }
}
