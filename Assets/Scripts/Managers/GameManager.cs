using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Extensions;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class TeamContainer
{
    public TeamInfo[] teams;
}

[Serializable]
public class GameObjectMap
{
    public GameObject GunnerObject;
    public GameObject DriverObject;
    public GameObject BodyObject;
}

public class GameManager : NetworkManager<GameManager>, IGameManager
{
    #region Show_in_Inspector_Variable
    [Header("플레이어(조종) 객체")]
    [SerializeField] private GameObject _playerObject;
    [Header("이동 객체 관련")]
    [SerializeField][Tooltip("전차 등의 객체(Prefab)을 직접 넣는 곳")] private GameObject[] _playerablePrefabs;
    [Header("게임 정보들")]
    [SerializeField] private double _basicSpawnTime;
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
    private double _currentTime;
    private double[] _RespawnTimer;
    private double[] _eventTimer;
    private double[] _eventEndTimer;

    private WaitForSecondsRealtime _tick;
    private Coroutine _timerCoroutine;
    private Coroutine _triggerTimerCoroutine;

    private Dictionary<PlayerTeamEnum, GameObjectMap> _managementObject = new();
    private EventScheduleManager _eventScheduleManager;
    #endregion

    // 네트워크 변수들
    #region Network_Variable
    private NetworkVariable<int> _startGameState = new(); // 0
    private NetworkVariable<int> _team1Score = new (writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> _team2Score = new (writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> _team3Score = new (writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> _team4Score = new (writePerm: NetworkVariableWritePermission.Owner);
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
        _RespawnTimer = new double[4];
        _isEventEndTimer = false;
        _eventCounter = 0;
        _tick = new WaitForSecondsRealtime(0.1f);
    }

    public override void OnNetworkSpawn()
    {
        
    }

    protected override void Register()
    {
        ServiceLocator.Register<IGameManager>(this);
    }
    protected override void Unregister() => ServiceLocator.Unregister<IGameManager>();
    
    public void AddEventSchedule(EventScheduleManager eventSchedulemanager)
    {
        Debug.Log($"[{name}] {eventSchedulemanager.name} 등록됌");
        _eventScheduleManager = eventSchedulemanager;
        _eventTimer = _eventScheduleManager.GetTimer();
        _eventEndTimer = eventSchedulemanager.GetStopTimer();
        _hasEventEndtimer = _eventEndTimer != null;
    }

    //[ClientRpc(Delivery = RpcDelivery.Reliable)]

    IEnumerator Timer()
    {
        _startTime = NetworkManager.Singleton.ServerTime.Time;
        _currentTime = 0;
        Debug.Log("타이머 시작됌");
        while (_currentTime <= _gamePlayableTime)
        {
            _currentTime = NetworkManager.Singleton.ServerTime.Time - _startTime;
            OnChangeTime?.Invoke((int)(_gamePlayableTime - _currentTime));
            if (IsServer && _eventScheduleManager != null)
            {
                if (_eventCounter < _eventTimer.Length &&_eventTimer[_eventCounter] <= _currentTime)
                {
                    if (_eventEndTimer != null && _eventCounter < _eventEndTimer.Length)
                        _isEventEndTimer = true;
                    _eventScheduleManager.OnEventSpawnServerRpc();
                    _eventCounter++;
                }
                else if (_isEventEndTimer && _eventEndTimer != null && _eventEndTimer[_eventCounter - 1] <= _currentTime)
                {
                    _isEventEndTimer = false;
                    _eventScheduleManager.OnEventDespawnServerRpc();
                }
            }
            yield return _tick;
        }
        GameEnd();
    }

    public void SetData(TeamInfo[] teams, in string roomID, int mapNumber)
    {
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
        ServiceLocator.Get<IMapManager>().SelectMap(_mapNumber);
        if (IsServer) _startGameState.Value = 0;
        try
        {
            StartCoroutine(StartGameStateMachineCoroutine());
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            if (IsServer) ServiceLocator.Get<INetworkSceneLoader>().LoadScene("LobbyRoom");    
        }
    }

    /// <summary>
    /// State 변경을 모든 Host 및 Client 가 작업 완료시에 변경하는 것이 좋을 것 같은데, 아이디어가 딱히 떠오르지가 않음 ㅠㅠ
    /// stateNum : Desc  
    ///        0 : Start
    ///        1 : ResetGameData
    ///        2 : InstantiateVeichle
    ///        3 : SetOtherDataForGame
    ///        4 : Finish
    /// </summary>
    private IEnumerator StartGameStateMachineCoroutine()
    {
        while (_startGameState.Value < 5)
        {
            Debug.Log($"[GameManager] Start Game State Machine State : {_startGameState.Value}");
            switch (_startGameState.Value)
            {
                case 0:
                    if (IsServer) _startGameState.Value++;
                    break;
                case 1:
                    ResetGameData();
                    break;
                case 2:
                    InstantiateVehicle();
                    break;
                case 3:
                    SetOtherDataForGame();
                    break;
                case 4:
                    yield break;
            }
            yield return new WaitForSecondsRealtime(0.5f);
        }
        Debug.Log($"[GameManager] The 'StartGame' method was working at {name}.");
    }

    private void SetOtherDataForGame()
    {
        Debug.Log("[GameManager] Set Other Data For Game ... ");
        var userInfo = ServiceLocator.Get<IUserInfoManager>().GetUserInfo();
        _voiceChannelName = VoiceChannelFormat(userInfo.teamNum);
        ServiceLocator.Get<IVoiceManager>()?.OnJoinVoiceChannel(_voiceChannelName);
        ServiceLocator.Get<IAudioService>().PlayBGM(_mapNumber);
        Debug.Log("[GameManager] Set Other Data For Game ... Done ");
        if (IsServer) _startGameState.Value++;
    }
    
    private void ResetGameData()
    {
        Debug.Log("[GameManager] Reset Game Data ... ");
        if (!IsServer)
        {
            Debug.Log("[GameManager] Reset Game Data ... SKIP");
            return;
        }
        _team1Score.Value = 0;
        _team2Score.Value = 0;
        _team3Score.Value = 0;
        _team4Score.Value = 0;
        _timerCoroutine = StartCoroutine(Timer());
        _startGameState.Value++;
    }

    private GameObject CreatePlayerObject(TeamInfo team, PlayerInfo player)
    {
        GameObject obj = Instantiate(_playerObject, transform);
        obj.SetActive(true);
        obj.name = $"{team.teamNum.ToString()}_{player.role}";
        obj.GetComponent<NetworkObject>().SpawnAsPlayerObject(player.clientId,true);
        return obj;
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
            ulong gunnerId = 0L;
            foreach (var player in team.players)
            {
                if (player.role == PlayerRole.Driver)
                {
                    // Driver
                    driverId = player.clientId;
                    var driverObj= CreatePlayerObject(team, player);
                    _managementObject[team.teamNum].DriverObject = driverObj;
                    Debug.Log($"[GameManager] 조종수 객체 : {driverObj.name} 생성 완료");
                }
                else
                {
                    // Gunner
                    var gunnerObj = CreatePlayerObject(team, player);
                    _managementObject[team.teamNum].GunnerObject = gunnerObj;
                    Debug.Log($"[GameManager] 사수 객체 : {gunnerObj.name} 생성 완료");
                }
            }
            // Body
            try
            {
                GameObject bodyObj = Instantiate(_playerablePrefabs[(int)team.vehicle]);
                bodyObj.SetActive(true);
                bodyObj.name = $"{team.teamNum.ToString()}_{team.vehicle.ToString()}";
                // tankPos
                var pos = ServiceLocator.Get<IMapManager>().GetStartPoint(team.teamNum);
                Debug.Log($"[GameManager] {bodyObj.name}'s pos is {pos}");
                bodyObj.transform.position = pos;
                // Spawn on Network
                bodyObj.GetComponent<NetworkObject>().SpawnAsPlayerObject(driverId, true);
                // set data; team and teamColor
                var tc = bodyObj.GetComponent<TankController>();
                Debug.Log($"[GameManager] {bodyObj.name}'s team: {team.teamNum}");
                tc.SetDataClientRpc(team.teamNum);
                _managementObject[team.teamNum].BodyObject = bodyObj;

                Debug.Log($"[GameManager] 이동 객체 : {bodyObj.name} 생성 완료");
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                throw;
            }
        }
        _startGameState.Value++;
    }

    // 소환된 경우 모든 Client 들에게 알려야함.
    [ClientRpc]
    private void ReSpawnVehicleClientRpc(PlayerTeamEnum team, Vector3 pos) 
    {
        // ToDO. 터진놈이 알아서 리스폰 되게 해야함. 게임 매니저는 딱히 알 필요가 없음.
        var bodyObject = _managementObject[team].BodyObject;
        bodyObject.SetActive(true);
        bodyObject.transform.position = pos;
        Debug.Log($"{_managementObject[team].BodyObject.name} 리스폰 완료");
    }

    /// <summary>
    /// 체력이 0 이하가 되어 파괴 판정이 된 경우 호출
    /// 체력이 0인 경우 호출 방법 :  myTeam(자신)이 enemy(적)에게 파괴되었습니다.
    /// </summary>
    /// <param name="myTeam"></param>
    /// <param name="enemy"></param>
    [ServerRpc]
    public void OnDestoryVehicleServerRpc(PlayerTeamEnum myTeam, PlayerTeamEnum enemy)
    {
        // 이동 수단 비활성화 및 플레그 호출
        // ToDO. 탱크가 스스로 GameManager 에게 요청을 보내야함.
        // ToDO. 맞은 놈은 disable 되게, 때린놈은 킬로그 요청 하게.
        _managementObject[myTeam].BodyObject.SetActive(false);
        // TODO : 플레그 관련 호출 정의될 시 여기서 호출
        var respawnPos = ServiceLocator.Get<IMapManager>().GetStartPoint(myTeam);
        _RespawnTimer[(int)myTeam] = _currentTime + _basicSpawnTime;
        if (_triggerTimerCoroutine == null)
            _triggerTimerCoroutine = StartCoroutine(RespawnCoroutine(respawnPos));

        // 점수
        switch(enemy) // TODO : 메모리 변조 같은 간단한 값에 대한 위조 방지 장치가 필요한지 논의 필요
        {
            case PlayerTeamEnum.firstTeam:
                _team1Score.OnValueChanged(_team1Score.Value,_team1Score.Value += 1);
                break;
            case PlayerTeamEnum.secondTeam:
                _team2Score.OnValueChanged(_team2Score.Value, _team2Score.Value += 1);
                break;
            case PlayerTeamEnum.thirdTeam:
                _team3Score.OnValueChanged(_team3Score.Value, _team3Score.Value += 1);
                break;
            case PlayerTeamEnum.fourthTeam:
                _team4Score.OnValueChanged(_team4Score.Value, _team4Score.Value += 1);
                break;
        }
        OnChangeScore?.Invoke(new int[4] {_team1Score.Value, _team2Score.Value, _team3Score.Value, _team4Score.Value});

        // 킬로그 호출
        OnKillLog?.Invoke(myTeam,enemy);
    }

    IEnumerator RespawnCoroutine(Vector3 respawnPos)
    {
        byte i;
        byte counter;
        while (true)
        {
            counter = 0;
            for (i = 0; i < _RespawnTimer.Length; i++)
            {
                if (_currentTime <= _RespawnTimer[i])
                {
                    ReSpawnVehicleClientRpc((PlayerTeamEnum)i, respawnPos);
                }
                else
                {
                    counter++;
                }
                    
            }
            if (3 < counter)
                break;
            yield return _tick;
        }
        _triggerTimerCoroutine = null;
    }

    private void GameEnd()
    {
        // 게임 종료 시 호출 될 것들

        // 이벤트 스케줄러 해제
        _eventScheduleManager = null;

        // 맵 끄기
        // 음성 채널 탈퇴
        ServiceLocator.Get<IVoiceManager>()?.OnLeaveVoiceChannel(_voiceChannelName);

        // 타이머 정지
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }

        if (_triggerTimerCoroutine != null)
        {
            StopCoroutine(_triggerTimerCoroutine);
            _triggerTimerCoroutine = null;
        }

        foreach (var team in _teams)
        {
            // ToDO. 각자의 Client 에서 알아서 파괴 되게 해야함.
            Destroy(_managementObject[team.teamNum].BodyObject);
            Destroy(_managementObject[team.teamNum].GunnerObject);
            Destroy(_managementObject[team.teamNum].DriverObject);
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
        if (IsServer) ServiceLocator.Get<INetworkSceneLoader>().LoadScene("Result");
    }
}