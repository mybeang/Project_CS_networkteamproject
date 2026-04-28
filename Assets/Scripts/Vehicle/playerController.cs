using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class playerController : NetworkBehaviour
{
    private ulong _myID;
    private testTank _myTank;

    private InputSystem_Actions input;
    private Vector2 moveInput;

    void Awake()
    {
        input = new InputSystem_Actions();

        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        input.Player.Attack.performed += ctx => Attack();

        //임시로 카메라를 E를 꾹눌러서 켬.
        input.Player.Interact.performed += ctx => _myTank.TurnOnCamera(_myID);    
    }

    void OnEnable() => input.Enable();

    void OnDisable() => input.Disable();

    public override void OnNetworkSpawn()
    {
        //클라이언트 ID 부여
        _myID = NetworkManager.Singleton.LocalClientId;
        Debug.Log($"myID : {NetworkManager.Singleton.LocalClientId}");
    }

    void Update()
    {
        //_myTank 를 어떻게 가져올지 초기화 코드 필요
        if (_myTank == null)
        {
            foreach (var tank in FindObjectsOfType<testTank>())
            {
                //if (tank.DriverID.Value == _myID)
                //{
                //    _myTank = tank;
                //    Debug.Log($"내 탱크 찾음 : {_myID}");
                //    break;
                //}
                Debug.Log($"_myTank 초기화 완료 : {_myID}");
                _myTank = tank;
                
                break;
            }
        }

        if (!IsOwner) return;

        // driver일때의 행동
        if ( _myID == _myTank.DriverID)
        {
            Debug.Log($"MoveBody : {moveInput}");
            _myTank.MoveBodyServerRpc(moveInput);
            
        }

        // gunner일때의 행동
        if (_myID == _myTank.GunnerID)
        {
            Debug.Log($"MoveTurret : {moveInput}");
            _myTank.MoveTurretServerRpc(moveInput);
        }
    }

    //Attack : gunner일때만 가능
    private void Attack()
    {
        //테스트코드
        if( _myTank == null ) return;

        if (_myID != _myTank.GunnerID) return;
        _myTank.Shoot();
    }

}
