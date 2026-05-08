using System.Collections;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Windows;

public class TankController : NetworkBehaviour, IDamageableObject
{
    [SerializeField] private PlayerableStatisticsSO _stat;

    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Transform _turret;

    [SerializeField] private Camera _camBody;
    [SerializeField] private Camera _camTurret;

    [SerializeField] private GameObject _driverUI;
    [SerializeField] private GameObject _gunnerUI;

    [SerializeField] private ProjectileManager _projectileManager;

    //현재 UI
    private GameObject _UI;
    private PlayerTeamEnum _self;

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

    private Coroutine _coroutine;
    private RaycastHit _rayHit; 

    private bool _canMove;
    private bool _isDamageable;

    private void Awake()
    {
        //driver, gunner 초기화가 이루어지면 클라이언트에서 UI를 실행시켜줌.
        _driverID.OnValueChanged += TurnOndriverUI;
        _gunnerID.OnValueChanged += TurnOnGunnerUI;
    }

    private void OnEnable()
    {
        //_hp.Value = _stat.VechicleMaximumHP;
        _reloadTime = 0;
        _isDamageable = false;
        _canMove = false;
        _coroutine = StartCoroutine(SpawnDelay());
    }

    private void OnDestroy()
    {
        _driverID.OnValueChanged -= TurnOndriverUI;
        _gunnerID.OnValueChanged -= TurnOnGunnerUI;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        //첫 클라가 ID: 0 이길래 MaxValue해줬음.
        _driverID.Value = ulong.MaxValue;
        _gunnerID.Value = ulong.MaxValue;
    }

    IEnumerator SpawnDelay()
    {
        yield return new WaitForSeconds(1f);
        _canMove = true;
        yield return new WaitForSeconds(2f);
        _isDamageable = true;
    }

    private void Update()
    {
        //쿨타임 줄여주기 / TODO : Update -> Coroutine 교체 필요...
        _reloadTime -= Time.deltaTime;
        if(_reloadTime < 0 ) _reloadTime = 0;
        _gunnerUI.GetComponent<Tank_Gunner>().UpdateToReloadUI(_reloadTime); // TODO : 나중에 UI cashing 후 사용하기

    }

    //탱크 초기화 함수
    public void Init(ulong driverID, ulong gunnerID, PlayerTeamEnum self)
    {
        if (!IsServer) return;
        Debug.Log("전차 초기화 완료");

        _driverID.Value = driverID;
        _gunnerID.Value = gunnerID;

        _self = self;

        _hp.Value = _stat.VechicleMaximumHP;
        _reloadTime = _stat.VechicleReloadtime;     
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
    [ServerRpc]
    public void MoveBodyServerRpc(Vector2 input)
    {
        transform.Rotate(0, input.x * _stat.VechicleRotationSpeed * Time.deltaTime, 0);

        Vector3 moveDir = transform.forward * input.y * _stat.VechicleMoveSpeed;

        if (Physics.Raycast(transform.position, Vector3.down, out _rayHit, 0.1f))
        {
            moveDir = Vector3.ProjectOnPlane(moveDir, _rayHit.normal).normalized;
            Quaternion rotation = Quaternion.FromToRotation(transform.up, _rayHit.normal) * transform.rotation;

            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * _stat.VechicleRotationSpeed);
        }

        if (input.y == 0)
            _rigidbody.linearVelocity = new Vector3(0, _rigidbody.linearVelocity.y, 0);
        else
            _rigidbody.linearVelocity = new Vector3(moveDir.x, _rigidbody.linearVelocity.y, moveDir.z);
    }

    //Turret을 회전하는 함수
    private Vector2 _lastInput; // 마지막에 어떻게 움직였는지 기록해놓고
    private bool _isHorizontalMode; // 수평, 수직방향중 한번에 하나의 방향으로만 움직일 수 있음.

    [ServerRpc]
    public void MoveTurretServerRpc(Vector2 input)
    {
        // 마지막에 입력된 축을 우선판정 
        if (input.x != 0 && _lastInput.x == 0)
        {
            _isHorizontalMode = true;
        }
        else if (input.y != 0 && _lastInput.y == 0)
        {
            _isHorizontalMode = false;
        }

        _lastInput = input;


        // 수평 회전
        if (_isHorizontalMode && input.x != 0)
        {
            _turret.Rotate(
                0,
                input.x * _stat.TurretHorizontalRotationSpeed * Time.deltaTime,
                0,
                Space.Self
            );
        }

        // 수직 회전
        if (!_isHorizontalMode && input.y != 0)
        {
            Vector3 localRot = _turret.localEulerAngles;

            // 현재 각도 변환
            float pitch = localRot.x;
            if (pitch > 180f)
                pitch -= 360f;

            // 목표 각도 계산
            float targetPitch = pitch - (input.y * _stat.TurretVerticalRotationSpeed * Time.deltaTime);

            // 예시) MaxElevationAngle = 60 ,MaxDepressionAngle = 30
            // ->   -30 ~ +60 제한
            targetPitch = Mathf.Clamp(
                targetPitch,
                -_stat.TurretMaximumDepressionAngle,
                _stat.TurretMaximumElevationAngle
            );

            localRot.x = targetPitch;
            _turret.localEulerAngles = localRot;
        }
    }

    public void Shoot()
    {
        Debug.Log("_projectileManager.Shot()");

        //재장전시간 체크
        if (_reloadTime > 0) return;
        _reloadTime = _stat.VechicleReloadtime;

        _projectileManager.Shot(_self);
    }

    public void KillLog(PlayerTeamEnum enemy)
    {
        _driverUI.GetComponent<Driver_UI>().UpdateKillLog(_self, enemy); // TODO : 상위 객체를 호출하다 보니 문제가 생길 수도...?
    }

    public void TakeDamaged(int dmg, PlayerTeamEnum enemy)
    {
        if (!_isDamageable) return;
        Debug.Log($"_hp : {_hp} , dmg : {dmg}");
        _hp.Value -= dmg;
        _driverUI.GetComponent<Driver_UI_Tank>().ChangeVehicleHealth(_hp.Value / _stat.VechicleMaximumHP); // TODO : 나중에 UI script를 cashing해서 사용하기
        if (_hp.Value <= 0)
            ServiceLocator.Get<IGameManager>().OnDestoryVehicleServerRpc(_self, enemy);
    }
}