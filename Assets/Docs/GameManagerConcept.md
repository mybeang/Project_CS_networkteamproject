# GameManager.cs

## 파일 위치
```
Interface: Assets/Scripts/Interfaces/Managers/IGameManager.cs
Class    : Assets/Scripts/Managers/GameManager.cs
```
## 역할

- 게임의 전반적인 흐름에 대한 제어
- Room 으로 부터 받은 User 들의 기본 데이터 관리
- 기본적으로 Host 만 제어 가능 
  - 단, Function 실행은 Client 가 될 수 있다. (ServerRPC/ClientRPC 모두 가능) 

## 기능
### 게임 시작

- 유저별 음성 채널 가입
- Player Prafab 확정 및 배치
  - 팀별 Player 배치
  - 팀내 Player 배치; 포수 & 운전수
- 게임 시간 제어 시작 ( 10분 )

### 게임 중간

- User Score Update 
  - User 사망시 서버측으로 Score 데이터 업데이트 요청 함
  - 당연하지만, Client 도 자신 UserInfo 내 Score 업데이트 해야 함
- 각 Client 의 UI 에서 User Score 확인 기능
- 각 Client 의 UI 에서 Kill Log 를 띄워주는 기능
- User 사망시 Respawn 관리

### 게임 종료

- 게임 종료 조건: 게임 시간이 0 일 경우 게임 종료.
- 유저별 음성 채널 탈퇴
- 네트워크 씬 로드를 이용한 `게임 결과 화면`으로의 전환

## 필요 데이터

- 모든 팀에 대한 정보
  - User ID / Score 정보 포함.
- 게임 시간 (NetworkVariable 사용 유력)

## 기타 

- User Score Update 방법
  - RPC 활용; `RpcDelivery.Reliable` 기능 활용할 것.