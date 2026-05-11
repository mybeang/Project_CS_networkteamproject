# 초원 맵 기믹 설계

## 설계 의도

탱크가 워프 포인트 구간에 진입할 시 랜덤으로 다른 포인트에 워프하는 구조로 설계할 예정

## 필요한 데이터

현재 워프 포인트 자식 오브젝트에 각각의 개별 오브젝트가 존재함

W1,W2... 같은 하위 오브젝트

### 해당 오브젝트는 Collider를 갖고있음
 
그리고 isTrigger가 체크되어 있음

그렇다면?

- OnTriggerEnter -> 충돌될 때 1회
- OnTriggerExit -> 빠져나올 때 1회

### 워프 포인트에 진입시 랜덤

내가 워프 포인트에 도달했을 때 도달한 워프 포인트를 제외한 나머지 포인트에

랜덤하게 워프되어야 함.

Random 함수의 필요?

조건문을 사용하여

```
if (만약 워프포인트에 진입했다면?)
{
    다른 포인트로 랜덤하게 워프 동작
}
```

만약 랜덤하게 워프가 되고나면, 해당 포인트에 워프될 시 트리거에 닿아 연쇄적으로 워프작용이 일어나게 됨

이것에 대한 해결방안은 bool타입의 변수를 선언하여

ex) 

```csharp
bool _isWarp

private void OnTriggerEnter(Collider other)
{
    if (_isWarp) // 워프가 가능한지 판별하여
    {
        워프 실행
    }
}

private void OnTriggerExit(Collider other)
{
    if (!_isWarp) // 이미 워프가 되었다면?
    {
        _isWarp를 true로 바꿔준다.
    }
}
```
이런 식으로 설계방향을 가져간다면 좋을 것 같음.

워프 스크립트는 탱크가 갖고 있는게 좋을 듯하다.

워프의 트리거 역할은 워프 객체가 하고 있어야 하지만

가능한지 판별하는 bool 플래그를 들고 스스로가 판단되는 건 결국 탱크라서

그 설계방향으로 가야함 쿨타임을 적용하는 방식 대신에 bool 타입변수가 더 편리할 듯

### W1 ~ W9 위치를 탱크에게 적용

워프 포인트 9곳의 정보를 탱크가 알고있어야지만 해당 포인트의 위치를 알고 갈 수 있음

탱크와 맵의 워프 포인트 모두 프리팹으로 이루어져 있고, 런타임 중 실행되어야 하기 때문에 Hierarchy상에서 갖고올 수 있는 GameObject.Find()를 사용한다.

EX)

```csharp
Transform _warpPoints;

List<Transform> _warps = new List(9);

private void Awake()
{
    OnWarpSearch();
}

private void OnWarpSearch()
{
    _warpPoints = GameObject.Find("WarpPoints").transform;

    foreach (Transform obj in _warpPoints)
    {
       _warps.Add(obj)
    }
}
```




### 멀티플레이 동기화

Networktransform의 키워드 확인 -> 각 플레이어별 위치가 공유될 수 있는 동기화

그리고 위치 이동을 클라이언트가 할지, 서버가 할지 고려해야함

그에 대한 방안으로

서버에서 호출하고, 클라이언트에서 실행되도록

서버가 해당 탱크를 워프시켜야겠다고 판단, 클라이언트RPC를 호출한다.

그리고 모든 클라이언트는 해당 워프될 탱크의 포지션을 변경하고

그렇게 된다면 모든 플레이어 화면에서 탱크가 같은 위치에 나타날 것.

이에 대한 참고 스크립트는 GameManager.cs의 ReSpawnVehicleClientRpc 로직 부분참고