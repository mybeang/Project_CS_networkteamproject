# LobbyConcept

## 기능

- 방 목록 확인
  - [관련 문서](https://docs.unity.com/ko-kr/lobby/query-for-lobbies) 
  - 설명: 현재 생성된 방 전체에 대한 확인
  - 필요 데이터
    - Lobby ID: 방 ID
    - Lobby name: 방제목
    - Creator: 방 생성자; host user ID
    - Cur/Total: 방 참여자 수 / 최대 참여자 수
- 방 생성
  - [관련 문서](https://docs.unity.com/ko-kr/lobby/create-a-lobby) 
  - 설명: 방 만들기 기능. Relay 환경이기 때문에 Join Code 활성 필요.
  - 필요 데이터
    - Lobby name: 방 제목
    - Lobby visility: (공개 고정)
    - Lobby size: 최대 수용 인원 (8 고정)
    - Host User Data
      - User ID, Team, 운전수/포수    
- 방 참여
  - [관련 문서](https://docs.unity.com/ko-kr/lobby/join-a-lobby) 
  - 설명: 방 참여.
  - 필요 데이터
    - Lobby ID: Generated Lobby ID
    - Join User Data
      - User ID, Team, 운전수/포수 
- 방 떠나기
  - [관련 문서](https://docs.unity.com/ko-kr/lobby/leave-a-lobby) 
  - 설명: 해당 방을 떠날 때 필요한 기능
    - 방장일 경우
      - 다른 유저에게 방장 권한 위임 -> 확인해보기 -> 자동으로 됨(Lobby Leave 관련 문서 내 확인됨)
      - 나가고 초기화가 필요한 데이터 초기화 해주기.
    - 방장이 아닐 경우
      - 나가고 초기화가 필요한 데이터 초기화 해주기.
  - 필요 데이터
    -  Player ID; UnityAuth 에서 제공하는 Player ID. `lobby.Players` 로 접근 가능.
- 게임 시작
  - GameManager 에게 전달할 데이터
    - 선택된 유저의 Team 및 Role 데이터 업데이트
    - 선택된 맵 데이터 
  - Relay 관련 Logic 실행
    - Relay Host Start 를 통해 JoinCode 획득
    - 획득한 JoinCode 를 통해 Relay Client Start 시작 

## 데이터; 전역 관리 데이터

-  JoinCode ; NetworkVariable ; fixedstring

## 기타

- Host 는 주기적으로 heartbeat 를 보내야함 -> 폭파 당하지 않기 위함
