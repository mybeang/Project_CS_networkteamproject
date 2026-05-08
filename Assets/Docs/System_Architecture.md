# Project CS - 매니저 시스템 설계 문서

---

## 목차

1. [시스템 설계 흐름도](#1-시스템-설계-흐름도)
2. [아키텍처 개요](#2-아키텍처-개요)
3. [시스템 간 의존 관계도](#3-시스템-간-의존-관계도)
4. [매니저별 사용법 (API 레퍼런스)](#4-매니저별-사용법-api-레퍼런스)

---

## 1. 시스템 설계 흐름도

### 1-1. 매니저 생명주기

모든 매니저는 `Manager<T>`를 상속받고, 아래 순서대로 생명주기가 진행된다.

```
Awake()
  └─ Init() 호출 (자식이 오버라이드)
       └─ 비동기 초기화가 필요한 매니저는 여기서 await 처리
          (LobbyManager, RelayHostManager, VoiceManager)

OnEnable()
  └─ Register() 호출
       └─ ServiceLocator.Register<인터페이스>(this)
          → 이 시점부터 외부에서 접근 가능

OnDisable()
  └─ Unregister() 호출
       └─ ServiceLocator.Unregister<인터페이스>()
          → 이 시점부터 외부에서 접근 불가
```

DDOL(DontDestroyOnLoad) 처리는 `Manager<T>` 내부가 아닌 `BootstrapDDOL` 스크립트가 담당한다. 모든 매니저는 BootstrapDDOL 오브젝트의 자식으로 배치되어 있으며, BootstrapDDOL이 자기 자신에게 DDOL을 적용하면 자식 매니저들도 함께 보존된다.

```csharp
// BootstrapDDOL.cs
public class BootstrapDDOL : MonoBehaviour
{
    private void Awake()
    {
        transform.SetParent(null);
        var candidates = GameObject.FindGameObjectsWithTag("ddolObject");
        if (candidates.Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }
}
```

같은 태그("ddolObject")를 가진 오브젝트가 이미 존재하면 자기 자신을 파괴하여 중복 생성을 방지한다.

### 1-2. 외부 코드의 매니저 접근 흐름

외부 클래스(UI Controller 등)가 매니저를 사용하는 과정이다.

```
[외부 코드]
    │
    │ ServiceLocator.Get<인터페이스>()
    │   → Dictionary에서 해당 인터페이스 타입으로 등록된 인스턴스 반환
    │   → 없으면 null 반환
    ▼
[인터페이스]
    │  외부 코드는 인터페이스만 알고, 구현체(매니저)는 모른다.
    │  → 구현체를 교체해도 외부 코드 수정 불필요
    ▼
[매니저 구현체]
    │  인터페이스에 정의된 메서드를 실제로 실행
    │  필요시 내부에서 다른 매니저를 ServiceLocator로 호출
    ▼
[외부 서비스]
    Firebase / Unity Lobby / Unity Relay / Vivox
```

### 1-3. 매니저 간 통신 흐름

매니저끼리 직접 참조하지 않고, 필요할 때 ServiceLocator를 통해 접근한다.

```
LobbyManager 내부
  ├─ 방 생성/입장 시 → ServiceLocator.Get<IUserInfoManager>()?.SetRoomId()
  └─ 방 퇴장 시     → ServiceLocator.Get<IDatabaseBackend>()?.RemoveJoinCodeAsync()

RelayHostManager 내부
  └─ Host/Client 시작 시 → ServiceLocator.Get<IUserInfoManager>()?.SetClientId()
```

UserInfoManager는 다른 매니저를 호출하지 않는다. 데이터를 보관만 하고, 다른 매니저들이 필요할 때 가져다 쓰는 구조다.

### 1-4. 매니저 분류

| 분류 | 매니저 | 부모 클래스 | 외부 서비스 |
|------|--------|------------|------------|
| 기반 | AudioManager | Manager\<T\> | 없음 (로컬) |
| 기반 | UserInfoManager | Manager\<T\> | 없음 (로컬) |
| 기반 | LocalSceneManager | Manager\<T\> | 없음 (로컬) |
| 네트워크 | DatabaseBackend | Manager\<T\> | Firebase Realtime DB |
| 네트워크 | LobbyManager | Manager\<T\> | Unity Lobby |
| 네트워크 | RelayHostManager | Manager\<T\> | Unity Relay + NGO |
| 네트워크 | VoiceManager | Manager\<T\> | Vivox |
| 네트워크 | NetworkSceneLoader | NetworkManager\<T\> | NGO SceneManager |

기반 매니저는 외부 서비스 없이 동작하므로 Init()에서 비동기 처리가 필요 없다.
네트워크 매니저는 외부 서비스 초기화를 기다려야 하므로 Init()에서 `await UnityServiceInitialize.Processing()`을 수행한다.

NetworkSceneLoader만 `NetworkManager<T>`(NetworkBehaviour 기반)를 상속받는다. IsServer 체크, RPC 등 NGO 전용 기능이 필요하기 때문이다.

---

## 2. 아키텍처 개요

### 2-1. Manager\<T\> - Template Method 패턴

모든 매니저의 부모 클래스다. 생명주기 흐름은 부모가 정의하고, 구체적인 내용은 자식이 채운다.

```csharp
public abstract class Manager<T> : MonoBehaviour
{
    private void Awake() => Init();

    private void OnEnable() => Register();
    private void OnDisable() => Unregister();

    protected virtual void Init() { }          // 선택적 오버라이드
    protected abstract void Register();         // 필수 구현
    protected abstract void Unregister();       // 필수 구현
}
```

자식 매니저를 새로 만들 때 해야 하는 것은 세 가지뿐이다.
- Init()에서 초기화 로직 작성 (필요한 경우만)
- Register()에서 ServiceLocator에 자기 인터페이스 타입으로 등록
- Unregister()에서 등록 해제

DDOL 처리는 BootstrapDDOL이 별도로 담당하므로, Manager\<T\>는 ServiceLocator 등록/해제와 초기화에만 집중한다.

네트워크 기능이 필요한 매니저는 `NetworkManager<T>`를 대신 상속받는다. 구조는 동일하되 NetworkBehaviour를 기반으로 하여 IsServer, RPC 등을 사용할 수 있다.

### 2-2. ServiceLocator 패턴

인터페이스 타입을 키로, 매니저 인스턴스를 값으로 저장하는 전역 딕셔너리다.

```csharp
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new();

    public static void Register<T>(T service)   => _services[typeof(T)] = service;
    public static void Unregister<T>()          => _services.Remove(typeof(T));
    public static T Get<T>()                    => _services.TryGetValue(typeof(T), out var s) ? (T)s : default;
}
```

싱글톤 대신 이 패턴을 쓰는 이유는 다음과 같다.
- 싱글톤을 남발하면 클래스 간 강한 커플링이 발생하고, 구현체를 교체하기 어렵다.
- ServiceLocator는 인터페이스로만 접근하므로, 구현체를 바꿔도 호출하는 쪽의 코드를 수정할 필요가 없다.

### 2-3. 전체 구조

```
외부 코드 (UI Controller 등)
    │
    │  ServiceLocator.Get<인터페이스>()?.메서드()
    ▼
인터페이스 계층
    IAudioService / IUserInfoManager / IDatabaseBackend
    ILobbyManager / IRelayHostManager / IVoiceManager
    ILocalSceneLoader / INetworkSceneLoader
    │
    │  implements
    ▼
매니저 계층 (Manager<T> 또는 NetworkManager<T> 상속)
    AudioManager / UserInfoManager / DatabaseBackend
    LobbyManager / RelayHostManager / VoiceManager
    LocalSceneManager / NetworkSceneLoader
    │
    │  외부 서비스 호출 (해당 매니저만 담당)
    ▼
외부 서비스 계층
    Firebase / Unity Lobby / Unity Relay / Vivox
```

하나의 외부 서비스에는 하나의 매니저만 접근한다. 예를 들어 Firebase에 접근하는 코드는 DatabaseBackend에만 존재하고, 다른 매니저나 UI에서 Firebase를 직접 호출하지 않는다.

---

## 3. 설계 의도

### 3-1. 왜 싱글톤 대신 ServiceLocator를 썼는지

기본적으로 게임을 만들고 그 게임에 대한 시스템을 설계할 때 우리는 시스템을 관리해주는 매니저를 먼저 생각한다.

그렇다면 해당 매니저들은 그 관리자의 성격을 가지고 있어, 해당 시스템을 관리해주는 객체가 결국 하나만 존재해야 하는데

그럴경우 보통 전역적으로 존재해야하고, 씬이 바뀔 때 파괴되지 않는 구조를 가진 싱글톤 패턴을 떠올린다.

허나, 싱글톤 패턴은 의존성이 커질 수밖에 없는데, 결국 외부 클래스가 싱글톤 매니저를 직접적으로 알고 참조해야 호출이 가능하기 때문에 우리가 배웠던 의존역전의 원칙에도 부합하지 않는다.

예시로 오디오 매니저를 갖고와야 한다면

`싱글톤 패턴`은

> AudioManager.Instance.SetBGMVolume()

위와 같은 형식으로 사용하여 오디오 매니저를 직접 알고있어야지만 참조가 가능한데

이 프로젝트에서 사용한 `ServiceLocator 패턴`의 경우

해당 매니저 즉, 실제로 구현하는 구현체는 몰라도

> ServiceLocator.Get<IAudioService>()?.SetBGMVolume();

ServiceLocator 클래스와 연관된 인터페이스만 알고 직접 구현하는 클래스는 모른채 호출할 수 있다.

### 3-2. 왜 Manager<T>로 공통 구조를 강제했는지

매니저가 하나둘이면 상관없지만, 프로젝트에 매니저가 늘어날수록 각각의 초기화 순서나 ServiceLocator 등록/해제 타이밍이 제각각이면 버그를 잡기 어려워진다.

Manager<T>는 Template Method 패턴을 활용해서 모든 매니저가 동일한 생명주기를 따르도록 강제한다.

```csharp
private void Awake()
{
    Init();       // 자식이 초기화 로직을 채움
}

private void OnEnable() => Register();     // ServiceLocator에 등록
private void OnDisable() => Unregister();  // ServiceLocator에서 해제
```

Awake → Init → Register 순서는 부모가 정해놓고, 자식 매니저는 각 단계의 내용만 채우면 된다. 새로운 매니저를 만들 때 Init, Register, Unregister 세 가지만 구현하면 ServiceLocator 연동이 자동으로 따라온다.

이 구조가 없다면 매니저마다 각자 Init()을 호출하고, 각자 OnEnable에서 ServiceLocator.Register를 호출해야 하는데, 하나라도 빠뜨리면 런타임에서 NullReferenceException이 터지고 원인을 찾기 어렵다. Manager<T>가 이 실수를 구조적으로 막아주는 역할을 한다.

DDOL의 관리는 기존 Manager<T>에서 관리를 했으나 BootstrapDDOL로 분리하여 따로 관리를 해준다.


### 3-3. DDOL을 왜 따로 관리하는지

기존 Manager<T>에서 관리해주는 방식일 때, 인스펙터 상에서 매니저마다 개별로 체크박스를 관리해주는 방식이었고, 중복 방지에 대한 예외처리가 없었던 상황이었다.

또한, 새 매니저를 추가한다면 인스펙터에서 체크박스를 체크하는 것을 깜빡했을 시 DDOL이 활성화가 되지 않는 문제가 발생한다.

```csharp
var candidates = GameObject.FindGameObjectsWithTag("ddolObject");
if (candidates.Length > 1)
{
    Destroy(gameObject);  // 이미 있으면 자기 자신(+ 자식 매니저 전부)을 파괴
    return;
}
DontDestroyOnLoad(gameObject);
```

그래서, 최상위 오브젝트에 BootstrapDDOL 스크립트를 추가하여 부모 오브젝트 하나로 통합하고, 태그 기반으로 이미 자신이 있으면 파괴하는 예외처리와 자식으로 넣기만 하면 자동적으로 적용되게 끔 설계하여 좀더 DDOL을 편리하게 관리해주기 위해 이러한 설계방식으로 수정하였다.

### 3-4. 왜 매니저를 나눠놨는지

현재 씬을 전환하는 매니저가

- LocalSceneManager

- NetworkSceneLoader
  
두 가지로 분류 되었는데

일반적인 상황이라면 하나의 매니저로 관리하겠지만 현재 만들고 있는 게임은 Unity Netcode 기반의 멀티플레이어 게임이다.

```csharp
    public void LoadScene(string sceneName)
    {
        if (!IsServer) return; // 서버에서 실행하고 있지 않다면 리턴

       // Unity NetCode의 NetworkSceneManager를 통해 씬 전환
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);   
    }
```

위 처럼 IsServer와 같은 기능은 NetworkBehaviour를 상속받은 클래스만 사용 가능하기 때문에 기존 씬 매니저와 따로 분리하여 사용할 필요가 있다.

### 3-5. 왜 UserInfoManager는 독립적인가

UserInfoManager는 유저의 정보(UserInfo클래스)만 저장하고 있는 역할을 하고있다.

LobbyManager, RelayHostManager 같은 여러 매니저가 유저의 정보를 필요로 하는데

그 상황에서 따로 저장한다면 유저의 정보가 달라질 수 있다.

그렇기 때문에 UserInfoManager 한 곳에서만 정보를 저장하고 다른 매니저들이 필요할 때 읽고 쓰는 구조로 설계한 것이다.

---

## 4. 매니저별 사용법 (API 레퍼런스)

접근 방법은 모든 매니저 공통이다.

```csharp
ServiceLocator.Get<인터페이스>()?.메서드();
```

---

### 4-1. IAudioService (AudioManager)

| 메서드 | 기능 |
|--------|------|
| `PlayBGM(AudioClip clip)` | BGM 재생 |
| `PlayOneShotSfx(AudioClip clip)` | 효과음 1회 재생 |
| `PlaySfx(AudioClip clip)` | 효과음 재생 |
| `SetBGMVolume(float volume)` | BGM 볼륨 설정 |
| `SetSfxVolume(float volume)` | SFX 볼륨 설정 |
| `GetBGMVolume(out float volume)` | BGM 볼륨 조회 |
| `GetSfxVolume(out float volume)` | SFX 볼륨 조회 |

---

### 4-2. IUserInfoManager (UserInfoManager)

유저 데이터의 단일 출처. 다른 매니저들이 유저 정보를 읽거나 쓸 때 반드시 이 매니저를 거친다.

| 메서드 | 기능 |
|--------|------|
| `GetUserInfo()` | UserInfo 객체 반환 |
| `SetUserId(string userId)` | 유저 ID 설정 |
| `SetRoomId(string roomId)` | 방 ID 설정 |
| `SetTeamNum(int teamNum)` | 팀 번호 설정 |
| `SetIsDriver(PlayerRole role)` | 역할 설정 |
| `SetClientId(ulong clientId)` | NGO ClientId 설정 |
| `AddScore(int score)` | 점수 추가 |
| `GetScore()` | 점수 반환 |

---

### 4-3. IDatabaseBackend (DatabaseBackend)

Firebase Realtime DB 통신 전담. 유저 접속 상태와 JoinCode를 관리한다.

| 메서드 | 기능 |
|--------|------|
| `SaveUserAsync(string userId)` | 접속 기록 저장 |
| `RemoveUserAsync(string userId)` | 접속 기록 삭제 |
| `ValidateDuplicateUserIdAsync(string userId)` | ID 중복 체크 (중복이면 true) |
| `RegisterUserDisconnectHandler(string userId)` | 비정상 종료 시 유저 데이터 자동 삭제 |
| `SetJoinCodeAsync(string roomId, string joinCode)` | JoinCode 저장 |
| `GetJoinCodeAsync(string roomId)` | JoinCode 조회 |
| `RemoveJoinCodeAsync(string roomId)` | JoinCode 삭제 |
| `RegisterRemoveRoomHandler(string roomId)` | 비정상 종료 시 JoinCode 자동 삭제 |

호출 예시 (로그인):

```csharp
var db = ServiceLocator.Get<IDatabaseBackend>();
bool isDup = await db.ValidateDuplicateUserIdAsync(inputId);
if (isDup) return;
db.SaveUserAsync(inputId);
db.RegisterUserDisconnectHandler(inputId);
```

---

### 4-4. ILocalSceneLoader / INetworkSceneLoader

로컬 씬 전환과 네트워크 씬 전환을 분리했다. NGO의 NetworkSceneManager는 string 파라미터만 지원하기 때문에 인터페이스도 다르다.

| 인터페이스 | 메서드 | 특징 |
|-----------|--------|------|
| ILocalSceneLoader | `LoadScene(string sceneName)` | 내 클라이언트만 전환 |
| ILocalSceneLoader | `LoadScene(int sceneIndex)` | 빌드 인덱스 전환 |
| INetworkSceneLoader | `LoadScene(string sceneName)` | Host만 호출 가능, 모든 클라이언트 동시 전환 |

---

### 4-5. ILobbyManager (LobbyManager)

Unity Lobby 서비스를 통한 방 관리 전반을 담당한다. 내부에서 Heartbeat 코루틴과 실시간 이벤트 구독을 자동으로 관리한다.

| 메서드 | 기능 |
|--------|------|
| `CreateRoom(string subject)` | 방 생성 |
| `JoinRoom(string roomId)` | 방 입장 |
| `LeaveRoom()` | 방 퇴장 (마지막이면 JoinCode 정리) |
| `GetRoomList(int offset)` | 방 목록 조회 (4개씩) |
| `RefreshRoomList()` | 방 목록 새로고침 |
| `GetPlayerList()` | 방 내 플레이어 목록 |
| `GetMyPlayerData()` | 내 데이터 조회 |
| `UpdatePlayerData(List<(string, string)>)` | 내 데이터 서버 반영 |
| `GetRoomID()` / `GetRoomName()` | 방 정보 조회 |
| `GetHostId()` | 방장 유저 ID |
| `Lock(bool isLock)` | 방 잠금/해제 |
| `IsUpdating()` | 데이터 갱신 중 여부 |
| `LobbyDataOnChangedAddListener(Action<Lobby>)` | 변경 콜백 등록 |
| `LobbyDataOnChangedRemoveListener(Action<Lobby>)` | 변경 콜백 해제 |

플레이어 데이터 키 (LobbyPlayerDataKey):

| 상수 | 값 | 용도 |
|------|----|------|
| USER_ID | "UserID" | 게임 내 유저 ID |
| TEAM | "Team" | 팀 번호 ("0"~"4") |
| ROLE | "Role" | 역할 (PlayerRole 문자열) |
| READY | "Ready" | 준비 상태 ("true"/"false") |

호출 예시:

```csharp
var lobby = ServiceLocator.Get<ILobbyManager>();

// 팀/역할 업데이트
var data = new List<(string, string)>
{
    (LobbyPlayerDataKey.TEAM, "1"),
    (LobbyPlayerDataKey.ROLE, $"{PlayerRole.Driver}")
};
await lobby?.UpdatePlayerData(data);

// 데이터 변경 감지
lobby?.LobbyDataOnChangedAddListener(OnLobbyChanged);
```

---

### 4-6. IRelayHostManager (RelayHostManager)

Unity Relay를 통한 Host/Client 네트워크 연결을 담당한다. Host 비정상 종료 감지 기능도 포함한다.

| 메서드 | 기능 |
|--------|------|
| `StartHost()` | Relay Host 시작, JoinCode 반환 |
| `StartClient(string joinCode)` | JoinCode로 Client 접속 |
| `Disconnect()` | 네트워크 종료 |
| `GetClientId()` | 내 NGO ClientId |
| `OnHostDisconnectedAddListener(Action)` | Host 비정상 종료 콜백 등록 |
| `OnHostDisconnectedRemoveListener(Action)` | 콜백 해제 |

호출 예시:

```csharp
var relay = ServiceLocator.Get<IRelayHostManager>();

// Host
string joinCode = await relay.StartHost();

// Client
relay.StartClient(joinCode);

// Host 비정상 종료 감지
relay.OnHostDisconnectedAddListener(() => {
    // 로비로 복귀 처리
});
```

---

### 4-7. IVoiceManager (VoiceManager)

Vivox 음성 채팅 채널 관리를 담당한다. 채널 이름은 `roomId + teamNum` 조합이며, 같은 팀끼리만 같은 채널에 입장한다.

| 메서드 | 기능 |
|--------|------|
| `OnJoinVoiceChannel(string channelName)` | 채널 입장 |
| `OnLeaveVoiceChannel(string channelName)` | 채널 퇴장 |
| `SetVolume(string channelName, int volume)` | 볼륨 조절 (-50 ~ 50) |
| `LoginEventAddListener(Action)` | Vivox 로그인 완료 콜백 등록 |
| `LoginEventRemoveListener(Action)` | 콜백 해제 |

호출 예시:

```csharp
var voice = ServiceLocator.Get<IVoiceManager>();
voice.OnJoinVoiceChannel(roomId + teamNum);
```
