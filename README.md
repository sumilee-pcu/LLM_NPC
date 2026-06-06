# LLM NPC Simulator — 게임인공지능 13장 실습

LLM(대형 언어 모델)으로 움직이는 **대화형 NPC 시뮬레이터**입니다.
플레이어가 자유롭게 말을 걸면, NPC가 **페르소나·게임 상태에 맞춰 대답**하고,
그 결과로 **호감도가 변하며 표정과 엔딩이 달라집니다.**

> 이 프로젝트의 핵심 메시지: *"모델이 대답한다"가 아니라 **게임 시스템 안에서 통제된 LLM NPC를 설계한다.*** (교재 13장)

- **엔진**: Unity 6.3 LTS (Universal 2D)
- **LLM 연동**: OpenAI 호환 API (클라우드 / 로컬 **같은 코드, 설정만 교체**)

---

## 1. 실행에 필요한 것
- Unity 6.3 LTS (6000.3.x) — 프로젝트 폴더: `LLMnpc/`
- 아래 둘 중 **하나**의 LLM 백엔드
  - **클라우드 (Windows 학생 권장)**: **Gemini(무료, 추천)** 또는 OpenAI API 키
    - Gemini 키 발급: [Google AI Studio](https://aistudio.google.com/apikey) → 무료, 신용카드 불필요
  - **로컬 (GPU 있는 PC)**: [LM Studio](https://lmstudio.ai/) 또는 Ollama

---

## 2. 빠른 시작 (5단계)

### 1) 프로젝트 열기
Unity Hub → `LLMnpc/` 폴더 열기.

### 2) 설정 파일 만들기 (API 키 입력)
`LLMnpc/Assets/StreamingAssets/` 안에서:

- **Gemini로 돌릴 때 (무료, 추천)** → `config.gemini.example.json` 을 복사해 **`config.json`** 으로 이름 변경 후, `api_key` 에 [Google AI Studio](https://aistudio.google.com/apikey) 키 입력.
- **OpenAI로 돌릴 때** → `config.cloud.example.json` 을 복사해 **`config.json`** 으로 이름 변경 후, `api_key` 에 본인 키 입력.
- **로컬로 돌릴 때** → `config.local.example.json` 을 복사해 **`config.json`** 으로 이름 변경. (LM Studio에서 서버를 포트 1234로 켜기)

```json
{
  "base_url": "https://api.openai.com/v1/chat/completions",
  "api_key": "sk-...",
  "model": "gpt-4o-mini",
  "max_tokens": 300,
  "temperature": 0.8,
  "history_turns": 10
}
```

> ⚠️ `config.json` 은 `.gitignore` 에 등록되어 **GitHub에 올라가지 않습니다.** (키 보호)
> 클라우드↔로컬 전환은 `base_url`/`api_key`/`model` 만 바꾸면 됩니다. **코드 수정 없음.**

### 3) 캐릭터 에셋 넣기 (자동 매핑)
캐릭터 이미지는 라이선스상 이 저장소에 포함하지 않습니다. **링크에서 직접 받아** 추출 스크립트를 한 번 돌리면, Unity가 자동으로 표정을 매핑합니다.

1. [Sutemo 무료 캐릭터 스프라이트](https://sutemo.itch.io/female-character) 에서 **PSD 다운로드** (상업 OK, 크레딧 권장)
2. 추출 스크립트 실행 (Python 필요):
   ```bash
   pip install -r tools/requirements.txt
   python tools/extract_yuna.py "<다운로드한 PSD 경로>"
   # 경로 생략 시 프로젝트 폴더/Downloads 에서 자동 탐색
   ```
   → `LLMnpc/Assets/Art/Characters/yuna/expr/` 에 표정 11종 PNG 생성
3. Unity로 돌아오면 **자동으로 PortraitController 에 매핑**됩니다 (`YunaArtPostprocessor`).
   - 안 되면: `Tools > LLM NPC > Expression Mapper` → **씬에 적용**
4. 표정 조합을 바꾸려면 **Expression Mapper** 창에서 드롭다운으로 선택 → 적용 (코드 수정 불필요)

> 표정 그림이 없어도 게임은 **플레이스홀더로 정상 작동**합니다(대화·호감도 OK). 그림은 위 단계로 나중에 채우면 됩니다.

### 4) 씬 구성 (UI 연결)
`SampleScene` 에 Canvas를 만들고 아래를 배치 → 각 스크립트의 `[SerializeField]` 슬롯에 할당:
- `InputField`(플레이어 입력), `Button`(전송), `Text`(NPC 대사), `Text`(로그), `Slider`(호감도)
- 빈 오브젝트에 `GameManager`, `DialogueUI`, `LLMClient` 부착, 캐릭터에 `PortraitController` 부착

### 5) ▶ Play
NPC에게 말을 걸어보세요. 호감도가 변하고 표정이 바뀌면 성공입니다.

---

## 3. 어떻게 동작하나 (아키텍처)

```
플레이어 입력 (DialogueUI)
  → 상태 수집 (GameState: 페르소나 + 호감도 + 최근 N턴)
  → 프롬프트 조립 (PromptBuilder)
  → LLM 호출 (LLMClient, OpenAI 호환)
  → JSON 응답 후처리 (ResponseProcessor)  { reply, emotion, affection_delta }
  → 출력: 대사(DialogueUI) · 표정(PortraitController) · 호감도(코드가 적용)
  → 세이브 (SaveSystem) → 엔딩 판정
```

**설계 원칙**: 호감도·엔딩 같은 **게임 상태는 코드(GameState)가 결정**합니다.
LLM은 `affection_delta` 를 *제안*만 하고, 실제 적용·한계·저장은 게임이 합니다.

---

## 4. 폴더 구조
```
LLM_NPC/
├─ README.md            ← (지금 이 파일)
├─ CREDITS.md           ← 에셋 출처/라이선스
├─ .gitignore
├─ docs/
│   ├─ PRD.md                   ← 제품 요구사항 문서
│   └─ chapter13_mapping.md     ← 교재 13장 ↔ 코드 매핑
└─ LLMnpc/              ← Unity 프로젝트
    └─ Assets/
        ├─ Scripts/            ← 실습 코드 (계층별로 분리)
        ├─ Resources/Personas/ ← 페르소나 JSON
        └─ StreamingAssets/    ← config.*.example.json (실제 config.json은 직접 생성)
```

---

## 5. 자주 막히는 곳
| 증상 | 원인 / 해결 |
|---|---|
| 응답이 안 옴 | `config.json` 없음/오타. Console 경고 확인 |
| 401 / 403 | API 키 오류 (클라우드) |
| 로컬 연결 실패 | LM Studio 서버 미실행 또는 포트 불일치(1234) |
| 표정이 안 바뀜 | `PortraitController` 슬롯 미할당 |
| JSON 파싱 실패 | 모델이 형식을 안 지킴 → 코드가 폴백 처리(정상). 더 좋은 모델 사용 권장 |

---

## 6. 라이선스
- 코드: 교육용. 자유롭게 사용/수정.
- 에셋: 각 에셋의 라이선스를 따르며 `CREDITS.md` 에 출처를 기록할 것.
- **금지**: 상용 게임/타인 캐릭터 무단 사용, 비상업(NC) 에셋의 상업적 사용.
