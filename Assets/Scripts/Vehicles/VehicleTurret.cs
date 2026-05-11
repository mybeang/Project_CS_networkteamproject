using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class VehicleTurret : NetworkBehaviour
{
    [SerializeField] private PlayerableStatisticsSO _vehicleData;
    [SerializeField] private Canvas _gunnerUICanvas;
    //[SerializeField] private VehicleMovement ;
    [SerializeField] private ProjectileManager _projectile;

    private Tank_Gunner _gunnerUI; // TODO : 나중에 상위 객체를 받아서 전환하게 바꾸기

    private InputSystem_Actions _inputActions;

    private TeamInfo _myTeamInfo;


    public override void OnNetworkSpawn()
    {

        // 조건 검사로 내가 탑승한 차량이 내가 조종 권한이 있는 지 확인

        //if (!)
        _inputActions = new InputSystem_Actions();

        _inputActions.Player.Move.performed += TurretMovement;
        _inputActions.Player.Move.canceled += TurretMovement;
    }

    private void OnDestroy()
    {
        _inputActions.Player.Move.performed -= TurretMovement;
        _inputActions.Player.Move.canceled -= TurretMovement;
    }

    public void SetData(TeamInfo info, PlayerableStatisticsSO so)
    {
        _myTeamInfo = info;
        _vehicleData = so;
    }

    public void TurretMovement(InputAction.CallbackContext ctx)
    {
        // 터렛 움직임을 확정 지을 곳
    }

    private void Shot()
    {
        //_projectile.Shot();
    }

}
