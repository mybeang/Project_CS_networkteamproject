# 총력전 (All-out Rumble) — 기술 문서 색인

> 하나의 전차를 **운전수·포수 2명**이 나눠 조종하는 4v4 협력 럼블 슈팅.
> Unity 6 / URP · Netcode for GameObjects · Unity Lobby · Relay · Vivox · Firebase Realtime DB.
> 아래는 아키텍처·네트워크·게임플레이·데이터·패턴을 시스템별로 정리한 포트폴리오 기술 문서 목록이다.

각 문서는 `1. 개요 → 2. 설계 목표 → 3. 구성 요소 → 4. 핵심 흐름 → 5. 클래스 구조(Mermaid) → 6. 코드 하이라이트 → 7. 기술 포인트 → 8. 확장/한계`의 표준 골격을 따른다. (단 트러블슈팅 모음은 케이스북 형식)

---

## A. 아키텍처 · 설계

| 문서 | 한 줄 소개 |
| --- | --- |
| [`ServiceLocator.md`](./ServiceLocator.md) | Service Locator 기반 의존성 관리 — 인터페이스로 매니저를 등록·조회하는 DI 뼈대 |
| [`ManagerLifecycle.md`](./ManagerLifecycle.md) | `Manager<T>`·`NetworkManager<T>` — Template Method로 매니저 초기화·등록·정리 수명주기 자동화 |
| [`GameStateMachine.md`](./GameStateMachine.md) | `NetworkVariable<GameState>` + 코루틴 — 서버 권위형 게임 상태 머신 |
| [`Bootstrap.md`](./Bootstrap.md) | `BootstrapDDOL`·씬로더 — 영속(누가 사는가)·기동(언제 시작)·전환(어떻게 넘어가나)의 분리 |

## B. 네트워크 · 멀티플레이

| 문서 | 한 줄 소개 |
| --- | --- |
| [`RelayHostLifecycle.md`](./RelayHostLifecycle.md) | Relay 연결 생명주기 & 호스트 다운 감지 — 끊김 콜백의 자기참조로 서버 붕괴를 판별 |
| [`NetcodeSyncPatterns.md`](./NetcodeSyncPatterns.md) | Netcode 동기화 패턴 — 상태(NetworkVariable)·명령(RPC)·권한(Ownership) 3축 |
| [`LobbyPipeline.md`](./LobbyPipeline.md) | 로비→매칭→인게임 파이프라인 — 집합(UGS Lobby)·합의(PlayerData)·전이(SetData 핸드오프) |
| [`TeamVoiceChat.md`](./TeamVoiceChat.md) | Vivox 팀 음성 채널 — `roomId_teamNum` 이름 규칙만으로 팀 격리 |

## C. 게임플레이

| 문서 | 한 줄 소개 |
| --- | --- |
| [`CoopTankControl.md`](./CoopTankControl.md) | 협력 탱크 조작 — 운전수(소유·직접물리)와 포수(비소유·ServerRpc)의 역할 분리 |
| [`ProjectileDamage.md`](./ProjectileDamage.md) | 투사체·데미지 판정 — 히트스캔(Raycast)+비행 지연+서버 SphereCast 거리 감쇠 |
| [`RespawnScore.md`](./RespawnScore.md) | 리스폰·스코어 — 파괴 1건을 리스폰+점수+킬로그로 서버 원자 처리 |
| [`MapEventScheduler.md`](./MapEventScheduler.md) | 맵 돌발 이벤트 스케줄러 — ServerTime 기준 스케줄과 EventTask(Template Method) 내용 분리 |

## D. 데이터 · 백엔드

| 문서 | 한 줄 소개 |
| --- | --- |
| [`FirebaseBackend.md`](./FirebaseBackend.md) | Firebase Realtime DB — `OnDisconnect` 자동 정리 + `ValueChanged` 실시간 동기 |
| [`ScriptableObjectData.md`](./ScriptableObjectData.md) | ScriptableObject 데이터 설계 — 밸런스·에셋을 코드에서 분리해 에셋으로 관리 |

## E. 패턴 · 회고

| 문서 | 한 줄 소개 |
| --- | --- |
| [`ObserverLayers.md`](./ObserverLayers.md) | Observer 3계층 — C# event / NetworkVariable.OnValueChanged / Firebase ValueChanged |
| [`Troubleshooting.md`](./Troubleshooting.md) | 트러블슈팅 모음 — 호스트 다운·비동기 초기화·서버 권위 등 6케이스(증상→원인→해결) |

---

## 읽는 순서 추천

1. **아키텍처 먼저** — [`ServiceLocator`](./ServiceLocator.md) → [`ManagerLifecycle`](./ManagerLifecycle.md) → [`Bootstrap`](./Bootstrap.md) → [`GameStateMachine`](./GameStateMachine.md) 순으로 뼈대를 잡는다.
2. **네트워크 계층** — [`NetcodeSyncPatterns`](./NetcodeSyncPatterns.md)로 동기화 관용구를 익힌 뒤 [`RelayHostLifecycle`](./RelayHostLifecycle.md)·[`LobbyPipeline`](./LobbyPipeline.md)로 연결 흐름을 본다.
3. **게임플레이·데이터**는 관심 시스템 위주로, **[`Troubleshooting`](./Troubleshooting.md)**은 마지막에 문제 해결 서사로 읽으면 좋다.
