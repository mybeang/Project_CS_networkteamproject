# 트러블슈팅 모음 (Troubleshooting Casebook)

> 총력전 개발에서 마주친 네트워크·비동기·데이터 정합성 문제와 그 해결을 케이스별로 정리한다. 각 항목은 *증상 → 원인 → 해결 → 결과/트레이드오프* 순으로, 실제 코드 근거와 함께 남긴다.
> 시스템 문서(1~8 골격)와 달리 이 문서는 문제 해결 서사를 담는 케이스북 형식이다.
>
> 관련 문서: [`RelayHostLifecycle.md`](./RelayHostLifecycle.md) · [`LobbyPipeline.md`](./LobbyPipeline.md) · [`NetcodeSyncPatterns.md`](./NetcodeSyncPatterns.md) · [`ProjectileDamage.md`](./ProjectileDamage.md) · [`FirebaseBackend.md`](./FirebaseBackend.md)

---

## 케이스 목록

| # | 문제 | 영역 | 핵심 해결 |
| --- | --- | --- | --- |
| 1 | 호스트 다운을 클라가 즉시 감지 못함 | Relay/Netcode | 끊김 콜백의 `clientId` 자기참조 판별 |
| 2 | 여러 매니저의 서비스 초기화 중복·경쟁 | 비동기 초기화 | 단일 `Task` 캐시 게이트 |
| 3 | MPPM에서 로비 갱신 간헐 누락 | 로비 동기화 | 재시도 큐 + 재진입 가드 |
| 4 | 데미지 판정 권위 소재 결정 | 서버 권위 | 판정=서버 / 적용=소유자 분리 |
| 5 | 씬 전환 시 팀 데이터 유실 위험 | 상태 승계 | 로컬+네트워크 이중 핸드오프 |
| 6 | 비정상 종료 시 유령 유저·방 잔존 | 백엔드 정리 | Firebase `OnDisconnect` 서버 자동 정리 |

---

## 1. 호스트 다운 감지 — 끊김 콜백의 자기참조를 붕괴 신호로

**증상.** Relay 세션에서 호스트가 나가면 방 전체가 무너지는데, 남은 클라이언트가 "서버가 죽었다"를 즉시 알 방법이 없었다. Netcode는 별도의 "서버 종료" 이벤트를 주지 않는다.

**원인.** 클라이언트 입장에서 서버 붕괴는 일반적인 연결 종료와 구분되지 않는다. `OnClientDisconnectCallback`은 오지만, 그것이 남의 이탈인지·내 종료인지·서버 붕괴인지 콜백만으로는 알 수 없다.

**해결.** Relay 세션에서 **서버가 사라지면 끊긴 대상이 자기 자신(`LocalClientId`)으로 통보된다**는 동작을 판별식으로 삼았다. 여기에 자발적 종료를 가리는 `_isQuit` 플래그를 더해 세 상황을 갈랐다.

```csharp
private void HostDisconnected(ulong clientId) {
    if (LocalClientId != ServerClientId) {                 // 나는 클라
        if (clientId == LocalClientId) {                    // 서버 붕괴 시 '나'로 통보됨
            if (_isQuit) { _isQuit = false; return; }       // 내가 자발적으로 나감 → 제외
            _onHostDisconnected?.Invoke();                   // 서버 붕괴 → 상위 통보
        }
    } else _isQuit = false;                                  // 나는 서버 → 남의 이탈
}
```

**결과.** 별도 하트비트·타임아웃 없이 프레임워크가 주는 신호만으로 호스트 다운을 감지해, 호스트 마이그레이션([`RelayHostLifecycle`](./RelayHostLifecycle.md))의 방아쇠를 확보했다. *트레이드오프:* 특정 Netcode/Relay 버전의 동작에 의존하므로 업그레이드 시 회귀 검증이 필요하다.

## 2. 비동기 서비스 초기화 — 중복·경쟁을 단일 Task로

**증상.** Relay·Lobby·Voice 매니저가 각자 UGS(Unity Services) 초기화와 익명 로그인을 요청하면서, 같은 초기화가 중복 실행되거나 경쟁 상태가 생겼다.

**원인.** 각 매니저의 `Init`이 독립적으로 `UnityServices.InitializeAsync`/로그인을 호출했다. 여러 매니저가 거의 동시에 깨어나 초기화가 겹쳤다.

**해결.** 초기화를 단일 `Task`로 캐시하는 정적 게이트를 뒀다. 진행 중이면 그 `Task`를 함께 `await`하고, 실제 초기화는 한 번만 실행된다.

