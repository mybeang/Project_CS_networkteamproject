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
    [SerializeField] private VehicleMovement _vehicleMovement;
    [SerializeField] private AudioClip _shotSound;
    [SerializeField] private AudioClip _reloadSound;

    private Gunner_UI _gunnerUI; // TODO : 나중에 상위 객체를 받아서 전환하게 바꾸기
    private TeamInfo _teamInfo;
    private ulong _gunnerId;

    private WaitForSeconds _tick;
    private bool isReloading;
    private bool _activeScript;

    public override void OnNetworkDespawn()
    {
        UnbindHandlers();
    }

    private void Awake()
    {
        _tick = new WaitForSeconds(0.2f);
    }

    private void BindHanders()
    {
        if (!_activeScript && !IsClient) return;
        Debug.Log($"[VehicleTurrent] {_teamInfo.teamNum} BindHanders");
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Enable();
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.Move.performed += TurretMovement;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.Move.canceled += TurretMovement;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.Attack.performed += Shot;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.ScoreBoard.performed += OnScoreBoard;
        _gunnerUICanvas.SetActive(true);
    }

    private void UnbindHandlers()
    {
        if (!_activeScript && !IsClient) return;
        Debug.Log($"[VehicleTurrent] {_teamInfo.teamNum} UnbindHandlers");
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
                _gunnerId = player.clientId;
                _activeScript = true;
                ActiveScript();
                break;
            }
        }
    }

    private void OnDisable()
    {
        if (IsServer) UnbindHandlers();
    }

    private void ActiveScript()
    {
        _gunnerCam.SetActive(true);
        _gunnerUI = _gunnerUICanvas.GetComponent<Gunner_UI>();
        isReloading = false;
        Debug.Log("[VehicleTurrent] ActiveScript");
        BindHanders();
    }

    IEnumerator ReLoad()
    {
        isReloading = true;
        double _startTime = Time.time;
        double _currentTime = 0;
        Debug.Log("[VehicleTurrent] ReLoad ... ");
        ServiceLocator.Get<IAudioService>().PlayOneShotSfx(_reloadSound);
        while(_vehicleData.VechicleReloadtime > _currentTime)
        {
            _currentTime = Time.time - _startTime;
            _gunnerUI.UpdateToReloadUI( (float)_currentTime / _vehicleData.VechicleReloadtime );
            yield return null;
        }
        Debug.Log("[VehicleTurrent] ReLoad ... Done");
        isReloading = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendInputDataToServerRpc(Vector2 input, ulong gunnerId)
    {
        Debug.Log($"[VehicleTurrent] SendInputDataToServer {gunnerId}");
        _vehicleMovement.UpdateTurretPosition(input, gunnerId);
    }

    private void TurretMovement(InputAction.CallbackContext ctx)
    {
        Vector2 input = ctx.ReadValue<Vector2>(); // 회전 축 0.601
        input *= new Vector2(1, -1);
        Debug.Log("[VehicleTurrent] TurrnetMovement");
        // 들어온 입력이 0, 1, 0.707 / 3개 중 0 과 1에 대해서만 반응
        // if (input.x * input.y != 0) return;
        SendInputDataToServerRpc(input, _gunnerId);
    }

    private void Shot(InputAction.CallbackContext ctx)
    {
        if (isReloading) return;
        Debug.Log("[VehicleTurrent] Shot");
        // Shot Effect 추가 필요
        ServiceLocator.Get<IAudioService>().PlayOneShotSfx(_shotSound);
        _projectile.Shot(_gunnerCam.transform, _teamInfo.teamNum);
        _gunnerUI.Fire();
        StartCoroutine(ReLoad());
    }
}
