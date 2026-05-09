using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class PlayerController : NetworkBehaviour
{
    private ulong _myID;
    //임시
    public TankController _myTank;

    private InputSystem_Actions input;
    private Vector2 moveInput;
    private NetworkVariable<bool> _isEnd = new (writePerm: NetworkVariableWritePermission.Owner);
    private UserInfo _userInfo;

    void Awake()
    {
        input = new InputSystem_Actions();

        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        input.Player.Attack.performed += ctx => Attack();
    }

    void OnEnable() => input.Enable();

    void OnDisable() => input.Disable();

    private void OnDestroy()
    {
        input.Player.Move.performed -= ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled -= ctx => moveInput = Vector2.zero;

        input.Player.Attack.performed -= ctx => Attack();
    }

    public override void OnNetworkSpawn()
    {
        //클라이언트 ID 부여
        _myID = NetworkManager.Singleton.LocalClientId;
        Debug.Log($"[PlayerController] myID : {NetworkManager.Singleton.LocalClientId}");
        _isEnd.OnValueChanged += (_, _) => Destroy(gameObject);
        _userInfo = ServiceLocator.Get<IUserInfoManager>().GetUserInfo();
    }

    void Update()
    {
        if (_myTank == null) return;
        if (!IsOwner) return;
        if (_userInfo.role == PlayerRole.Driver) // _myID == _myTank.DriverID)
        {
            // driver일때의 행동 / TODO : Update -> Function 으로 교체 필요.....
            Debug.Log($"MoveBody : {moveInput}");
            // _myTank.MoveBodyServerRpc(moveInput);
        }
        else
        {
            // gunner일때의 행동
            Debug.Log($"MoveTurret : {moveInput}");
            // _myTank.MoveTurretServerRpc(moveInput);
        }
    }

    //Attack : gunner일때만 가능
    private void Attack()
    {
        //테스트코드
        if( _myTank == null ) return;
        if (_userInfo.role != PlayerRole.Gunner) return;
        Debug.Log("_myTank.Shoot();");
        _myTank.Shoot();
    }
    
    [ClientRpc]
    public void GameEndProcessClientRpc()
    {
        _isEnd.Value = true;
    }
}
