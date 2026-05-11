using System;
using System.Collections;
using Unity.Netcode;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class VehicleTurret : NetworkBehaviour
{
    [SerializeField] private PlayerableStatisticsSO _vehicleData;
    [SerializeField] private Canvas _gunnerUICanvas;
    //[SerializeField] private VehicleMovement ;
    [SerializeField] private ProjectileManager _projectile;
    [SerializeField] private Camera _gunnerCam;

    [SerializeField] private GameObject _turret;
    [SerializeField] private Transform _canon;

    private Tank_Gunner _gunnerUI; // TODO : 나중에 상위 객체를 받아서 전환하게 바꾸기
    private InputSystem_Actions _inputActions;

    private TeamInfo _teamInfo;

    private bool isReloading;
    private float _canonAngle;

    private Vector3 _lastInput;

    public override void OnNetworkSpawn()
    {
        if (!IsLocalPlayer) return;
        _inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        _gunnerUICanvas.enabled = true;
        _inputActions.Player.Move.performed += TurretMovement;
        _inputActions.Player.Move.canceled += TurretMovement;
        _inputActions.Player.Attack.performed += Shot;
        _inputActions.Enable();
        StartCoroutine(RotatoinUpdater());
    }

    private void OnDisable()
    {
        StopCoroutine(RotatoinUpdater());
        _inputActions.Player.Move.performed -= TurretMovement;
        _inputActions.Player.Move.canceled -= TurretMovement;
        _gunnerUICanvas.enabled = false;
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

    IEnumerator RotatoinUpdater()
    {
        while (true)
        {
            transform.localRotation *= Quaternion.Euler(0, _lastInput.x * _vehicleData.TurretHorizontalRotationSpeed, 0);

            _canonAngle += _lastInput.y * _vehicleData.TurretVerticalRotationSpeed;
            _canonAngle  = Math.Clamp(_canonAngle, _vehicleData.TurretMaximumDepressionAngle, _vehicleData.TurretMaximumElevationAngle); // 나중에 데이터 기반으로 재구성
            _canon.localRotation = Quaternion.Euler(_canonAngle, 0, 0);

            yield return null;
        }
    }

    public void TurretMovement(InputAction.CallbackContext ctx)
    {
        Vector3 input = ctx.ReadValue<Vector3>(); // 회전 축 0.601

        // 들어온 입력이 0, 1, 0.707 / 3개 중 0 과 1에 대해서만 반응
        if (input.x * input.y != 0) return;
        _lastInput = input;
    }

    private void Shot(InputAction.CallbackContext ctx)
    {
        if (!IsLocalPlayer && !isReloading) return;
        _projectile.Shot(_gunnerCam.transform, _teamInfo.GetTeamNum());
        StartCoroutine(ReLoad());
    }
}
