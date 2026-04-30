using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class testTank : NetworkBehaviour, IDamageableObject
{
    [SerializeField] private PlayerableStatisticsSO _stat;

    [SerializeField] private Transform _turret;

    [SerializeField] private Camera _camBody;
    [SerializeField] private Camera _camTurret;

    [SerializeField] private GameObject _driverUI;
    [SerializeField] private GameObject _gunnerUI;

    [SerializeField] private ProjectileManager _projectileManager;

    //현재 UI
    private GameObject _UI;

    // driver 클라이언트 ID
    private NetworkVariable<ulong> _driverID = new();
    public ulong DriverID
    {
        get { return _driverID.Value; }
    }
    // gunner 클라이언트 ID
    private NetworkVariable<ulong> _gunnerID = new();
    public ulong GunnerID
    {
        get { return _gunnerID.Value; }
    }
    
    // 현재 HP
    private NetworkVariable<int> _hp;
    // 공격 쿨다운 (일단 일반변수로 가보자)
    private float _reloadTime;

    private void Awake()
    {
        //driver, gunner 초기화가 이루어지면 클라이언트에서 UI를 실행시켜줌.
        _driverID.OnValueChanged += TurnOndriverUI;
        _gunnerID.OnValueChanged += TurnOnGunnerUI;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        //첫 클라가 ID: 0 이길래 MaxValue해줬음.
        _driverID.Value = ulong.MaxValue;
        _gunnerID.Value = ulong.MaxValue;
    }

    private void Update()
    {
        //쿨타임 줄여주기
        _reloadTime -= Time.deltaTime;
        if(_reloadTime < 0 ) _reloadTime = 0;
        
    }

    //탱크 초기화 함수
    public void Init(ulong driverID, ulong gunnerID)
    {
        if (!IsServer) return;

        _driverID.Value = driverID;
        _gunnerID.Value = gunnerID;

        _hp.Value = _stat.VechicleMaximumHP;
        _reloadTime = _stat.VechicleReloadtime;     
    }

    //탱크 체력 초기화 함수
    public void SetHp(int hp)
    {
        _hp.Value = hp;
    }

    //탱크 위치 초기화 함수
    public void SetPosition(Vector3 pos)
    {
        transform.position = pos;
    }

    // tank에서 driver, gunner 초기화가 이루어졌다면 UI를 작동시킴.
    void TurnOndriverUI(ulong prevValue, ulong newValue)
    {

        if (newValue != NetworkManager.Singleton.LocalClientId) return;

        //탱크 player에게 할당해주기
        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localPlayer != null && localPlayer.TryGetComponent(out playerController pc))
        {
            pc._myTank = this;
        }

        if (_UI == null) _UI = Instantiate(_driverUI);

        //테스트용 mainCam 끄기
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.gameObject.SetActive(false);
        }
        _camBody.gameObject.SetActive(true);
    }
    void TurnOnGunnerUI(ulong prevValue, ulong newValue)
    {
        if (newValue != NetworkManager.Singleton.LocalClientId) return;

        //탱크 player에게 할당해주기
        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localPlayer != null && localPlayer.TryGetComponent(out playerController pc))
        {
            pc._myTank = this;
        }

        if (_UI == null) _UI = Instantiate(_gunnerUI);

        //테스트용 mainCam 끄기
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.gameObject.SetActive(false);
        }
        _camTurret.gameObject.SetActive(true);
    }


    //Body 자체를 회전하고 움직이는 함수
    // serverRpc로 안하니까 owner가 아닌 클라이언트에서 transform을 동기화 할 수가 없음.
    [Rpc(SendTo.Server,InvokePermission = RpcInvokePermission.Everyone)]
    public void MoveBodyServerRpc(Vector2 input)
    {
        // 회전
        transform.Rotate(0, input.x * _stat.VechicleRotationSpeed * Time.deltaTime, 0);

        // 이동
        Vector3 move = transform.forward * input.y;
        move = move * _stat.VechicleMoveSpeed * Time.deltaTime;
        transform.position += move;
    }

    //Turret을 회전하는 함수
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void MoveTurretServerRpc(Vector2 input)
    {
        // 수평회전
        _turret.Rotate(0, input.x * _stat.TurretHorizontalRotationSpeed * Time.deltaTime, 0);
        // 수직회전
        _turret.Rotate(input.y * _stat.TurretVerticalRotationSpeed * Time.deltaTime, 0, 0);

        
        
    }

    public void Shoot()
    {
        Debug.Log("_projectileManager.Shot()");

        //재장전시간 체크
        if (_reloadTime > 0) return;
        _reloadTime = _stat.VechicleReloadtime;

        _projectileManager.Shot();
    }

    public void TakeDamaged(int dmg)
    {
        Debug.Log($"_hp : {_hp} , dmg : {dmg}");
        _hp.Value -= dmg;
    }

}
