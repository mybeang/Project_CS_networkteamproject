using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class testTank : NetworkBehaviour, IDamageableObject
{
    [SerializeField] private PlayerableStatisticsSO _tankStat;

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

    //이것들도 networkvariable로 가야하는건가?
    // 현재 HP
    private int _hp;
    // 공격 쿨다운
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

    //탱크 초기화 함수
    public void Init(ulong driverID, ulong gunnerID)
    {
        if (!IsServer) return;

        _driverID.Value = driverID;
        _gunnerID.Value = gunnerID;

        _hp = _tankStat.VechicleMaximumHP;
        _reloadTime = _tankStat.VechicleReloadtime;     
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
        transform.Rotate(0, input.x * _tankStat.VechicleRotationSpeed * Time.deltaTime, 0);

        // 이동
        Vector3 move = transform.forward * input.y;
        move = move * _tankStat.VechicleMoveSpeed * Time.deltaTime;
        transform.position += move;
    }

    //Turret을 회전하는 함수
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void MoveTurretServerRpc(Vector2 input)
    {
        // 수평회전
        _turret.Rotate(0, input.x * _tankStat.TurretHorizontalRotationSpeed * Time.deltaTime, 0);
        // 수직회전
        _turret.Rotate(input.y * _tankStat.TurretVerticalRotationSpeed * Time.deltaTime, 0, 0);
        
    }

    public void Shoot()
    {
        //쿨다운 없이 일단 테스트
        Debug.Log("_projectileManager.Shot()");

        _projectileManager.Shot();
    }

    public void TakeDamaged(int dmg)
    {
        Debug.Log($"_hp : {_hp} , dmg : {dmg}");
        _hp -= dmg;
    }

}
