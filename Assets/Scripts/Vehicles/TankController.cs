using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class TankController : NetworkBehaviour, IDamageableObject, IWindowViewer
{
    [SerializeField] private PlayerableStatisticsSO _stat;
    [SerializeField] private ProjectileManager _projectileManager;
    [SerializeField] private Material[] _materials;
    [SerializeField] private VehicleMovement _movement;
    [SerializeField] private VehicleTurret _turret;
    [SerializeField] private MeshRenderer _turretRenderer;
    [SerializeField] private MeshRenderer _canonRenderer;
    //현재 UI
    private PlayerTeamEnum _teamNum;
    
    // 현재 HP
    private NetworkVariable<int> _hp = new(0, writePerm: NetworkVariableWritePermission.Owner); 
    private NetworkVariable<bool> _isAlive = new(true, writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> _isEnd = new(writePerm: NetworkVariableWritePermission.Owner);
    // 공격 쿨다운 (일단 일반변수로 가보자)

    private Material _material;
    private MeshRenderer _meshRenderer;
    private BoxCollider _collider;
    
    private bool _isDamageable;

    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _collider = GetComponent<BoxCollider>();
        _teamNum = PlayerTeamEnum.neutralObject;
    }

    private void OnEnable()
    {
        OnSpawnProcess();
    }

    public override void OnNetworkSpawn()
    {
        _isAlive.OnValueChanged += SpawnControl;
        _isEnd.OnValueChanged += (_, _) => DestoryOnNetwork();
    }

    private void DestoryOnNetwork()
    {
        var userInfo = ServiceLocator.Get<IUserInfoManager>().GetUserInfo();
        if (_teamNum != userInfo.teamNum && userInfo.role != PlayerRole.Driver) return;
        var ngo = GetComponent<NetworkObject>();
        ngo.Despawn();
    }

    [ClientRpc]
    public void SetDataClientRpc(PlayerTeamEnum teamNum, Vector3 pos)
    {
        Debug.Log($"[TankController] Set Data ...");
        _teamNum = teamNum;
        Debug.Log($"[TankController] Set Data ... {_teamNum}");
        _material = _materials[(int)teamNum];
        Debug.Log($"[TankController] Set Data ... {_material.name}");
        Init(pos);
    }
    
    //탱크 초기화 함수
    private void Init(Vector3 pos)
    {
        Debug.Log("[TankController] Init Tank ... ");
        var userInfo = ServiceLocator.Get<IUserInfoManager>().GetUserInfo();
        Debug.Log($"[TankController] Init Tank ... My team is {_teamNum}");
        Debug.Log($"[TankController] Init Tank ... Change Color {_material.name}");
        GetComponent<MeshRenderer>().material = _material;
        _turretRenderer.material = _material;
        _canonRenderer.material = _material;
        if (_teamNum == userInfo.teamNum && userInfo.role == PlayerRole.Driver)
            _hp.Value = _stat.VechicleMaximumHP;
        var teamInfo = ServiceLocator.Get<IGameManager>().GetMyTeamInfo(_teamNum);
        _movement.SetDriverData(_stat, teamInfo);
        _turret.SetGunnerData(_stat, teamInfo);
        transform.position = pos;
        Debug.Log($"[TankController] Init Tank ... position ; {pos} => {transform.position}");
        Debug.Log("[TankController] Init Tank ... Completed");
    }

    private void SpawnControl(bool oldVal, bool newVal)
    {
        _collider.enabled = newVal;
        _meshRenderer.enabled = newVal;
        if (newVal) OnSpawnProcess();
    }
    
    private void OnSpawnProcess()
    {
        StartCoroutine(ChangeDamagableCoroutine());
    }

    private IEnumerator ChangeDamagableCoroutine()
    {
        _isDamageable = false;
        yield return new WaitForSeconds(3f);
        _isDamageable = true;
    }

    public void TakeDamaged(int dmg, PlayerTeamEnum enemy)
    {
        if (!_isDamageable) return;
        Debug.Log($"_hp : {_hp} , dmg : {dmg}");
        var user = ServiceLocator.Get<IUserInfoManager>().GetUserInfo();
        if (_teamNum == user.teamNum && user.role == PlayerRole.Driver)
            _hp.Value -= dmg;
        if (_hp.Value <= 0)
        {
            _isAlive.Value = false;
            ServiceLocator.Get<IGameManager>().OnDestoryVehicleServerRpc(_teamNum, enemy);
        }
    }
    
    [ClientRpc]
    public void GameEndProcessClientRpc()
    {
        var userInfo = ServiceLocator.Get<IUserInfoManager>().GetUserInfo();
        if (_teamNum == userInfo.teamNum && userInfo.role == PlayerRole.Driver)
            _isEnd.Value = true;
    }
    
    [ClientRpc]
    public void RespawnClientRpc(Vector3 pos)
    {
        var userInfo = ServiceLocator.Get<IUserInfoManager>().GetUserInfo();
        if (_teamNum == userInfo.teamNum && userInfo.role == PlayerRole.Driver)
        {
            transform.position = pos;
            _isAlive.Value = true;
        }
    } 

    public void ExplosionDamaged(System.Numerics.Vector3 expsPos, int dmg, PlayerTeamEnum enemy)
    {
        
    }

    public void ViewEffectControl(bool enable)
    {
        Debug.Log($"[TankController] View Effect Control ... {enable}");
    }
}