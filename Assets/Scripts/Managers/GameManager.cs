using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class TeamContainer
{
    public TeamInfo[] teams;
}

public class GameManager : NetworkManager<GameManager>, IGameManager
{
    #region Show_in_Inspector_Variable
    [Header("이동 객체 관련")]
    [SerializeField][Tooltip("전차 등의 객체(Prefab)을 직접 넣는 곳")] private GameObject[] _playerPrefabs;
    [Header("게임 정보들")]
    [SerializeField][Range(1, 10)] private int _respawnInterval;
    [SerializeField][Range(60, 1800)] private int _gamePlayableTime;
    #endregion

    #region Private_Variable
    // 내부 변수 
    private TeamInfo[] _teams;

    private string _roomId;
    private string _voiceChannelName;
    private int _mapNumber;
    private int _eventCounter;

    private bool _isEventEndTimer;
    private bool _hasEventEndtimer;

    private double _startTime;
    private double[] _eventTimer;
    private double[] _eventEndTimer;

    private WaitForSecondsRealtime _tick;
    private Coroutine _timerCoroutine;
    private Dictionary<PlayerTeamEnum, Coroutine> _triggerTimerCoroutines = new();

    private Dictionary<PlayerTeamEnum, GameObject> _managementObject = new();
    private EventScheduleManager _eventScheduleManager;
    #endregion

