using System.Collections.Generic;
using UnityEngine;

// 모닥불 범위를 감지하는 스크립트
// 모닥불 오브젝트마다 이 스크립트를 붙이고, Collider + isTrigger를 설정해야 함
public class BonfireArea : MonoBehaviour
{
    // 현재 모닥불 범위 안에 있는 탱크들을 저장하는 리스트
    private List<TankController> _tanksInArea = new List<TankController>();

    // 탱크가 모닥불 범위에 들어왔을 때 호출
    private void OnTriggerEnter(Collider other)
    {
        // 충돌한 오브젝트에서 TankController 컴포넌트를 가져온다
        TankController tank = other.GetComponent<TankController>();

        // TankController가 존재하고, 리스트에 아직 없다면 추가
        if (tank != null && !_tanksInArea.Contains(tank))
        {
            _tanksInArea.Add(tank);
        }
    }

    // 탱크가 모닥불 범위에서 나갔을 때 호출
    private void OnTriggerExit(Collider other)
    {
        TankController tank = other.GetComponent<TankController>();

        // TankController가 존재하고, 리스트에 있다면 제거
        if (tank != null && _tanksInArea.Contains(tank))
        {
            _tanksInArea.Remove(tank);
        }
    }

    // BlizzardEvent에서 특정 탱크가 모닥불 안에 있는지 확인할 때 호출하는 메서드
    public bool IsTankInArea(TankController tank)
    {
        return _tanksInArea.Contains(tank);
    }
}