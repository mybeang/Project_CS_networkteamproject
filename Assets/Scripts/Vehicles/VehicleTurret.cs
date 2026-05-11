using System.Collections;
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

    private TeamInfo _teamInfo;

    private bool isReloading;

    public override void OnNetworkSpawn()
    {
        if (!IsLocalPlayer) return;
        _inputActions = new InputSystem_Actions();

        _inputActions.Player.Move.performed += TurretMovement;
        _inputActions.Player.Move.canceled += TurretMovement;
        _inputActions.Player.Attack.performed += Shot;
        _inputActions.Enable();

        _gunnerUICanvas.enabled = true;
    }

    private void OnDestroy()
    {
        if (_gunnerUICanvas.enabled)
        {
            _inputActions.Player.Move.performed -= TurretMovement;
            _inputActions.Player.Move.canceled -= TurretMovement;
        }
    }

    public void SetGunnerData(PlayerableStatisticsSO so, TeamInfo team)
    {
        _vehicleData = so;
        _teamInfo = team;
    }

    IEnumerator ReLoad()
    {
        isReloading = true;
        double _startTime = Time.time;
        double _currentTime = 0;
        while(_vehicleData.VechicleReloadtime <= _currentTime)
        {
            _currentTime += Time.time - _startTime;
            yield return null;
        }
        isReloading = false;
    }

    public void ShowScoreUI()
    {

    }

    public void TurretMovement(InputAction.CallbackContext ctx)
    {
        // 포탑을 상 하 또는 좌 우만 움직이게 하기
    }

    private void Shot(InputAction.CallbackContext ctx)
    {
        if (!IsLocalPlayer && !isReloading) return;
        _projectile.Shot(transform, _teamInfo.GetTeamNum());
        StartCoroutine(ReLoad());
    }

}
