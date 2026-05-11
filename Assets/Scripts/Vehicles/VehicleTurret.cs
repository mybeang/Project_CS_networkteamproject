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

    public override void OnNetworkSpawn()
    {
        _inputActions = new InputSystem_Actions();

        _inputActions.Player.Move.performed += TurretMovement;
        _inputActions.Player.Move.canceled += TurretMovement;
        _inputActions.Enable();
    }

    private void OnDestroy()
    {
        if (_gunnerUICanvas.enabled)
        {
            _inputActions.Player.Move.performed -= TurretMovement;
            _inputActions.Player.Move.canceled -= TurretMovement;
        }
    }

    public void SetGunnerData(PlayerableStatisticsSO so)
    {
        _vehicleData = so;
        _gunnerUICanvas.enabled = true;
    }

    public void TurretMovement(InputAction.CallbackContext ctx)
    {
        
    }

    private void Shot()
    {
        
    }

}
