using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Driver_UI_Viewer : MonoBehaviour
{
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private TextMeshProUGUI _killLog;

    /// <summary>
    /// 탱크가 피해를 입었을 시 해당 함수 호출 및 백분율로 현재 체력 표시
    /// 반드시 넘겨 줄때 float 값으로 넘겨 줄것(int / int 하면 0 나올 수도 있음)
    /// 예시
    /// 조건 : 1000 체력일때 90의 피해를 받은 경우
    /// TakeDamage( 910 / 1000 ) 혹은 TakeDamag( 0.91 )
    /// </summary>
    /// <param name="currentHealthPoint"></param>
    public void ChangeVehicleHealth(float currentHealthPoint)
    {
        _hpSlider.value = currentHealthPoint;
    }

    
    /// <summary>
    /// KillLog에 표시 방식
    /// B 팀이 A팀을 파괴 했습니다\n
    /// D 팀이 C팀을 파괴 했습니다.
    /// </summary>
    /// <param name="log"></param>
    public void ChangeKillLog(string log)
    {
        _killLog.text = log;
    }
}
