using System;
using System.Collections.Generic;

// 게임인공지능 13장 실습 - 공용 데이터 타입
// 모든 클래스는 JsonUtility 직렬화를 위해 [Serializable] + public 필드 사용.
namespace LLMNpc
{
    // === 13.3 / 13.8 LLM 설정 ===
    // 클라우드(OpenAI 등)와 로컬(LM Studio/Ollama)은 '같은 코드'로 동작한다.
    // 차이는 이 config 값(base_url, api_key, model)뿐이다.  ← "두 가지로 돌아간다"의 핵심
    [Serializable]
    public class LLMConfig
    {
        public string base_url = "https://api.openai.com/v1/chat/completions";
        public string api_key = "";
        public string model = "gpt-4o-mini";
        public int max_tokens = 300;
        public float temperature = 0.8f;
        public int history_turns = 10; // 단기 메모리: 최근 몇 턴을 문맥에 넣을지
    }

    // === 13.5 페르소나 ===
    [Serializable]
    public class Persona
    {
        public string id;
        public string name;
        public string role;
        public string personality;
        public string speech_style;
        public string[] knows;          // 알고 있는 정보
        public string[] never_reveals;  // 절대 말하면 안 되는 정보
        public string relation_to_player;
    }

    // === 13.6 프롬프트: OpenAI 호환 메시지 한 개 ===
    [Serializable]
    public class ChatMessage
    {
        public string role;    // "system" | "user" | "assistant"
        public string content;
        public ChatMessage() { }
        public ChatMessage(string role, string content) { this.role = role; this.content = content; }
    }

    // 요청 바디 (POST /v1/chat/completions)
    [Serializable]
    public class ChatRequest
    {
        public string model;
        public ChatMessage[] messages;
        public int max_tokens;
        public float temperature;
    }

    // 응답 바디
    [Serializable] public class ChatChoice { public ChatMessage message; }
    [Serializable] public class ChatCompletionResponse { public ChatChoice[] choices; }

    // === 13.4-1 구조화 출력: LLM이 돌려줘야 하는 JSON ===
    // reply  -> 대사 UI
    // emotion-> 표정 스프라이트
    // affection_delta -> 게임 로직(호감도). 단, '제안'일 뿐 최종 적용은 코드가 한다.
    [Serializable]
    public class NpcResponse
    {
        public string reply;
        public string emotion;       // neutral | happy | sad | angry | shy
        public int affection_delta;  // -5 ~ +5 (코드가 강제 클램프)
        public string action;        // none|start_quest|complete_quest|give_gift|leave (NPC '제안', 실행은 코드가 결정)
    }

    // === 13.7 세이브: 단기 메모리(history) + 장기 상태(affection) ===
    [Serializable]
    public class SaveData
    {
        public string persona_id;
        public int affection;
        public int turn_count;
        public string quest_stage = "퀘스트 시작 전";
        public string location = "학교 교실";
        public List<ChatMessage> history = new List<ChatMessage>();
    }
}
