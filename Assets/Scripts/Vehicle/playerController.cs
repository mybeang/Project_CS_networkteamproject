using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class playerController : NetworkBehaviour
{
    private ulong _myID;
    //임시
    public testTank _myTank;

    private InputSystem_Actions input;
    private Vector2 moveInput;

    void Awake()
    {
        input = new InputSystem_Actions();

        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        input.Player.Attack.performed += ctx => Attack();

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
        if (_myTank == null) return;
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
