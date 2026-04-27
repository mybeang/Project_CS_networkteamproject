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

    void OnEnable()
    {
        input.Enable();
    }

    void OnDisable()
    {
        input.Disable();
    }

    public override void OnNetworkSpawn()
    {
        //클라이언트 ID 부여
        _myID = NetworkManager.Singleton.LocalClientId;
    }

    void Update()
    {
        //_myTank 초기화 코드 필요
        if (_myTank == null)
        {
            foreach (var tank in FindObjectsOfType<testTank>())
            {
                if (tank.DriverID.Value == _myID)
                {
                    _myTank = tank;
                    Debug.Log($"내 탱크 찾음 : {_myID}");
                    break;
                }
            }
        }

        if (!IsOwner) return;

        if( _myID == _myTank.DriverID.Value)
        {
            Debug.Log($"! : {moveInput}");
            _myTank.MoveBody(moveInput);
        }

    }
}
