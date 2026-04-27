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
    }

    public override void OnNetworkSpawn()
    {
        //클라이언트 ID 부여
        _myID = NetworkManager.Singleton.LocalClientId;
    }

    void Update()
    {
        //_myTank 초기화 코드 필요
        if (_myTank == null) return;

        if (!IsOwner) return;

        if( _myID == _myTank.DriverID.Value)
        {
            _myTank.MoveBody(moveInput);
        }

    }
}
