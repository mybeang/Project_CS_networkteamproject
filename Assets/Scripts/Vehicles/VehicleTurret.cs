using System;
using System.Collections;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class VehicleTurret : NetworkBehaviour
{
    [SerializeField] private PlayerableStatisticsSO _vehicleData;
    [SerializeField] private GameObject _gunnerUICanvas;
    //[SerializeField] private VehicleMovement ;
    [SerializeField] private ProjectileManager _projectile;
    [SerializeField] private GameObject _gunnerCam;

    [SerializeField] private Transform _canon;

    private Gunner_UI _gunnerUI; // TODO : 나중에 상위 객체를 받아서 전환하게 바꾸기

    private TeamInfo _teamInfo;

    private WaitForSeconds _tick;

    private bool isReloading;
    private bool _activeScript;
    private float _canonAngle;

    private Vector3 _lastInput;

    public override void OnNetworkSpawn()
    {

    }

    private void Awake()
    {
        _tick = new WaitForSeconds(0.2f);
    }

    private void OnEnable()
    {
        if (!_activeScript || !IsClient) return;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.Move.performed += TurretMovement;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.Move.canceled += TurretMovement;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.Attack.performed += Shot;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.ScoreBoard.performed += OnScoreBoard;
        _gunnerUICanvas.SetActive(true);

        ServiceLocator.Get<IInputSystem>().GetInputSystem().Enable();
        StartCoroutine(RotatoinUpdater());
    }

    private void OnDisable()
    {
        if (!_activeScript || !IsClient) return;
        StopCoroutine(RotatoinUpdater());
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.Move.performed -= TurretMovement;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.Move.canceled -= TurretMovement;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.Attack.performed -= Shot;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.ScoreBoard.performed -= OnScoreBoard;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Disable();
        _gunnerUICanvas.SetActive(false);
    }

    private void OnScoreBoard(InputAction.CallbackContext ctx)
    {
        _gunnerUI.ShowScore();
    }

    public void SetGunnerData(PlayerableStatisticsSO so, TeamInfo team)
    {
        _vehicleData = so;
        _teamInfo = team;

        foreach (var player in _teamInfo.players)
        {
            if (player.role == PlayerRole.Gunner && player.clientId == NetworkManager.Singleton.LocalClientId)
            {
                _activeScript = true;
                ActiveScript();
                break;
            }
        }
    }

    private void ActiveScript()
    {
        _gunnerCam.SetActive(true);
        _gunnerUICanvas.SetActive(true);
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.Move.performed += TurretMovement;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.Move.canceled += TurretMovement;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.Attack.performed += Shot;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Enable();
        StartCoroutine(RotatoinUpdater());
    }

    IEnumerator ReLoad()
    {
        isReloading = true;
        double _startTime = Time.time;
        double _currentTime = 0;
        while(_vehicleData.VechicleReloadtime <= _currentTime)
        {
            _currentTime += Time.time - _startTime;
            _gunnerUI.UpdateToReloadUI( (float)_currentTime / _vehicleData.VechicleReloadtime);
            yield return _tick;
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
        Vector2 input = ctx.ReadValue<Vector2>(); // 회전 축 0.601

        // 들어온 입력이 0, 1, 0.707 / 3개 중 0 과 1에 대해서만 반응
        if (input.x * input.y != 0) return;
        _lastInput = input;
    }

    private void Shot(InputAction.CallbackContext ctx)
    {
        if (!IsLocalPlayer && !isReloading) return;
        _projectile.Shot(_gunnerCam.transform, _teamInfo.teamNum);
        _gunnerUI.Fire();
        StartCoroutine(ReLoad());
    }
}