    // 네트워크 변수들
    #region Network_Variable
    private NetworkVariable<GameState> _gameState = new(); // 0
    private NetworkVariable<int> _team1Score = new (writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> _team2Score = new (writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> _team3Score = new (writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> _team4Score = new (writePerm: NetworkVariableWritePermission.Owner);
    
    private NetworkVariable<int> _team1RespawnTime = new ();
    private NetworkVariable<int> _team2RespawnTime = new ();
    private NetworkVariable<int> _team3RespawnTime = new ();
    private NetworkVariable<int> _team4RespawnTime = new ();
    private NetworkVariable<double> _remainingTime = new();
    #endregion

    #region ActionFuntion
    /// <summary>
    /// self, enemy 형태로 보낼 예정
    /// 받을 때 주의할 것
    /// </summary>
    public event Action<PlayerTeamEnum, PlayerTeamEnum> OnKillLog;

    /// <summary>
    /// 시간을 전역적으로 알려줄 함수.
    /// </summary>
    public event Action<int> OnChangeTime;

    /// <summary>
    /// 스코어가 바뀔 경우 Invoke해줄 함수.
    /// </summary>
    public event Action<int[]> OnChangeScore;
    #endregion

    private void Awake()
    {
        _isEventEndTimer = false;
        _eventCounter = 0;
        _tick = new WaitForSecondsRealtime(0.1f);
        _triggerTimerCoroutines[PlayerTeamEnum.firstTeam] = null;
        _triggerTimerCoroutines[PlayerTeamEnum.secondTeam] = null;
        _triggerTimerCoroutines[PlayerTeamEnum.thirdTeam] = null;
        _triggerTimerCoroutines[PlayerTeamEnum.fourthTeam] = null;
    }

    public TeamInfo GetMyTeamInfo(PlayerTeamEnum myTeamNum)
    {
        foreach (var team in _teams)
        {
            if (team.teamNum == myTeamNum) return team;
        }

        return null;
    }

    public Dictionary<PlayerTeamEnum, GameObject> GetPlayableObjects() => _managementObject;
    
    public override void OnNetworkSpawn() { }

    protected override void Register()
    {
        ServiceLocator.Register<IGameManager>(this);
    }
    protected override void Unregister() => ServiceLocator.Unregister<IGameManager>(this);
    
    public void AddEventSchedule(EventScheduleManager eventSchedulemanager)
    {
        Debug.Log($"[GameManager] Add Event Schedule; {eventSchedulemanager.name} ");
        _eventScheduleManager = eventSchedulemanager;
        _eventTimer = _eventScheduleManager.GetTimer();
        _eventEndTimer = eventSchedulemanager.GetStopTimer();
        _hasEventEndtimer = _eventEndTimer != null;
    }

    //[ClientRpc(Delivery = RpcDelivery.Reliable)]

    IEnumerator Timer()
    {
        _startTime = NetworkManager.Singleton.ServerTime.Time;
        _remainingTime.Value = 0;
        Debug.Log("[GameManager] Game Start ... Starting Timer");
        while (_remainingTime.Value <= _gamePlayableTime)
        {
            _remainingTime.Value = NetworkManager.Singleton.ServerTime.Time - _startTime;
            OnChangeTime?.Invoke((int)(_gamePlayableTime - _remainingTime.Value));
            if (IsServer && _eventScheduleManager != null)
            {
                if (_eventCounter < _eventTimer.Length &&_eventTimer[_eventCounter] <= _remainingTime.Value)
                {
                    if (_eventEndTimer != null && _eventCounter < _eventEndTimer.Length)
                        _isEventEndTimer = true;
                    Debug.Log($"[GameManager] Catch Event ec:{_eventCounter} et:{_eventTimer[_eventCounter]} el{_remainingTime.Value}");
                    _eventScheduleManager.OnEventSpawn();
                    _eventCounter++;
                }
                else if (_isEventEndTimer && _eventEndTimer != null && _eventEndTimer[_eventCounter - 1] <= _remainingTime.Value)
                {
                    Debug.Log($"[GameManager] Release Event ec:{_eventCounter} et:{_eventEndTimer[_eventCounter - 1]} el{_remainingTime.Value}");
                    _isEventEndTimer = false;
                    _eventScheduleManager.OnEventDespawn();
                }
            }
            yield return _tick;
        }
        _gameState.Value = GameState.GameEnd;
    }

    public void SetData(TeamInfo[] teams, in string roomID, int mapNumber)
    {   // Lobby 로 부터 데이터를 받는 함수
        _teams = teams;
        _roomId = roomID;
        _mapNumber = mapNumber;
        Debug.Log("[GameManager] ---- Data Updated on Server ----");
        Debug.Log($"[GameManager] roomID: {_roomId}");
        Debug.Log($"[GameManager] mapNumber: {_mapNumber}");
        foreach (var team in _teams)
            Debug.Log($"[GameManager] {team.ToPrettyString()}");
        Debug.Log("[GameManager] --------------------------------");
        if (!IsServer) return;
        var teamContainer = new TeamContainer() {teams = teams};
        var teamJson = JsonUtility.ToJson(teamContainer);
        Debug.Log($"[GameManager] teamJson: {teamJson}");
        SetDataClientRpc(teamJson, roomID, mapNumber);
    }

    [ClientRpc]
    private void SetDataClientRpc(string teamsJson, string roomId, int mapNumber)
    {
        if (IsServer) return;
        _roomId = roomId;
        _mapNumber = mapNumber;
        Debug.Log($"[GameManager] {teamsJson}");
        var teamInfo = JsonUtility.FromJson<TeamContainer>(teamsJson);
        _teams = teamInfo.teams;
        Debug.Log("[GameManager] ---- Data Updated on Client ----");
        Debug.Log($"[GameManager] roomID: {_roomId}");
        Debug.Log($"[GameManager] mapNumber: {_mapNumber}");
        foreach (var team in _teams)
            Debug.Log($"[GameManager] {team.ToPrettyString()}");
        Debug.Log("[GameManager] --------------------------------");
    }

    public TeamInfo[] GetTeams() => _teams;

    private string VoiceChannelFormat(PlayerTeamEnum teamNum) => $"{_roomId}_{teamNum}";
    
    public void StartGame()
    {
        Debug.Log($"[GameManager] Start Game at {name}"); 
        ServiceLocator.Get<IInGameCommonUIController>().SetLoadingUIActive(true);
        ServiceLocator.Get<IMapManager>().SelectMap(_mapNumber);
        if (IsServer) _gameState.Value = GameState.Init;
        try
        {
            StartCoroutine(GameStateMachineCoroutine());
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            if (IsServer) ServiceLocator.Get<INetworkSceneLoader>().LoadScene("LobbyRoom");    
        }
    }

    // --------------------------------
    /// <summary>
    /// Game State 관리
    /// </summary>
    /// <returns></returns>
    private IEnumerator GameStateMachineCoroutine()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(1f);
            Debug.Log($"[GameManager] Game State Machine State : {_gameState.Value}");
            switch (_gameState.Value)
            {
                case GameState.Init:
                    if (IsServer) _gameState.Value = GameState.ResetGameData;
                    break;
                case GameState.ResetGameData:
                    ResetGameData();
                    break;
                case GameState.InstantiateVehicle:
                    InstantiateVehicle();
                    break;
                case GameState.SetOtherDataForGame:
                    SetOtherDataForGame();
                    break;
                case GameState.ReadyDone:
                    ReadyDone();
                    break;
                case GameState.GameEnd:
                    GameEnd();
                    yield break;
                case GameState.DoNothing:
                    break;
            }
        }
    }
    
    private void ResetGameData()
    {
        Debug.Log("[GameManager] Reset Game Data ... ");
        if (!IsServer)
        {
            Debug.Log("[GameManager] Reset Game Data ... SKIP");
            return;
        }
        _timerCoroutine = StartCoroutine(Timer());
        _team1Score.Value = 0;
        _team2Score.Value = 0;
        _team3Score.Value = 0;
        _team4Score.Value = 0;
        _gameState.Value = GameState.InstantiateVehicle;
        Debug.Log("[GameManager] Reset Game Data ... Done");
    }
    
    private void InstantiateVehicle()
    {
        Debug.Log("[GameManager] ---- InstantiateVehicle ----");
        if (!IsServer)
        {
            Debug.Log("[GameManager] ---- InstantiateVehicle ---- SKIP");
            return;
        }

        foreach (var team in _teams)
        {
            _managementObject[team.teamNum] = new();
            Debug.Log($"[GameManager] Setting {team.teamNum}");
            ulong driverId = 0L;
            foreach (var player in team.players)
                if (player.role == PlayerRole.Driver) driverId = player.clientId;
            // Body
            GameObject bodyObj = Instantiate(_playerPrefabs[(int)team.vehicle]);
            bodyObj.SetActive(true);
            bodyObj.name = $"{team.teamNum.ToString()}_{team.vehicle.ToString()}";
            // tankPos
            var pos = ServiceLocator.Get<IMapManager>().GetStartPoint(team.teamNum);
            Debug.Log($"[GameManager] {bodyObj.name}'s pos is {pos}");
            // Spawn on Network
            bodyObj.GetComponent<NetworkObject>().SpawnAsPlayerObject(driverId, true);
            // set data; team and teamColor
            var tc = bodyObj.GetComponent<TankController>();
            Debug.Log($"[GameManager] {bodyObj.name}'s team: {team.teamNum}");
            tc.SetDataClientRpc(team.teamNum, pos);
            _managementObject[team.teamNum] = bodyObj;
            Debug.Log($"[GameManager] {bodyObj.name} 생성 완료");
        }
        _gameState.Value = GameState.SetOtherDataForGame;
    }

    private void SetOtherDataForGame()
    {
        Debug.Log("[GameManager] Set Other Data For Game ... ");
        var userInfo = ServiceLocator.Get<IUserInfoManager>().GetUserInfo();
        _voiceChannelName = VoiceChannelFormat(userInfo.teamNum);
        ServiceLocator.Get<IVoiceManager>()?.OnJoinVoiceChannel(_voiceChannelName);
        ServiceLocator.Get<IAudioService>().PlayBGM(_mapNumber);
        Debug.Log("[GameManager] Set Other Data For Game ... Done ");
        if (IsServer) _gameState.Value = GameState.ReadyDone;
    }

    private void ReadyDone()
    {
        Debug.Log("[GameManager] ReadyDone ... ");
        ServiceLocator.Get<IInGameCommonUIController>().SetLoadingUIActive(false);
        if (IsServer) _gameState.Value = GameState.DoNothing;
        Debug.Log("[GameManager] ReadyDone ... Enjoy Game !!");
    }
    // --------------------------------
    
    /// <summary>
    /// 체력이 0 이하가 되어 파괴 판정이 된 경우 호출
    /// 체력이 0인 경우 호출 방법 :  myTeam(자신)이 enemy(적)에게 파괴되었습니다.
    /// </summary>
    /// <param name="myTeam"></param>
    /// <param name="enemy"></param>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void OnDestoryVehicleServerRpc(PlayerTeamEnum myTeam, PlayerTeamEnum enemy)
    {
        // 이동 수단 비활성화 및 플레그 호출
        Debug.Log("[GameManager] OnDestoryVehicleServerRpc ... ");
        if (!IsServer) return;
        var respawnPos = ServiceLocator.Get<IMapManager>().GetStartPoint(myTeam);
        if (_triggerTimerCoroutines[myTeam] == null)
        {
            Debug.Log("[GameManager] OnDestoryVehicleServerRpc ... Start Respawn Coroutine");
            _triggerTimerCoroutines[myTeam] = StartCoroutine(RespawnCoroutine(myTeam, respawnPos));
        }

        // 점수
        switch(enemy) 
        {   // ToDo. Playable 유닛에 따라 점수 판정이 달라 짐. 추후 구현.
            case PlayerTeamEnum.firstTeam:
                _team1Score.Value += 1;
                break;
            case PlayerTeamEnum.secondTeam:
                _team2Score.Value += 1;
                break;
            case PlayerTeamEnum.thirdTeam:
                _team3Score.Value += 1;
                break;
            case PlayerTeamEnum.fourthTeam:
                _team4Score.Value += 1;
                break;
        }
        OnChangeScore?.Invoke(new int[4] {_team1Score.Value, _team2Score.Value, _team3Score.Value, _team4Score.Value});
    
        // 킬로그 호출
        OnKillLog?.Invoke(myTeam, enemy);
        Debug.Log("[GameManager] OnDestoryVehicleServerRpc ... Done");
    }

    public void AddKillLogHandler(Action<PlayerTeamEnum, PlayerTeamEnum> callback) => OnKillLog += callback;
    public void RemoveKillLogHandler(Action<PlayerTeamEnum, PlayerTeamEnum> callback) => OnKillLog -= callback;

    IEnumerator RespawnCoroutine(PlayerTeamEnum team, Vector3 respawnPos)
    {
        Debug.Log("[GameManager] RespawnCoroutine ... ");
        RespawnUIControl(team, true);
        var bodyObject = _managementObject[team];
        bodyObject.SetActive(false);
        Debug.Log("[GameManager] RespawnCoroutine ... Despawn");
        bodyObject.GetComponent<NetworkObject>().Despawn(false);
        NetworkVariable<int> netVar = _team1RespawnTime;
        switch (team)
        {
            case PlayerTeamEnum.secondTeam:
                netVar = _team2RespawnTime;
                break;
            case PlayerTeamEnum.thirdTeam:
                netVar = _team3RespawnTime;
                break;
            case PlayerTeamEnum.fourthTeam:
                netVar = _team4RespawnTime;
                break;
        }
        netVar.Value = _respawnInterval;
        while (netVar.Value > 0)
        {
            netVar.Value--;   
            yield return new WaitForSecondsRealtime(1f);    
        }

        ulong driverId = 0L;
        foreach (var teamInfo in _teams)
        {
            if (teamInfo.teamNum == team)
            {
                foreach (var player in teamInfo.players)
                    if (player.role == PlayerRole.Driver) driverId = player.clientId;        
            }
        }
        bodyObject.SetActive(true);
        Debug.Log("[GameManager] RespawnCoroutine ... Spawn");
        bodyObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(driverId, true);
        var tc = bodyObject.GetComponent<TankController>();
        Debug.Log($"[GameManager] RespawnCoroutine ; {bodyObject.name}'s team: {team}");
        tc.SetDataClientRpc(team, respawnPos);
        _triggerTimerCoroutines[team] = null; // 팀별로 있어야되.
        RespawnUIControl(team, false);
    }

    public void AddRespawnCounterHandler(PlayerTeamEnum team, NetworkVariable<int>.OnValueChangedDelegate callback)
    {
        switch (team)
        {
            case PlayerTeamEnum.firstTeam:
                _team1RespawnTime.OnValueChanged += callback;
                break;
            case PlayerTeamEnum.secondTeam:
                _team2RespawnTime.OnValueChanged += callback;
                break;
            case PlayerTeamEnum.thirdTeam:
                _team3RespawnTime.OnValueChanged += callback;
                break;
            case PlayerTeamEnum.fourthTeam:
                _team4RespawnTime.OnValueChanged += callback;
                break;
        }
    }

    public void RemoveRespawnCounterHandler(PlayerTeamEnum team, NetworkVariable<int>.OnValueChangedDelegate callback)
    {
        switch (team)
        {
            case PlayerTeamEnum.firstTeam:
                _team1RespawnTime.OnValueChanged -= callback;
                break;
            case PlayerTeamEnum.secondTeam:
                _team2RespawnTime.OnValueChanged -= callback;
                break;
            case PlayerTeamEnum.thirdTeam:
                _team3RespawnTime.OnValueChanged -= callback;
                break;
            case PlayerTeamEnum.fourthTeam:
                _team4RespawnTime.OnValueChanged -= callback;
                break;
        }
    }
    
    private void RespawnUIControl(PlayerTeamEnum teamEnum, bool enable)
    {
        ServiceLocator.Get<IInGameCommonUIController>().SetRespawnUIActive(teamEnum, enable);
    }

    private void GameEnd()
    {
        // 게임 종료 시 호출 될 것들
        Debug.Log("[GameManager] GameEnd ... ");
        // 이벤트 스케줄러 해제
        _eventScheduleManager = null;
        _eventCounter = 0;
        // 맵 끄기
        // 음성 채널 탈퇴
        Debug.Log("[GameManager] GameEnd ... Leave VoiceChannel");
        ServiceLocator.Get<IVoiceManager>()?.OnLeaveVoiceChannel(_voiceChannelName);

        // 타이머 정지
        if (_timerCoroutine != null)
        {
            Debug.Log("[GameManager] GameEnd ... Stop Timer");
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }

        Debug.Log("[GameManager] GameEnd ... Stop Trigger Timer");
        foreach (var team in _teams)
        {
            if (_triggerTimerCoroutines[team.teamNum] != null)
            {
                StopCoroutine(_triggerTimerCoroutines[team.teamNum]);
                _triggerTimerCoroutines[team.teamNum] = null;
            }
        }

        foreach (var team in _teams)
        {
            // ToDO. 각자의 Client 에서 알아서 파괴 되게 해야함.
            Debug.Log($"[GameManager] GameEnd ... {team.teamNum} end process");
            if (IsServer)
            {
                _managementObject[team.teamNum].GetComponent<TankController>().GameEndProcessClientRpc();
            }
            
            switch (team.teamNum)
            {
                case PlayerTeamEnum.firstTeam:
                    team.score = _team1Score.Value;
                    break;
                case PlayerTeamEnum.secondTeam:
                    team.score = _team2Score.Value;
                    break;
                case PlayerTeamEnum.thirdTeam:
                    team.score = _team3Score.Value;
                    break;
                case PlayerTeamEnum.fourthTeam:
                    team.score = _team4Score.Value;
                    break;
            }
        }
        Debug.Log("게임 종료 성공적으로 호출됌");
        ServiceLocator.Get<ILocalSceneLoader>().LoadScene("Result");
    }

    [ContextMenu("Show Service Locator List")]
    private void PrintServiceLocator()
    {
        ServiceLocator.PrintServices();
    }
    
    [ContextMenu("Show Scores")]
    private void PrintScores()
    {
        Debug.Log($"--- Scores ----\n" +
                  $"> team1: {_team1Score.Value}\n" +
                  $"> team2: {_team2Score.Value}\n" +
                  $"> team3: {_team3Score.Value}\n" +
                  $"> team4: {_team4Score.Value}\n" +
                  $"--------------------------------");
        
    }
}