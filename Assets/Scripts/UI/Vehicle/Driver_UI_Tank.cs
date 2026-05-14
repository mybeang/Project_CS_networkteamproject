using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Driver_UI_Tank : Driver_UI
{
    private void OnEnable()
    {
        ServiceLocator.Get<IGameManager>().AddTimerHandler(ChangeTime);
    }

    private void OnDisable()
    {
        ServiceLocator.Get<IGameManager>().RemoveTimerHandler(ChangeTime);
    }
    /// <summary>
    /// 탱크가 피해를 입었을 시 해당 함수 호출 및 백분율로 현재 체력 표시
    /// 반드시 넘겨 줄때 float 값으로 넘겨 줄것(int / int 하면 0 나올 수도 있음)
    /// 예시
    /// 조건 : 1000 체력일때 90의 피해를 받은 경우
    /// TakeDamage( 910 / 1000 ) 혹은 TakeDamag( 0.91 )
    /// </summary>
    /// <param name="currentHealthPoint"></param>
    public override void ChangeVehicleHealth(float currentHealthPoint)
    {
        _hpSlider.value = currentHealthPoint;
    }

    private void ChangeTime(double oldVal, double newVal)
    {
        // ToDO. Hardcoding
        int time = 600 - (int)newVal;
        Debug.Log($"[Driver_UI_Tank] ChangeTime ... {time}");
        _timer.text = $"{time / 60} : {time % 60}";
    }

    public override void UpdateKillLog(PlayerTeamEnum self, PlayerTeamEnum enemy)
    {
        _killLog.text = $"{enemy} 팀이 {self} 팀을 박살냈습니다!";
    }
}
