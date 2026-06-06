# 씬 셋업 상세 가이드 (Unity 에디터)

이 문서는 `SampleScene` 에 UI와 스크립트를 연결해 실제로 NPC와 대화가 되게 만드는 단계별 안내입니다.
코드는 이미 들어가 있으니, 여기서는 **인스펙터 연결**만 하면 됩니다.

> ⚠️ **중요 — 반드시 "Legacy" UI 사용**
> 우리 스크립트는 `UnityEngine.UI` 의 `Text`, `InputField`, `Button`, `Slider`, `Image` 를 씁니다.
> Unity 6 에서 `GameObject > UI > Text` 는 기본이 TextMeshPro라 타입이 안 맞습니다.
> **반드시 `GameObject > UI > Legacy >` 아래의 Text / Input Field / Button 을 사용**하세요.
> (TextMeshPro로 가고 싶다면 스크립트의 `using UnityEngine.UI` / 타입을 TMP로 바꿔야 함)

---

## 0. 사전 준비
- `Assets/StreamingAssets/config.json` 생성 (example 복사 후 키 입력) — 안 하면 응답이 안 옵니다.

## 1. Canvas 만들기
1. Hierarchy 우클릭 → **UI > Canvas** (EventSystem 자동 생성됨)
2. Canvas 선택 → Canvas Scaler → **UI Scale Mode = Scale With Screen Size**, Reference 1920x1080 권장

## 2. UI 요소 배치 (모두 Canvas 자식으로)
Hierarchy의 Canvas 우클릭 → 각 항목 생성:

| 만들 것 | 메뉴 | 이름(권장) | 용도 |
|---|---|---|---|
| 캐릭터 일러스트 | UI > **Image** | `Portrait` | 표정 스프라이트 표시 |
| NPC 대사 | UI > Legacy > **Text** | `DialogueText` | 현재 대사 |
| 대화 로그 | UI > Legacy > **Text** | `LogText` | 누적 로그(선택) |
| 입력창 | UI > Legacy > **Input Field** | `InputField` | 플레이어 입력 |
| 전송 버튼 | UI > Legacy > **Button** | `SendButton` | 보내기 |
| 호감도 게이지 | UI > **Slider** | `AffectionGauge` | 0~100 |

- `AffectionGauge` 선택 → Slider 컴포넌트에서 **Min Value 0, Max Value 100**, Whole Numbers 체크
- `DialogueText`, `LogText` 는 글자색/크기를 보기 좋게 조정, 영역 넓히기

## 3. 빈 게임오브젝트(매니저) 만들기
1. Hierarchy 우클릭 → **Create Empty** → 이름 `Game`
2. `Game` 선택 → Inspector → **Add Component** 로 다음 3개 부착:
   - `GameManager`
   - `DialogueUI`
   - `LLMClient`
   - (`PortraitController` 는 아래 4번처럼 Portrait 오브젝트에 붙여도 되고 여기에 붙여도 됨)

## 4. 표정(Portrait) 연결
1. `Portrait` (UI Image) 선택 → **Add Component** → `PortraitController`
2. `PortraitController` 의 슬롯 채우기:
   - **Portrait** ← `Portrait` 오브젝트의 Image 컴포넌트 드래그
   - **Neutral / Happy / Sad / Angry / Shy** ← Sutemo에서 export한 표정 스프라이트 각각 드래그
     - (아직 에셋이 없으면 임시로 같은 이미지 5개를 넣어 동작만 확인 가능)

## 5. DialogueUI 연결
`Game` 의 `DialogueUI` 컴포넌트 슬롯:
- **Input Field** ← `InputField`
- **Send Button** ← `SendButton`
- **Dialogue Text** ← `DialogueText`
- **Log Text** ← `LogText`
- **Affection Gauge** ← `AffectionGauge`

## 6. GameManager 연결
`Game` 의 `GameManager` 컴포넌트 슬롯:
- **Ui** ← `Game` (DialogueUI가 붙은 오브젝트)
- **Portrait** ← `Portrait` (PortraitController가 붙은 오브젝트)
- **Llm** ← `Game` (LLMClient가 붙은 오브젝트)
- **Persona Resource Name** = `Personas/yuna` (기본값)
- **Save Slot** = 0, **Ending Turn** = 10

## 7. 실행
- 상단 ▶ Play
- 입력창에 "안녕" 입력 → 전송 → NPC 응답 + 호감도 게이지 변화 + (스프라이트 있으면) 표정 변화 확인
- Console에 `[Save] ...` 로그가 뜨면 세이브 정상

---

## 동작 점검 체크리스트
- [ ] config.json 생성하고 키 입력함
- [ ] 모든 UI가 Legacy 타입 (TMP 아님)
- [ ] DialogueUI / GameManager / PortraitController 슬롯 전부 채움
- [ ] Play 후 응답이 옴 (안 오면 Console 경고/HTTP 코드 확인)
- [ ] 표정 슬롯에 스프라이트 할당 (없으면 표정만 안 바뀜, 대화는 정상)

## 트러블슈팅
| 증상 | 해결 |
|---|---|
| 버튼 눌러도 무반응 | DialogueUI 의 SendButton/InputField 슬롯 미할당 |
| `NullReferenceException` | GameManager 의 ui/portrait/llm 슬롯 미할당 |
| 타입이 안 맞아 드래그가 안 됨 | UI가 TMP임 → Legacy로 다시 생성 |
| 응답 없음 / 401 | config.json 누락 또는 API 키 오류 |
| 로컬 연결 실패 | LM Studio 서버 실행 + 포트 1234 확인 |
