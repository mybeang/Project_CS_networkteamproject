# Managers

- Manager 는 기본적으로 DontDestoryOnLoad Object 로 한다.
- 이는 Manager class 를 상속받는 것으로 한다.
- Manager class 상속시, service locater 에 등록 하는 함수가 포함되어 있다.
  - 이를 OnEnable/OnDisable Event 함수를 이용하여 등록한다.
- 등록 후 실제 사용 예시는 아래와 같다.
```csharp
ServiceLocater.Get<IAudioService>()?.PlaySfx(someSfxClip);
```