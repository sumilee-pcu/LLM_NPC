# PRD — LLM NPC Simulator

> 게임인공지능 13장 실습 겸용 · Unity 6.3 LTS (Universal 2D)

## 1. 개요
플레이어가 자유 텍스트로 NPC와 대화하면, LLM이 페르소나·게임 상태에 맞춰 응답하고,
그 결과로 호감도가 변하며 표정·엔딩이 분기되는 2D 대화형 시뮬레이터.
LLM은 대사를 생성하고, **게임 상태는 코드가 관리**하는 분리 구조를 학생이 직접 구현·관찰한다.

## 2. 목표 / 비목표
**Goals**
- 입력 → LLM → 게임 반영의 완결된 루프
- JSON 구조화 출력(`reply/emotion/affection_delta`)으로 분기
- 호감도는 코드가 관리(LLM은 제안만)
- 단기 메모리(최근 N턴) + 장기 상태(호감도) 세이브
- 프로바이더 중립: 설정만으로 클라우드↔로컬 전환
- 13장 절과 코드 1:1 매핑 (교보재)

**Non-Goals (MVP 제외)**
- 3D, TTS/립싱크, Live2D, 멀티 NPC, 멀티플레이
- 무검열/성인 콘텐츠 (전연령 고정)
- 벡터DB RAG (채팅기록 JSON으로 충분)

## 3. 대상 사용자
- 학생(주): 다양한 PC → **클라우드 API가 기본 경로**
- 교수자: 맥북 로컬 LLM로 시연
- 플레이어: 대화형 시뮬레이터 이용자

## 4. 핵심 루프
```
입력 → 상태수집(페르소나+호감도+최근N턴) → 프롬프트 → LLM 호출
→ JSON 응답(reply/emotion/affection_delta) → 후처리/검증
→ 호감도 갱신(코드) + 표정 교체 + 대사 출력 → 세이브 → 엔딩 분기
```

## 5. 기능 요구사항 (P0=MVP)
- FR-01 텍스트 입력 + 대화 로그 UI (P0)
- FR-02 LLM HTTP 호출(OpenAI 호환) (P0)
- FR-03 JSON 응답 파싱 (P0)
- FR-04 호감도(코드 관리, 0~100 클램프) (P0)
- FR-05 페르소나 = 시스템 프롬프트 (P0)
- FR-06 단기 메모리(최근 N턴) (P0)
- FR-07 표정 스프라이트 스왑 (P0)
- FR-08 세이브/로드(JSON) (P0)
- FR-09 엔딩 분기(호감도 구간) (P0)
- FR-10 가드레일(검증·폴백) (P0)
- FR-11 응답 스트리밍(TTFT↓) (P1)
- FR-12 설정 화면(URL/모델/키 런타임 변경) (P1)
- FR-13 금지어 필터 (P1)
- FR-14 선택지 모드(LLM이 후보 생성) (P1)

## 6. LLM 연동 사양
- 프로토콜: OpenAI 호환 `POST /v1/chat/completions`
- 전환은 Base URL만 변경 (클라우드 / `localhost:1234`)
- 구조화 출력 스키마(계약):
```json
{ "reply": "string", "emotion": "neutral|happy|sad|angry|shy", "affection_delta": -5 }
```
- `affection_delta` 범위 -5~+5 (코드가 클램프), 파싱 실패 시 폴백

## 7. 데이터 모델
- 페르소나: `Resources/Personas/<id>.json`
- 세이브: `persistentDataPath/save_slot<n>.json` (persona_id, affection, turn_count, history)
- 설정: `StreamingAssets/config.json` (git 미포함)

## 8. 비기능 요구사항
- 재현성: 클라우드 기본 경로로 사양 무관 동작
- 응답 지연: 로컬 7~14B 기준 체감 1~2초 / 클라우드는 네트워크 의존
- 보안: API 키는 config.json 분리, example만 커밋
- 가독성: 스크립트 상단에 대응 13장 절 주석

## 9. 마일스톤
- M0 셋업: 프로젝트 + 첫 호출 성공
- M1 코어 루프(P0): 입력→JSON→호감도→표정→세이브→엔딩 1개
- M2 마감(P1): 스트리밍, 설정화면, 필터, 선택지
- M3 교보재화: 절↔코드 매핑, 단계별 커밋

## 10. 리스크
- JSON 미준수 → JSON 모드 + 폴백
- 학생 PC 로컬 모델 불가 → 클라우드 기본
- 호감도 일관성 → delta만 제안, 적용은 코드
- 키 유출 → gitignore + example 템플릿

## 11. 성공 기준 (DoD)
- README만으로 15분 내 첫 대화 루프
- 페르소나 교체 시 성격 변화 확인
- 설정만으로 클라우드↔로컬 전환
- 재시작 시 호감도·대화 복원
- 호감도 임계치에서 엔딩 분기
