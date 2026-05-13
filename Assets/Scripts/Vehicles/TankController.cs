using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TankController : NetworkBehaviour, IDamageableObject, IWindowViewer
{
    [SerializeField] private PlayerableStatisticsSO _stat;
    [SerializeField] private ProjectileManager _projectileManager;
    [SerializeField] private Material[] _materials;
    [SerializeField] private VehicleMovement _movement;
    [SerializeField] private VehicleTurret _turret;
    [SerializeField] private MeshRenderer _turretRenderer;
    [SerializeField] private MeshRenderer _canonRenderer;
    [SerializeField] private Driver_UI_Tank _driverUI;
    
    [Header("SnowEffect")]
    [SerializeField] private GameObject _snowVfx;
    [SerializeField] private GameObject _gunnerSnowSight;
    [SerializeField] private GameObject _driverSnowSight;
    
    [Header("MiniMapSetting")]
    [SerializeField] private MinimapSetting[] _minimapSettings;
    [SerializeField] private Camera _minimapCamera;
    [SerializeField] private MeshRenderer _minimapDotMr;
    
    //현재 UI
    private PlayerTeamEnum _teamNum;
    
    // 현재 HP
    private NetworkVariable<int> _hp = new(0, writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> _isEnd = new(writePerm: NetworkVariableWritePermission.Owner);
    // 공격 쿨다운 (일단 일반변수로 가보자)

    private Material _material;
    private MeshRenderer _meshRenderer;
    private BoxCollider _collider;
    private Rigidbody _rigidbody;
    
    private bool _isDamageable;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
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
        _isEnd.OnValueChanged += (_, _) => DestoryOnNetwork();
    }

    public override void OnNetworkDespawn()
    {
        Camera.main.gameObject.SetActive(true);
    }

    private void DestoryOnNetwork()
    {
        ServiceLocator.Get<IGameManager>().RemoveKillLogHandler(KillLogHandler);
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
        foreach (var item in _minimapSettings)
        {
            if (item.TeamNum == _teamNum)
            {
                _minimapDotMr.material = item.MinimapMaterial;
                _minimapCamera.targetTexture = item.MaximapTexture;
                break;
            }
        }

        if (_teamNum == userInfo.teamNum && userInfo.role == PlayerRole.Driver)
            _hp.Value = _stat.VechicleMaximumHP;
        
        _hp.OnValueChanged += HpValueChangeHandler;
        ServiceLocator.Get<IGameManager>().AddKillLogHandler(KillLogHandler);
        var teamInfo = ServiceLocator.Get<IGameManager>().GetMyTeamInfo(_teamNum);
        _movement.SetDriverData(_stat, teamInfo);
        _turret.SetGunnerData(_stat, teamInfo);
        _rigidbody.position = pos;
        Debug.Log($"[TankController] Init Tank ... position ; {pos} => {_rigidbody.position}");
        Debug.Log("[TankController] Init Tank ... Completed");
    }

    private void HpValueChangeHandler(int oldVal, int newVal)
    {
        Debug.Log($"[TankController] {gameObject.name} _hp : {oldVal} -> {newVal}");
        var hpRate = newVal / (float)_stat.VechicleMaximumHP;
        _driverUI?.ChangeVehicleHealth(hpRate);
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
        Debug.Log($"[TankController] {gameObject.name} _hp : {_hp.Value} , dmg : {dmg}");
        TakeDamageClientRpc(dmg, enemy);
    }

    [ClientRpc]
    private void TakeDamageClientRpc(int dmg, PlayerTeamEnum enemy)
    {
        var user = ServiceLocator.Get<IUserInfoManager>().GetUserInfo();
        Debug.Log($"[TankController] {_teamNum}:{user.teamNum} | {user.role}:{PlayerRole.Driver}");
        if (_teamNum == user.teamNum && user.role == PlayerRole.Driver)
        {
            _hp.Value -= dmg;
            if (_hp.Value <= 0)
            {
                ServiceLocator.Get<IGameManager>().OnDestoryVehicleServerRpc(_teamNum, enemy);
            }
        }
    }

    private void KillLogHandler(PlayerTeamEnum self, PlayerTeamEnum enemy) => KillLogClientRpc(self, enemy);

    [ClientRpc]
    private void KillLogClientRpc(PlayerTeamEnum self, PlayerTeamEnum enemy)
    {
        _driverUI?.UpdateKillLog(self, enemy);   
    }
    
    [ClientRpc]
    public void GameEndProcessClientRpc()
    {
        var userInfo = ServiceLocator.Get<IUserInfoManager>().GetUserInfo();
        if (_teamNum == userInfo.teamNum && userInfo.role == PlayerRole.Driver)
            _isEnd.Value = true;
    }

    public void ExplosionDamaged(System.Numerics.Vector3 expsPos, int dmg, PlayerTeamEnum enemy)
    {
        
    }

    [ClientRpc]
    private void ViewEffectControlClientRpc(bool enable)
    {   // 각자 내 것만 켜.
        var userInfo = ServiceLocator.Get<IUserInfoManager>().GetUserInfo();
        if (_snowVfx != null)
        {
            if (userInfo.teamNum == _teamNum && userInfo.role == PlayerRole.Driver)
            {
                _snowVfx.SetActive(enable);
                _driverSnowSight.SetActive(enable);
                return;
            }
            if (userInfo.teamNum == _teamNum && userInfo.role == PlayerRole.Gunner)
            {
                _snowVfx.SetActive(enable);
                _gunnerSnowSight.SetActive(enable);
            }
        }
    }
    
    public void ViewEffectControl(bool enable)
    {
        Debug.Log($"[TankController] View Effect Control ... {enable}");
        ViewEffectControlClientRpc(enable);
    }
}