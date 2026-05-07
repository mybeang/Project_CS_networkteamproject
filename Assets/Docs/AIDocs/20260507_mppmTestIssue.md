# Unity MPPM 환경 트러블슈팅 리포트

## 이슈 발생일
- 2026년 05월 07일

---

## 이슈 발생: 플레이어 간 세션 충돌 및 서비스 파괴

- **상황**: MPPM을 통해 두 명의 플레이어(Virtual Player)를 실행했을 때, Player 1은 정상이나 Player 2가 접속하는 순간 Player 1에서 `NullReferenceException`이 발생하며 로비 서비스가 먹통이 됨.

- **추가 증상**: Firebase 사용 시 `ExecutionEngineException`과 함께 `Could not initialize persistence 에러`가 발생하며 유니티 엔진이 강제 종료(Crash)됨.

---

## 프롬프트: 주요 추가 질문 내용

- "Relay에서 방 폭파(Host 종료) 핸들링은 어떻게 해? OnClientDisconnectCallback은 내가 나갈 때도 발생하는 거 아냐?"
- "Player 2를 동작시킬 때 Player 1에서 NullReferenceException이 발생해. 뭔가 세션이 풀려버린 것 같은데 왜 그래?"
- "Firebase에서 String conversion error랑 persistence 초기화 실패 에러가 뜨면서 크래시가 나는데 원인이 뭐야?"

---

## 결과: 원인 분석 및 해결책

### A. UGS 세션 충돌 이슈
- **원인**: 유니티 서비스(UGS)는 기본적으로 한 기기에서 하나의 프로필만 사용하도록 설계됨. MPPM의 두 플레이어가 동일한 로컬 경로에 세션 데이터를 쓰려고 시도하면서 Player 1의 토큰이 오염됨.
- **해결책**: 초기화 시 `InitializationOptions.SetProfile`을 사용하여 각 플레이어에게 고유한 프로필 이름을 부여함으로써 저장 경로를 분리함.
- 예제 코드
```csharp
var options = new InitializationOptions();
options.SetProfile($"Player_{Guid.NewGuid().ToString().Substring(0, 8)}");
await UnityServices.InitializeAsync(options);
```

### B. 비동기 Race Condition (중복 호출)
- **원인**: 로비 이벤트 핸들러들이 동시다발적으로 `GetLobbyAsync`를 호출. 이전 요청이 완료되기 전 다음 요청이 들어오며 SDK 내부 엔진에서 참조 오류 발생.
- **해결책**: `isUpdating` 플래그를 이용한 비동기 락(`Async Lock`)을 구현하여 중복 호출을 방지하고, 가능한 경우 이벤트가 전달하는 데이터를 직접 활용함.

### C. Firebase Persistence 크래시
- **원인**: 두 프로세스가 동일한 로컬 SQLite 캐시 파일(.db)에 동시 접근하여 파일 잠금(File Lock) 충돌 발생. 네이티브 레이어의 에러가 C# 문자열로 변환되는 과정에서 메모리 오류(Illegal byte sequence) 유발.
- **해결책**: MPPM 테스트 환경에서는 로컬 캐싱 기능을 비활성화하여 파일 경합을 원천 차단함.
- 예제 코드
```csharp
FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(false);
```
---

# Unity MPPM 관련 추가 질문 내용

## 1. Unity 멀티플레이어의 물리적 실행 구조
- **질문**: 유니티 멀티플레이어에서 여러 플레이어가 생성되는 것은 멀티 프로세싱인가, 멀티 스레딩인가?
- **답변**: 둘 다 아니다. 유니티는 기본적으로 단일 프로세스 내의 단일 메인 스레드에서 동작한다. 네트워크로 수신된 데이터는 메인 스레드의 게임 루프(Update) 내에서 순차적으로 처리되어 오브젝트에 반영된다.

## 2. 싱글톤(Singleton) 충돌의 근본 원인
- **질문**: 멀티플레이어 환경에서 싱글톤 객체들이 자꾸 충돌하고 데이터가 꼬이는 이유는 무엇인가?
- **답변**: 객체 참조의 덮어쓰기(Overwrite) 문제이다. 한 화면에 '나'와 '상대방' 플레이어가 동시에 생성될 때, 두 객체가 동일한 `static Instance` 변수에 자기 자신을 등록하려다 보니 마지막에 생성된 객체만 싱글톤에 남게 되어 논리적 오류가 발생한다.

## 3. MPPM 환경에서의 Lobby 서비스 충돌 (Root Cause)
- **질문**: MPPM(Multiplayer Play Mode)에서 테스트할 때 유독 Lobby 같은 기본 제공 싱글톤이 말썽인 이유는?
- **답변**: 정적 메모리(Static Memory)의 공유 때문이다. MPPM은 프로세스를 완전히 분리하지 않고 하나의 프로세스 안에서 가상 클라이언트를 구동한다. 이때 LobbyService.Instance 같은 static 필드는 모든 가상 플레이어가 동일한 메모리 주소를 공유하므로, 한 플레이어의 상태가 다른 플레이어의 정보를 오염시키게 된다.

## 4. 해결 방안: 서비스 세션 및 컨텍스트 분리
- **질문**: `LobbyService.Instance`를 각 플레이어마다 별도로 들고 있을 수 없다면 어떻게 해결해야 하는가?
- **답변**: 싱글톤을 '도구'로만 사용하고 '데이터'는 객체에 격리해야 한다.
  - `LobbyService.Instance` 자체를 분리할 수는 없으므로, 호출 시점에 `LobbyID`나 `PlayerID` 같은 식별 데이터를 싱글톤 내부 상태에 의존하지 않고 매개변수(Parameter)로 직접 전달한다.
  - 각 가상 플레이어 컴포넌트 내부에 개별 멤버 변수로 정보를 저장하여 관리하는 서비스 래퍼(Wrapper) 패턴을 권장한다.