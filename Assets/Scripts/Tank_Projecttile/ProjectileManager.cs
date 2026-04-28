using System.Net.NetworkInformation;
using Unity.Netcode;
using UnityEngine;

public class ProjectileManager : NetworkBehaviour
{
    // raycast, 투사체 속도, 피해량, 범위, 범위별 피해 계수, 투사체 최대 비행 거리(사거리)
    [SerializeField] Camera _mainCam;
    [SerializeField] PlayerableStatisticsSO _vechicleSO;
    [SerializeField] LayerMask _damageableObject;

#if UNITY_EDITOR
    [Header("디버깅용")]
    [Tooltip("적중된 대상의 이름을 로그에 작성함.")][SerializeField] bool _onDebugLog;
#endif

    Ray _ray;
    RaycastHit _targetPoint;
    RaycastHit[] _hitedTargets;

    public override void OnNetworkSpawn()
    {

    }

    void Start()
    {
        _hitedTargets = new RaycastHit[5]; // 현재 게임 내에서 5개가 검출될 일은 없을 거라고 생각하지만, 설계 실수를 검출하기 위해 5개로 설정
        _ray = new Ray(_mainCam.transform.position, _mainCam.transform.forward);
    }

    public void Init(PlayerableStatisticsSO so) => _vechicleSO = so;

    // 외부에서 호출될 코드로 호출 시 Raycast 기반으로 사격 지점과 거리를 받아온 후 해당 지점에 거리 비례 시간 후에 피해를 입히는 방식으로 작동하면 될 거 같다는 생각.
    public void Shot()
    {
        Debug.Log("Shot!");

        if (_vechicleSO == null)
        {
            Debug.LogError("PlayerableStatisticsSO가 존재하지 않습니다.");
            return;
        }
        _ray = new Ray(_mainCam.transform.position, _mainCam.transform.forward);
        // raycast로 메인 카메라(사수의 카메라)의 중심(사격점)을 기준으로 가장 처음 닿은 위치(물체의 위치로 받으면 아마 물체의 중심을 받게 될거임.)를 받아온 후 DeignatDamageableGround 호출
        if (Physics.Raycast(_ray, out _targetPoint, _vechicleSO.ProjectileMaximumDinstance))
        {
            Debug.Log(_targetPoint.point);
            DesignatDamageableGroundServerRpc(_targetPoint.point);
        }
    }

    private void OnDrawGizmos()
    {
        if (_vechicleSO == null)
            return;
        Gizmos.color = Color.aquamarine;
        Gizmos.DrawLine(_mainCam.transform.position, _mainCam.transform.forward * _vechicleSO.ProjectileMaximumDinstance + _mainCam.transform.position);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_targetPoint.point, _vechicleSO.ProjectileMaximumDamageableRange);
    }

    [ServerRpc] // TODO : 서버측으로 잘 보내졌는 지 확인 필요 및 서버 부하 테스트 필요
    private void DesignatDamageableGroundServerRpc(Vector3 point)
    {
        // 지정된 위치에 구형 범위를 측정 및 일정 시간(짧은 시간) 후에 범위 안에 들어간 객체(피해를 입을 수 있는 객체)들 판정( 판정할 때 조심해야될 부분이 닿은 부위를 기준으로 해야됌, 중심으로 받으면 안됌)
        int count = Physics.SphereCastNonAlloc(
            point,
            _vechicleSO.ProjectileMaximumDamageableRange,
            Vector3.forward,
            _hitedTargets,
            0.001f,
            _damageableObject);

        if (count < 1 || _hitedTargets == null) return;

        for (int i = 0; i < count; i++)
        {
#if UNITY_EDITOR
            if (_onDebugLog)
            {
                Debug.Log($"검출된 대상 : {_hitedTargets[i].collider.name}");
                Debug.Log($"폭발 중심지에서 대상까지의 거리 : {Vector3.Distance(_hitedTargets[i].collider.ClosestPoint(point), point)}");
            }
#endif
            (_hitedTargets[i].collider.GetComponent<testTank>() as IDamageableObject)
            .TakeDamaged(
                    (int)Mathf.Lerp( // 거리에 따라 피해를 다를 게 주기 위해(선형 보간 처리를 위해) Mathf.Lerp로 처리
                        _vechicleSO.ProjectileDamage,
                        (_vechicleSO.ProjectileDamage / 4),
                        Vector3.Distance(_hitedTargets[i].collider.ClosestPoint(point), point) / _vechicleSO.ProjectileMaximumDamageableRange)); // 폭심지를 기준으로 콜라이더의 접촉부위 중 가장 가까운 지점과 거리 비교 후 피해량 측정
            Debug.Log($"TakeDamage: {_vechicleSO.ProjectileDamage} , {_vechicleSO.ProjectileDamage / 4} , {Vector3.Distance(_hitedTargets[i].collider.ClosestPoint(point), point) / _vechicleSO.ProjectileMaximumDamageableRange}");
        }
    }
}
