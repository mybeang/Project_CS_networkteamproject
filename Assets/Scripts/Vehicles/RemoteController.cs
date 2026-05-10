using UnityEngine;
using Unity.Netcode;

public class RemoteController : NetworkBehaviour
{
    // UI 배제

    [SerializeField] private BaseVehicle _vehicle;

    [Header("UI 생성용")]
    [SerializeField] private GameObject _driverUI;
    [SerializeField] private GameObject _gunnerUI;


    private InputSystem_Actions _inputActions;

    private bool _isDriver;
    private bool _isInit;

    private void Awake()
    {
        _inputActions = new InputSystem_Actions();
        _isInit = false;
    }

    public override void OnNetworkSpawn()
    {
        
    }

    public void SetControllerData(bool isDriver, teststCode vehicle)
    {
        if (!IsOwner && _isInit) return;
        _isInit = true;
        _vehicle = vehicle;
        _isDriver = isDriver;
        GameObject obj;
        if (_isDriver)
        {
            Debug.Log($"[{name}] 조종수로 생성 됌");
            obj = Instantiate(_driverUI, transform); // TODO : UI 생성 후 vehicle에 넘겨줄 무언가가 필요. 고민 중
            _inputActions.Player.Move.performed += _vehicle.ProcessMovement;
            _inputActions.Player.Move.canceled += _vehicle.ProcessMovement;
        }
        else
        {
            Debug.Log($"[{name}] 포수로 생성 됌");
            obj = Instantiate(_gunnerUI, transform);
            _inputActions.Player.Move.performed += _vehicle.ProcessTurret;
            _inputActions.Player.Move.canceled += _vehicle.ProcessTurret;
        }
        // 등록 되기 전에 Enable 되면 작동이 안되는 문제가 있음
        // 반드시 등록이 완료된 시점에 Enable되야함.
        _inputActions.Enable();
    }

    private void OnDestroy()
    {
        if (!IsOwner) return;
        if (_isDriver)
        {
            _inputActions.Player.Move.performed -= _vehicle.ProcessMovement;
            _inputActions.Player.Move.canceled -= _vehicle.ProcessMovement;
        }
        else
        {
            _inputActions.Player.Move.performed -= _vehicle.ProcessTurret;
            _inputActions.Player.Move.canceled -= _vehicle.ProcessTurret;
        }
        _inputActions.Disable();
    }
}
