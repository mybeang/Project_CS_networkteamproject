using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Auth;
using Unity.Netcode;
using UnityEngine;


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
    [SerializeField][Tooltip("해당 객체를 팀 단위로 식별하기 위해 넣어야하는 Material들")] private Material[] _PlayerableMaterials;
    [Header("게임 정보들")]
    // 맵은 고민이 조금 필요해 보임, 생각보다 크면 Instantiate 로 하나만 생성하게 만들고, 맵이 작으면 모두 불로온 뒤 Enable, Disable 정도만
    [SerializeField] private List<MapInfo> _maps;
    [SerializeField] private double _basicSpawnTime;
    [SerializeField][Range(60, 1800)] private int _gamePlayableTime;
    [Header("그 외")]
    [SerializeField] private Canvas _gameResultCanvas;

    [Header("디버기용")]
    [SerializeField] private bool _OnLoadedLog;
    [SerializeField] private bool _OnTimerStartLog;
    [SerializeField] private bool _OnSpawnLog;
    [SerializeField] private bool _OnReSpawnLog;
    [SerializeField] private bool _OnEventScheduleManagerLoadedLog;

    #endregion

    #region Private_Variable
    // 내부 변수 
    private TeamInfo[] _teams;

    private string _roomID;
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

    #region Network_Variable
    // 네트워크 변수들
    private NetworkVariable<int> _firstTeamScore;
    private NetworkVariable<int> _secondTeamScore;
    private NetworkVariable<int> _thirdTeamScore;
    private NetworkVariable<int> _fourTeamScore;
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
        _firstTeamScore = new NetworkVariable<int>(0);
        _secondTeamScore = new NetworkVariable<int>(0);
        _thirdTeamScore = new NetworkVariable<int>(0);
        _fourTeamScore = new NetworkVariable<int>(0);

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
        StartCoroutine(Timer());
        ServiceLocator.Register<IGameManager>(this);
    }
    protected override void Unregister() => ServiceLocator.Unregister<IGameManager>();

    public void AddEventSchedule(EventScheduleManager eventSchedulemanager)
    {
        if (_OnEventScheduleManagerLoadedLog)
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
        _roomID = roomID;
        _mapNumber = mapNumber;
    }

    public TeamInfo[] GetTeams() => _teams;

    // 로비에서 게임 시작 시 호출하여, 팀 정보 받아오기
    public void StartGame()
    {
        ServiceLocator.Get<IMapManager>().SelectMap(_mapNumber);
        ResetGameData();
        for (int i = 0; i < _teams.Length; i++)
        {
            ServiceLocator.Get<IVoiceManager>()?.OnJoinVoiceChannel($"{_roomID}{(int)_teams[i].GetTeamNum()}");
        }
        if (_OnLoadedLog)
            Debug.Log($"{name}에서 게임 시작 함수 정상 작동됌");
    }

    private void ResetGameData()
    {
        _firstTeamScore.Value = 0;
        _secondTeamScore.Value = 0;
        _thirdTeamScore.Value = 0;
        _fourTeamScore.Value = 0;

        InstantiateVehicleClientRpc();

        _timerCoroutine = StartCoroutine(Timer());
    }

    private GameObject CreatePlayerObject(TeamInfo team, PlayerInfo player)
    {
        GameObject obj = Instantiate(_playerObject, transform);
        obj.SetActive(true);
        obj.name = $"{team.GetTeamNum().ToString()}_{player.role}";
        obj.GetComponent<NetworkObject>().SpawnAsPlayerObject(player.clientId,true);
        return obj;
    }
    
    [ClientRpc]
    private void InstantiateVehicleClientRpc()
    {
        foreach (var team in _teams)
        {
            _managementObject[team.GetTeamNum()] = new();
            foreach (var player in team.players)
            {
                if (player.role == PlayerRole.Driver)
                {
                    // Driver
                    var driverObj= CreatePlayerObject(team, player);
                    _managementObject[team.GetTeamNum()].DriverObject = driverObj;

                    if (_OnSpawnLog)
                        Debug.Log($"조종수 객체 : {driverObj.name} 생성 완료");
           
                    // Body
                    GameObject bodyObj = Instantiate(_playerablePrefabs[(int)team.GetVehicle()]);
                    bodyObj.SetActive(true);
                    bodyObj.name = $"{team.GetTeamNum().ToString()}_{team.GetVehicle().ToString()}";
                    bodyObj.GetComponent<NetworkObject>().SpawnAsPlayerObject(player.clientId, true);
                    bodyObj.GetComponent<MeshRenderer>().materials[0] = _PlayerableMaterials[(int)team.GetTeamNum()];
                    bodyObj.transform.position = ServiceLocator.Get<IMapManager>().GetStartPoint(team.GetTeamNum());
                    _managementObject[team.GetTeamNum()].BodyObject = bodyObj;

                    if (_OnSpawnLog)
                        Debug.Log($"이동 객체 : {bodyObj.name} 생성 완료");
                }
                else
                {
                    // Gunner
                    var gunnerObj = CreatePlayerObject(team, player);
                    _managementObject[team.GetTeamNum()].GunnerObject = gunnerObj;
                    if (_OnSpawnLog)
                        Debug.Log($"사수 객체 : {gunnerObj.name} 생성 완료");
                }
            }
        }
    }

    // 소환된 경우 모든 Client 들에게 알려야함.
    [ClientRpc]
    private void ReSpawnVehicleClientRpc(PlayerTeamEnum team) 
    {
        var bodyObject = _managementObject[team].BodyObject;
        bodyObject.SetActive(true);
        bodyObject.transform.position = ServiceLocator.Get<IMapManager>().GetStartPoint(team);
        
        if (_OnReSpawnLog)
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
        _managementObject[myTeam].BodyObject.SetActive(false);
        // TODO : 플레그 관련 호출 정의될 시 여기서 호출

        _RespawnTimer[(int)myTeam] = _currentTime + _basicSpawnTime;
        if (_triggerTimerCoroutine == null)
            StartCoroutine(TrrigerTimer());

        // 점수
        switch(enemy) // TODO : 메모리 변조 같은 간단한 값에 대한 위조 방지 장치가 필요한지 논의 필요
        {
            case PlayerTeamEnum.firstTeam:
                _firstTeamScore.OnValueChanged(_firstTeamScore.Value,_firstTeamScore.Value += 1);
                break;
            case PlayerTeamEnum.secondTeam:
                _secondTeamScore.OnValueChanged(_secondTeamScore.Value, _secondTeamScore.Value += 1);
                break;
            case PlayerTeamEnum.thirdTeam:
                _thirdTeamScore.OnValueChanged(_thirdTeamScore.Value, _thirdTeamScore.Value += 1);
                break;
            case PlayerTeamEnum.fourthTeam:
                _fourTeamScore.OnValueChanged(_fourTeamScore.Value, _fourTeamScore.Value += 1);
                break;
        }
        OnChangeScore?.Invoke(new int[4] {_firstTeamScore.Value, _secondTeamScore.Value, _thirdTeamScore.Value, _fourTeamScore.Value});

        // 킬로그 호출
        OnKillLog?.Invoke(myTeam,enemy);
    }

    IEnumerator TrrigerTimer()
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
                    ReSpawnVehicleClientRpc((PlayerTeamEnum)i);
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
        yield break;
    }

    private void GameEnd()
    {
        // 게임 종료 시 호출 될 것들

        // 이벤트 스케줄러 해제
        _eventScheduleManager = null;

        // 맵 끄기
        // _maps[_mapNumber].maps.SetActive(false);
        // 음성 채널 탈퇴
        byte i;
        for (i = 0; i < _teams.Length; i++)
        {
            ServiceLocator.Get<IVoiceManager>()?.OnLeaveVoiceChannel($"{_roomID}{(int)_teams[i].GetTeamNum()}");
        }
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
            Destroy(_managementObject[team.GetTeamNum()].BodyObject);
            Destroy(_managementObject[team.GetTeamNum()].GunnerObject);
            Destroy(_managementObject[team.GetTeamNum()].DriverObject);
            switch (team.GetTeamNum())
            {
                case PlayerTeamEnum.firstTeam:
                    team.SetScore(_firstTeamScore.Value);
                    break;
                case PlayerTeamEnum.secondTeam:
                    team.SetScore(_secondTeamScore.Value);
                    break;
                case PlayerTeamEnum.thirdTeam:
                    team.SetScore(_thirdTeamScore.Value);
                    break;
                case PlayerTeamEnum.fourthTeam:
                    team.SetScore(_fourTeamScore.Value);
                    break;
            }
        }
        Debug.Log("게임 종료 성공적으로 호출됌");
        if (IsServer) ServiceLocator.Get<INetworkSceneLoader>().LoadScene("Result");
    }
}