```csharp
public static async Task Processing() {
    if (_isInitializing != null) { await _isInitializing; return; }  // 진행 중이면 공유
    _isInitializing = InternalInitializeAsync();
    await _isInitializing;
}
```

**결과.** 어느 매니저가 먼저 부르든 초기화·익명 로그인이 정확히 한 번 일어난다. 같은 게이트 철학이 Vivox 초기화([`TeamVoiceChat`](./TeamVoiceChat.md))·Firebase 참조 확보에도 반복 적용됐다.

## 3. MPPM 로비 갱신 누락 — 재시도 큐로 유실 복구

**증상.** 멀티플레이 플레이 모드(MPPM)로 테스트할 때, 로비 플레이어 데이터 갱신이 간헐적으로 누락돼 팀·레디 상태가 클라마다 어긋났다.

**원인.** 코드 주석대로, MPPM 환경에서 싱글턴이 세션을 공유하며 `GetLobbyAsync` 갱신이 빠지는 케이스가 있었다. 로비 변경 이벤트는 왔지만 재조회가 실패로 흘러 로컬 뷰가 갱신되지 않았다.

**해결.** 갱신 실패를 큐에 쌓고 코루틴이 되돌리는 재시도 장치와, 동시 갱신을 막는 재진입 가드를 더했다.

```csharp
private async void UpdateDataHandler() {
    if (_isUpdating) return;                       // 재진입 방어
    _isUpdating = true;
    try { Lobby = await GetLobbyAsync(_lobby.Id); _retryQueue.Clear(); }
    catch { _retryQueue.Enqueue(true); }           // 실패는 큐에 → RetryQueueCoroutine이 재시도
    _isUpdating = false;
}
```

**결과.** 누락된 갱신이 뒤이어 복구돼 로비 상태 정합성이 회복됐다([`LobbyPipeline`](./LobbyPipeline.md)). *트레이드오프:* 이는 근본 원인(싱글턴 세션 공유)의 우회책이며, 0.1초 폴링 코루틴이 상시 도는 비용이 남는다.

## 4. 데미지 판정 권위 — 판정은 서버, 적용은 소유자

**증상.** "누가 데미지를 계산하고 HP를 깎는가"를 정해야 했다. 각 클라가 제각기 판정하면 결과가 어긋나고, 전부 서버로 몰면 소유권 모델과 충돌했다.

**원인.** 탱크 소유권은 운전수 클라에 있고([`NetcodeSyncPatterns`](./NetcodeSyncPatterns.md)), HP는 `writePerm:Owner`라 소유자만 쓸 수 있다. 반면 폭발 범위·거리 감쇠 판정은 공정성을 위해 한 곳에서 해야 했다.

**해결.** 폭발 **판정**은 서버 권위(`SendTo.Server`에서 `SphereCast`+`Mathf.Lerp` 감쇠)로 모으고, 데미지 **적용**(HP 감산)은 소유자 클라가 하도록 나눴다.

```csharp
[Rpc(SendTo.Server, ...)] void DesignatDamageableGroundServerRpc(Vector3 point, PlayerTeamEnum self) {
    // 서버: 범위 탐지 + 거리 감쇠 데미지 산정
    (tc as IDamageableObject).TakeDamaged((int)Mathf.Lerp(dmg, dmg/4, dist/range), self);
}
```

**결과.** 판정의 공정성(서버 단일 산정)과 소유권 모델을 양립시켰다([`ProjectileDamage`](./ProjectileDamage.md)). *미해결 트레이드오프:* HP 적용이 소유 클라라 "서버 완전 권위"가 아니며, 소유 클라가 조작되면 정합성이 흔들린다. 엄격 권위가 필요하면 데미지 적용까지 서버로 옮겨야 한다 — 현재는 의도적으로 반쪽 권위를 택했다.

## 5. 씬 전환 데이터 승계 — 로컬+네트워크 이중 핸드오프

**증상.** 로비에서 편성한 팀 데이터가 인게임 씬으로 넘어가는 과정에서 유실될 위험이 있었다. 씬을 넘으면 로컬 상태가 사라지고, 클라마다 처리 타이밍도 달랐다.

**원인.** 씬 전환은 각 클라에서 일어나는데, 팀 편성은 로비(UGS)에만 있었다. 전환 후 인게임 매니저가 빈 데이터로 시작할 수 있었다.

