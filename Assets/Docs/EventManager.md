# 맵 기믹 제작 설명 문서
## 설계 구조
- GameManager
- ㄴEventScheduleManager
-   ㄴEventTask

## 설계 방식
- EventTask를 추상화 Class로 만들어서 상속 받아서 즉시 사용 가능하게 만들었습니다.
- EventScheduleManager는 EventTask를 관리하는 관리자로 생성 후 EventTask만 생성하면 됩니다.
- GameManager는 특별하게 건드릴 일 없습니다.

## 사용 방법
- 맵 내부에 EventScheduleManager를 생성 후 하위로 EventTask를 상속 받은 객체를 생성하면 됩니다.
- 위치는 상관 없습니다. 자동으로 GameManager에 등록 됩니다.

1. 만든 맵 하위 객체로 EventScheduleManager를 생성합니다.
2. 편의상 EventScheduleManager 하위로 기믹 객체를 생성한 후 Inspecter 혹은 코드 내부에서 excuteTime를 double[] 형태로 초기화 합니다.
   - 예시 : 2분 마다 기믹을 시전하고 싶다 -> excuteTime = new double[] {120,240,360,480,600} 필요시 더 추가
3. 기믹 객체를 EventScheduleManager Inspecter에 event에 끌어다가 놓습니다.

### EventTask
- Script(이하 기믹 객체)를 생성 후 EventTask를 상속 받습니다.
- excuteTime : 원하는 횟수 만큼 배열의 크기를 지정 후 초단위로 기입합니다.
  - 예시
    - 1분 31.5초에 작동 시키고 싶은 경우 => new double[] { 151.5 }
  - 주의 사항
    - tick 시간이 0.1초 이긴 합니다만. 호스트의 컴퓨터 또는 서버 상태에 따라 시간이 밀릴 수 있습니다.
- stopTriggerTime : 특정 이벤트의 경우 일정 시간만큼만 작동 시키고 싶을 수 있기 때문에 존재하며, 필요하지 않은 경우 무시합니다.
  - 예시
    - 1분 31.5초 후에 중지 시키고 싶으 경우 => new double[] {151.5}
- eventName : 단순 식별용입니다. 필요하지 않은 경우 무시합니다.
- public override void OnEventDespawn() : stopTriggerTime과 같은 사유로 존재하며, 필요하지 않은 경우 무시합니다.
- public override void OnEventSpawn() : 이벤트 호출 시 호출 되는 함수로 구현을 해당 함수에서 진행한 후 아래 주의 사항과 같이 ClientRcp를 붙인 함수를 호출 합니다.
- public override void OnNetworkSpawn() : NetworkBehaviour 함수로 Rcp 사용을 위해 반드시 기입합니다.


- **주의 사항** : 추상화에서 Rcp를 선언할 수 없기 때문에. 하단 가이드에 맞춰서 작업해주시면 됩니다.
  - 메테오를 생성할 때 무작위 위치(Vector3)에 떨구고 싶은 경우
  - 코드에서 위치를 저장한 후 원하는 함수를 선언 및 **ClientRpc를 붙여서 배포**하셔야합니다.
  - 호출은 서버에서만 호출 되기 때문에 큰 문제는 없을 수 있으나, 예외처리는 언제나 환영입니다.

### EventScheduleManager
- 특별하게 건드릴 일 없으며, 위에 사용 방법에서 명시된 내용처럼 EventTask를 상속 받은 객체를 참조해주시면 됩니다.
- InergrityCheck() : 필수 변수의 누락을 방지합니다.
- GetTimer() : EventTask에서 초기화한 executeTime를 불러옵니다.
- GetStopTimer() : EventTask에서 초기화한 stopTriggerTime를 호출하며, 초기화 되지 않은 경우 null을 반환합니다.
- OnEventSpawnServerRpc() : ServerRpc로 GameManager에서 호출합니다.
- OnEventDespawnServerRpc() : ServerRpc로 GameManager에서 호출합니다.