using System.Net.NetworkInformation;
using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    [SerializeField] Camera _mainCam;
    //[SerializeField] PlayerableStatisticsSO 깃 Feth 미진행 후 브랜치 생성으로 인한 후에 재처리 필요.

    // raycast, 투사체 속도, 피해량, 범위, 범위별 피해 계수, 투사체 최대 비행 거리(사거리)

    [SerializeField] float _maxProjectileDistance = 10; // 임시 작업.
    [SerializeField] LayerMask _damageableObject;

    [Header("디버깅용")]
    [SerializeField] bool _onDebugLog;

    Ray _ray;
    RaycastHit _targetPoint;
    Collider[] col;

    void Start()
    {
        col = new Collider[5]; // 현재 게임 내에서 5개가 검출될 일은 없을 거라고 생각하지만, 설계 실수를 검출하기 위해 5개로 설정
        _ray = new Ray(_mainCam.transform.position, _mainCam.transform.forward);
    }

    // 외부에서 호출될 코드로 호출 시 Raycast 기반으로 사격 지점과 거리를 받아온 후 해당 지점에 거리 비례 시간 후에 피해를 입히는 방식으로 작동하면 될 거 같다는 생각.
    public void Shot()
    {
        _ray = new Ray(_mainCam.transform.position, _mainCam.transform.forward);
        // raycast로 메인 카메라(사수의 카메라)의 중심(사격점)을 기준으로 가장 처음 닿은 위치(물체의 위치로 받으면 아마 물체의 중심을 받게 될거임.)를 받아온 후 DeignatDamageableGround 호출
        if (Physics.Raycast(_ray, out _targetPoint, _maxProjectileDistance))
        {
            Debug.Log(_targetPoint.point);
            DesignatDamageableGround(_targetPoint.point);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.aquamarine;
        Gizmos.DrawLine(_mainCam.transform.position, _mainCam.transform.forward * 10 + _mainCam.transform.position);

        Gizmos.DrawWireSphere(_targetPoint.point,5);
    }

    private void DesignatDamageableGround(Vector3 point)
    {
        // 지정된 위치에 구형 범위를 측정 및 일정 시간(짧은 시간) 후에 범위 안에 들어간 객체(피해를 입을 수 있는 객체)들 판정( 판정할 때 조심해야될 부분이 닿은 부위를 기준으로 해야됌, 중심으로 받으면 안됌)
        int count = Physics.OverlapSphereNonAlloc(point, 5, col, _damageableObject);

        foreach (var obj in col)
        {
            if (obj == null) return;
#if UNITY_EDITOR
            if (_onDebugLog)
                Debug.Log($"검출된 대상 : {obj.name}");
#endif
                (obj.GetComponent<teststCode>() as IDamageableObject).TakeDamaged(10);
            // TODO : Mathf.Lerp(100, 25, 현재 거리 / 최대 거리) 해당 코드 후에 10 대신 추가 필요. 
        }
    }
}
