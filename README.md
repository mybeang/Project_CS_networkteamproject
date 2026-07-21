![gametitle](./Assets/Docs/Imgs/readmeTitle.png)

# Project CS (Co-op Shooting)
## 게임 정보

- 게임 제목: 총력전 (All-out Rumble)

| 항목 | 내용 |
| --- | --- |
| 장르 | 2인 협력 럼블 슈팅 |
| 플랫폼 | Windows |
| 엔진 | Unity 6 (6000.3.9f1) · URP |
| 언어 | C# |
| 개발 기간 | 2026.04.23 ~ 2026.05.18 |
| 인원 | 개발 3인 |
| 담당 업무 | • 팀 리더<br>• 시스템 전반, 로비 구현, UI 일부, NetCode |
| 기술 스택 | • 네트워크 프로그래밍: NetCode, Unity Lobby<br>• 데이터: Firebase Realtime Database, Scriptable Objects |


## 플레이 방식

- 탱크 1대를 2명(포수/운전수)이 나눠 조종하여 적을 격퇴.
- 최종 스코어가 높은 팀이 우승.

### 조작 방식

- 공통
  - Tab 키로 Score Board 확인 가능
- 운전수
  - WASD 로 이동
  - SpaceBar 로 정위치 소환 가능
- 포수
  - WASD 로 포탑 회전 및 포신 제어
  - Enter 및 마우스 왼쪽 클릭으로 발사

## 아키텍처 하이라이트

### 1. Service Locator 기반 의존성 분리
- `ServiceLocator`(정적 `Dictionary<Type, object>`)를 중심으로 매니저를 **인터페이스 타입으로 등록/조회**한다.
- 소비 측은 구현체가 아닌 `ServiceLocator.Get<IGameManager>()`, `Get<IMapManager>()`처럼 인터페이스에만 의존 → 구현체 교체·모킹이 자유롭다.
- `IDatabaseBackend`, `IAudioService`, `IVoiceManager` 등 외부 백엔드(Firebase·오디오·Vivox)도 인터페이스 뒤로 숨겨 교체 가능한 형태로 격리.

### 2. 매니저 수명주기 자동화 (Template Method)
- 모든 매니저는 `Manager<T>`(일반) 또는 `NetworkManager<T>`(네트워크) 추상 베이스를 상속.
- `Awake → Init()`, `OnEnable → Register()`, `OnDisable → Unregister()`로 **ServiceLocator 등록/해제를 수명주기에 자동 연결** → 등록 누락·중복을 구조적으로 방지.
- `BootstrapDDOL`이 매니저 루트를 `DontDestroyOnLoad`로 유지하며 태그 기반 중복 인스턴스를 차단.

### 3. 서버 권위형 게임 상태 머신
- `GameManager`가 `GameStateMachineCoroutine`으로 `GameState`(`Init → ResetGameData → InstantiateVehicle → SetOtherDataForGame → ReadyDone → DoNothing / GameEnd`)를 순차 전이.
- **상태값 자체가 `NetworkVariable<GameState>`** → 서버만 전이를 결정하고 클라이언트는 이를 따라가는 서버 권위(Server-Authoritative) 구조.
- 차량 스폰(`SpawnAsPlayerObject`), 점수 판정, 리스폰 코루틴 등 게임 로직을 서버에 집중시켜 클라이언트 간 정합성을 확보.

### 4. Observer 패턴의 3계층 활용
- **C# 이벤트**: `OnKillLog`, `OnChangeScore`, `OnChangeTime`을 `Add/RemoveHandler` 쌍으로 노출해 UI가 게임 상태 변화를 구독.
- **Netcode `NetworkVariable.OnValueChanged`**: 리스폰 카운터·경과 시간 등 네트워크 동기화 값의 변화를 구독.
- **Firebase RealtimeDB `ValueChanged`**: 로비 맵 선택 등 방 데이터의 실시간 반영(`RegisterMapNumberValueChangedHandler`).

### 5. 역할별 멀티 백엔드 조합
- **Unity Lobby** — 방 목록/생성/매칭, **Relay** — 호스트 연결, **Netcode** — 인게임 동기화, **Firebase Realtime DB** — 유저/방/조인코드/맵의 영속 데이터, **Vivox** — 팀 음성 채널.
- 각 백엔드를 전용 매니저 + 인터페이스로 감싸 **책임을 명확히 분리**하고 ServiceLocator를 통해 조립.

## 화면 구성

### 로그인
![login](./Assets/Docs/Imgs/title.png)

- User ID 만을 이용해 로그인 가능.

### 로비 리스트
![lobbyList](./Assets/Docs/Imgs/lobbyList.png)

- 열린 방에 참여
- 혹은 방을 새로 생성 가능

### 로비 방
![lobbyRoom](./Assets/Docs/Imgs/lobbyRoom.png)

- 최소 4인, 총 8인 구성
- 팀별 2인 최소 배치 요구

### 인 게임
![InGame](./Assets/Docs/Imgs/inGame.png)

- 포수/운전수는 각각 다른 UI 로 확인 가능

### 결과창
![Result](./Assets/Docs/Imgs/result.png)

- Score 가 높은 사람이 전투 승리!


---
