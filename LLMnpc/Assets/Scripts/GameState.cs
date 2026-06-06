using System.Collections.Generic;
using UnityEngine;

namespace LLMNpc
{
    // 13.8 상태 계층 - 게임 상태의 '단일 진실 소유자'
    // 핵심 원칙: 호감도/엔딩 같은 게임 상태는 코드가 결정한다. LLM은 '제안'만 한다.
    public class GameState
    {
        public string PersonaId;
        public int Affection { get; private set; }
        public int TurnCount { get; private set; }
        public List<ChatMessage> History = new List<ChatMessage>();

        public GameState(string personaId, int affection = 30)
        {
            PersonaId = personaId;
            Affection = affection;
        }

        // 호감도는 코드가 적용·클램프한다 (LLM 의 affection_delta 는 제안일 뿐)
        public void ApplyAffection(int delta)
        {
            Affection = Mathf.Clamp(Affection + delta, 0, 100);
        }

        // 단기 메모리 누적
        public void RecordTurn(string playerText, string npcReply)
        {
            History.Add(new ChatMessage("user", playerText));
            History.Add(new ChatMessage("assistant", npcReply));
            TurnCount++;
        }

        // 13.9 엔딩 분기 - 상태는 코드가 결정 (LLM 이 결정하지 않는다)
        public string GetEnding()
        {
            if (Affection >= 71) return "GOOD";
            if (Affection >= 31) return "NORMAL";
            return "BAD";
        }

        // 13.7 세이브 직렬화
        public SaveData ToSave() => new SaveData
        {
            persona_id = PersonaId,
            affection = Affection,
            turn_count = TurnCount,
            history = History
        };

        public static GameState FromSave(SaveData d)
        {
            return new GameState(d.persona_id, d.affection)
            {
                TurnCount = d.turn_count,
                History = d.history ?? new List<ChatMessage>()
            };
        }
    }
}