**해결.** 두 경로로 데이터를 승계했다 — 전환 **전**에 각 클라의 `GameManager`(DDOL로 씬 넘어도 생존)에 로컬 주입(`SetData`)하고, 전환 **후** 서버가 `SetDataClientRpc`로 네트워크 재배포했다.

```csharp
// 전환 전: 로컬 주입 (DontDestroyOnLoad GameManager)
ServiceLocator.Get<IGameManager>().SetData(teams.ToArray(), roomId, mapNumber);
// 전환: 서버 권위 씬 로드
if (IsHost) ServiceLocator.Get<INetworkSceneLoader>().LoadScene("InGame");
// 전환 후: 네트워크 재배포 (SetDataClientRpc)
```

**결과.** 씬 경계를 사이에 둔 데이터 승계가 이중으로 보장됐다([`LobbyPipeline`](./LobbyPipeline.md)·[`Bootstrap`](./Bootstrap.md)). *남은 과제:* 모든 클라가 전환 전 `SetData`를 마쳤다는 타이밍 전제에 의존해, 진입 배리어(전원 준비 확인)가 있으면 더 견고하다.

## 6. 비정상 종료 정리 — Firebase OnDisconnect 서버 자동 삭제

**증상.** 클라이언트가 크래시·강제 종료되면 Firebase에 유령 유저·유령 방 데이터가 남았다. 정상 종료 경로의 정리 코드는 이런 경우 실행되지 못했다.

**원인.** 데이터 정리를 클라이언트의 종료 코드에 의존했기 때문이다. 비정상 경로(크래시, 네트워크 단절, 호스트 다운)에서는 그 코드가 돌지 않는다.

**해결.** Firebase `OnDisconnect`로 "이 연결이 끊기면 서버가 이 값을 지워라"를 미리 서버에 예약했다. 정리 책임을 클라가 아닌 백엔드 서버로 넘겼다.

```csharp
_db.RootReference.Child($"{ChildKey.USERS}/{userId}").OnDisconnect().RemoveValue();
_db.RootReference.Child($"{ChildKey.ROOMS}/{roomId}").OnDisconnect().RemoveValue();
```

**결과.** 클라가 어떻게 죽든 유저·방 데이터가 서버 측에서 자동 정리돼 유령 데이터가 사라졌다([`FirebaseBackend`](./FirebaseBackend.md)). *남은 과제:* Vivox 채널·Relay 세션 등 Firebase 밖 자원의 비정상 종료 정리는 여전히 별도 처리가 필요하다.

## 공통 교훈

- **프레임워크가 주는 신호를 재해석하라** — 호스트 다운(케이스 1)처럼, 없는 이벤트를 만들기보다 이미 오는 콜백을 판별식으로 승격하는 편이 가볍고 견고했다.
- **비동기 경쟁은 게이트로 직렬화하라** — 초기화 중복(케이스 2)·갱신 재진입(케이스 3)은 단일 `Task`/플래그 게이트로 반복해 해결됐다. 같은 패턴이 프로젝트 전반에 재사용됐다.
- **권위의 소재를 명시적으로 정하라** — 데미지(케이스 4)·데이터 승계(케이스 5)처럼, "누가 진리를 갖는가"를 값 단위로 결정하는 것이 멀티플레이 정합성의 핵심이었다.
- **정리 책임을 인프라로 넘겨라** — 비정상 종료(케이스 6)는 클라 코드로 감당하기 어려운 문제였고, 서버 측 자동 정리(`OnDisconnect`)가 근본적이었다.

## 남은 과제 (정직한 한계)

- **반쪽 서버 권위** — HP·점수가 `writePerm:Owner`라 서버 완전 권위가 아니다(케이스 4). 조작 방어가 필요한 경쟁 환경이면 판정·적용을 모두 서버로 옮겨야 한다.
- **우회책의 상시 비용** — MPPM 재시도 큐(케이스 3)는 근본 원인 미해결 상태의 우회이며, 폴링 비용이 남는다.
- **타이밍 전제 의존** — 데이터 승계(케이스 5)·이벤트 스케줄([`MapEventScheduler`](./MapEventScheduler.md))이 "제때 처리됐다"는 전제 위에서 동작한다. 진입 배리어·상태 동기 확인이 있으면 더 견고하다.
- **크로스 자원 정리 미비** — 비정상 종료 시 Relay/Vivox 세션 정리는 Firebase 정리(케이스 6)만큼 자동화돼 있지 않다.
