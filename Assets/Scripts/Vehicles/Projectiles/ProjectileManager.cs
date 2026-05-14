using System.Collections;
using System.Net.NetworkInformation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class ProjectileManager : NetworkBehaviour
{
    // raycast, 투사체 속도, 피해량, 범위, 범위별 피해 계수, 투사체 최대 비행 거리(사거리)
    [SerializeField] private PlayerableStatisticsSO _vechicleSO;
    [SerializeField] private LayerMask _damageableObject;
    [SerializeField] [Range(0.01f, 0.1f)] private float _waitTime;
    [SerializeField] private TargetRabbit _targetRabbit;
    [SerializeField] private Transform _shotVfxPos;
    [SerializeField] private GameObject _shotVfxPrefab;
    
    private Ray _ray;
    private RaycastHit _targetPoint;
    private RaycastHit[] _hitedTargets;

    public override void OnNetworkSpawn()
    {

    }

    private void Start()
    {
        _hitedTargets = new RaycastHit[5]; // 현재 게임 내에서 5개가 검출될 일은 없을 거라고 생각하지만, 설계 실수를 검출하기 위해 5개로 설정
    }

    public void Init(PlayerableStatisticsSO so) => _vechicleSO = so;

    private IEnumerator DelayExplosionCoroutine(PlayerTeamEnum self, Vector3 hitPosition)
    {
        var distance = Vector3.Distance(hitPosition, transform.position);
        yield return new WaitForSeconds(_waitTime * distance);
        DesignatDamageableGroundServerRpc(_targetPoint.point, self);
    }

    [ClientRpc(InvokePermission = RpcInvokePermission.Everyone)]
    public void ShotVfxPlayClientRpc()
    {
        Instantiate(_shotVfxPrefab, _shotVfxPos.position, _shotVfxPos.rotation);
    }
    
    // 외부에서 호출될 코드로 호출 시 Raycast 기반으로 사격 지점과 거리를 받아온 후 해당 지점에 거리 비례 시간 후에 피해를 입히는 방식으로 작동하면 될 거 같다는 생각.
    public void Shot(Transform shotPos, PlayerTeamEnum self)
    {
        Debug.Log("[ProjectileManager] Shot!");
        if (_vechicleSO == null)
        {
            Debug.LogError("[ProjectileManager] PlayerableStatisticsSO가 존재하지 않습니다.");
            return;
        }
        _ray = new Ray(shotPos.position, shotPos.forward);
        // raycast로 메인 카메라(사수의 카메라)의 중심(사격점)을 기준으로 가장 처음 닿은 위치(물체의 위치로 받으면 아마 물체의 중심을 받게 될거임.)를 받아온 후 DeignatDamageableGround 호출
        if (Physics.Raycast(_ray, out _targetPoint, _vechicleSO.ProjectileMaximumDinstance))
        {
            Debug.Log($"[ProjectileManager] {_targetPoint.point}");
            StartCoroutine(DelayExplosionCoroutine(self, _targetPoint.point));
        }
    }

    private void OnDrawGizmos()
    {
        if (_vechicleSO == null)
            return;
        //Gizmos.color = Color.aquamarine;
        //Gizmos.DrawLine(_mainCam.transform.position, _mainCam.transform.forward * _vechicleSO.ProjectileMaximumDinstance + _mainCam.transform.position);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_targetPoint.point, _vechicleSO.ProjectileMaximumDamageableRange);
    }

    //[ServerRpc] // TODO : 서버측으로 잘 보내졌는 지 확인 필요 및 서버 부하 테스트 필요
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DesignatDamageableGroundServerRpc(Vector3 point, PlayerTeamEnum self)
    {
        // Boom Effect 추가 필요
        ControlRabbitClientRpc(true, point);
        // 지정된 위치에 구형 범위를 측정 및 일정 시간(짧은 시간) 후에 범위 안에 들어간 객체(피해를 입을 수 있는 객체)들 판정( 판정할 때 조심해야될 부분이 닿은 부위를 기준으로 해야됌, 중심으로 받으면 안됌)
        int count = Physics.SphereCastNonAlloc(
            point,
            _vechicleSO.ProjectileMaximumDamageableRange,
            Vector3.forward,
            _hitedTargets,
            0.001f,
            _damageableObject);

        Debug.Log($"[ProjectileManager] 적중 위치 기반 탐지 된 대상 : {count}");

        if (count < 1 || _hitedTargets == null) return;
        
        for (int i = 0; i < count; i++)
        {
            Debug.Log($"[ProjectileManager] 검출된 대상 : {_hitedTargets[i].collider.name}");
            Debug.Log($"[ProjectileManager] 폭발 중심지에서 대상까지의 거리 : {Vector3.Distance(_hitedTargets[i].collider.ClosestPoint(point), point)}");
            var tc = _hitedTargets[i].collider.GetComponent<TankController>();
            // 폭심지를 기준으로 콜라이더의 접촉부위 중 가장 가까운 지점과 거리 비교 후 피해량 측정
            // 거리에 따라 피해를 다를 게 주기 위해(선형 보간 처리를 위해) Mathf.Lerp로 처리
            var damage = _vechicleSO.ProjectileDamage;
            var dmgRange = _vechicleSO.ProjectileMaximumDamageableRange;
            var distance = Vector3.Distance(_hitedTargets[i].collider.ClosestPoint(point), point);
            (tc as IDamageableObject).TakeDamaged((int)Mathf.Lerp(damage, damage / 4, distance / dmgRange), self);
            Debug.Log($"[ProjectileManager] TakeDamage: {damage}, {damage / 4}, {distance / dmgRange}");
            
            // 폭발에 따른 물리 효과
            var ep = _vechicleSO.projectileExplosionPower;
            var mr = _vechicleSO.ProjectileMaximumDamageableRange;
            var eu = _vechicleSO.projectileExplosionUpper;
            (tc as IImpactForce).ImpactPhysic(ep, point, mr, eu);
            Debug.Log($"[ProjectileManager] ImpactPhysic: {ep}, {mr}, {eu}");
        }
        ControlRabbitClientRpc(false, point);
    }

    [ClientRpc(InvokePermission = RpcInvokePermission.Everyone)]
    private void ControlRabbitClientRpc(bool active, Vector3 hitPosition)
    {
        _targetRabbit.gameObject.transform.position = hitPosition;
        if (active) _targetRabbit.BoomStart();
        else _targetRabbit.BoomStop();
    }
}
