# 교재 13장 ↔ 실습 코드 매핑

교재 *제13장 Generative AI / LLM NPC* 의 개념이 실습 코드의 어디에 구현되어 있는지 정리한 표입니다.
강의 시연·과제 출제 시 절 번호와 파일을 1:1로 연결해 사용하세요.

| 13장 절 | 개념 | 대응 코드 / 파일 |
|---|---|---|
| 13.3 동작 구조 | 입력→Unity→LLM API→응답→UI | `GameManager.cs` (전체 루프) |
| 13.4 핵심 구성요소 | 시스템 프롬프트 / 컨텍스트 / 후처리 | `PromptBuilder.cs`, `ResponseProcessor.cs` |
| 13.4-1 구조화 출력 | JSON 으로 받아 분기 | `NpcResponse`(DataTypes), `ResponseProcessor.cs` |
| 13.5 페르소나 | NPC 정의 문서 | `Assets/Resources/Personas/yuna.json`, `Persona`(DataTypes) |
| 13.6 프롬프트 엔지니어링 | 시스템 프롬프트 조립 | `PromptBuilder.BuildSystemPrompt()` |
| 13.7 메모리/상태 추적 | 단기(최근 N턴)+장기(호감도) | `GameState.History`, `SaveSystem.cs` |
| 13.8 상태 vs 표현 분리 | 상태는 코드, 표현은 LLM | `GameState.ApplyAffection()` (코드가 결정) |
| 13.9 엔딩/동적 반응 | 상태 기반 분기 | `GameState.GetEnding()` |
| 13.11 가드레일 | 검증/클램프/폴백/실패대비 | `ResponseProcessor.cs`, `GameManager.OnLLMError()` |
| 13.12 5계층 구조 | 입력/상태/프롬프트/생성/출력 | `Scripts/` 폴더 전체 |
| 13.3 프로바이더 | 클라우드/로컬 전환 | `LLMConfig`, `ConfigLoader.cs`, `config.*.example.json` |

## 실습 제안 → 과제 연결
| 교재 실습 제안 | 손볼 파일 |
|---|---|
| 페르소나 프롬프트 작성 | `Personas/*.json` 새로 추가 |
| 퀘스트 전/후 다른 반응 | `PromptBuilder` 에 상태 플래그 주입 |
| 최근 3턴 단기 메모리 설계 | `config.json` 의 `history_turns` = 3 |
| 응답 후처리 목록 작성 | `ResponseProcessor.Process()` 확장 |

## 5계층 다이어그램
```
1. 입력 계층  : DialogueUI (InputField/Button)
2. 상태 계층  : GameState (호감도·턴·히스토리) ← 단일 진실 소유자
3. 프롬프트   : PromptBuilder (페르소나 + 규칙 + 상태요약)
4. 생성 계층  : LLMClient (OpenAI 호환 호출)
   (횡단)     : ResponseProcessor (JSON 파싱·검증·폴백), SaveSystem (영속화)
5. 출력 계층  : DialogueUI (대사/게이지) + PortraitController (표정)
```
