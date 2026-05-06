using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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

    private double _startTime;
    private double _currentTime;
    private double[] _RespawnTimer;
    private double[] _eventTimer;
    private double[] _eventEndTimer;

    private WaitForSecondsRealtime _tick;
    private Coroutine _timerCoroutine;
    private Coroutine _triggerTimerCoroutine;

    private GameObject[] _managementObject;
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

    private void OnEnable()
    {
        StartCoroutine(Timer());
    }

    public override void OnNetworkSpawn()
    {
        
    }

    protected override void Register()
    {
        Debug.Log("등록 됌@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
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
                Debug.Log($"정상 초기화 및 이벤트 준비 완료");
                if (_eventCounter < _eventTimer.Length && _eventTimer[_eventCounter] <= _currentTime)
                {
                    if (_eventEndTimer != null)
                        _isEventEndTimer = true;
                    _eventScheduleManager.OnEventSpawnServerRpc();
                    _eventCounter++;
                }
                else if (_isEventEndTimer && _eventEndTimer[_eventCounter - 1] <= _currentTime)
                {
                    _isEventEndTimer = false;
                    _eventScheduleManager.OnEventDespawnServerRpc();
                }
            }
            yield return _tick;
        }
        GameEnd();
    }

    // 로비에서 게임 시작 시 호출하여, 팀 정보 받아오기
    public void StartGame(TeamInfo[] teams, in string roomID, int mapNumber)
    {
        _teams = teams;
        _roomID = roomID;
        _mapNumber = mapNumber;

        ResetGameData();

        _maps[_mapNumber].maps.SetActive(true);

        for (int i = 0; i < _teams.Length; i++)
        {
            ServiceLocator.Get<IVoiceManager>()?.OnJoinVoiceChannel($"{_roomID}{(int)_teams[i].TeamNum}");
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

    [ClientRpc]
    private void InstantiateVehicleClientRpc()
    {
        GameObject obj;
        _managementObject = new GameObject[_teams.Length * 3];
        for (byte i = 0; i < _teams.Length; i++)
        {
            obj = Instantiate(_playerObject,transform);
            obj.SetActive(true);
            obj.name = $"{_teams[i].TeamNum.ToString()} + Driver";
            obj.GetComponent<NetworkObject>().SpawnAsPlayerObject(_teams[i].DriverID,true);
            _managementObject[i * 3] = obj;

            if (_OnSpawnLog)
                Debug.Log($"조종수 객체 : {obj.name} 생성 완료");

            obj = Instantiate(_playerObject, transform);
            obj.SetActive(true);
            obj.name = $"{_teams[i].TeamNum.ToString()} + Gunner";
            obj.GetComponent<NetworkObject>().SpawnAsPlayerObject(_teams[i].GunnerID, true);
            _managementObject[(i * 3) + 1] = obj;

            if (_OnSpawnLog)
                Debug.Log($"사수 객체 : {obj.name} 생성 완료");

            obj = Instantiate(_playerablePrefabs[(int)_teams[i].VehicleNum]);
            obj.GetComponent<MeshRenderer>().materials[0] = _PlayerableMaterials[(int)_teams[i].TeamNum];
            obj.SetActive(true);
            obj.name = $"{_teams[i].TeamNum.ToString()} + {_teams[i].VehicleNum.ToString()}";
            obj.GetComponent<NetworkObject>().SpawnAsPlayerObject(_teams[i].DriverID, true);
            _managementObject[(i * 3) + 2] = obj;

            if (_OnSpawnLog)
                Debug.Log($"이동 객체 : {obj.name} 생성 완료");
        }
    }

    // 소환된 경우 모든 Client 들에게 알려야함.
    [ClientRpc]
    private void ReSpawnVehicleClientRpc(PlayerTeamEnum team) // TODO : 재소환 시 체력, 위치 재설정 가능하게 열려 있어야함.
    {
        int teamNum = (int)team * 3 + 2;
        _managementObject[teamNum].SetActive(true);

        // TODO : 여기에 이동 객체 초기화 함수 호출.

        if (_OnReSpawnLog)
            Debug.Log($"{_managementObject[teamNum].name} 리스폰 완료");
    }

    /// <summary>
    /// 체력이 0 이하가 되어 파괴 판정이 된 경우 호출
    /// 체력이 0인 경우 호출 방법 :  self(자신)이 enemy(적)에게 파괴되었습니다.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="enemy"></param>
    [ServerRpc]
    public void OnDestoryVehicleServerRpc(PlayerTeamEnum self, PlayerTeamEnum enemy)
    {
        // 이동 수단 비활성화 및 플레그 호출
        _managementObject[(int)self * 3 + 2].SetActive(false);
        // TODO : 플레그 관련 호출 정의될 시 여기서 호출

        _RespawnTimer[(int)self] = _currentTime + _basicSpawnTime;
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
        OnKillLog?.Invoke(self,enemy);
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
        _maps[_mapNumber].maps.SetActive(false);
        // 음성 채널 탈퇴
        byte i;
        for (i = 0; i < _teams.Length; i++)
        {
            ServiceLocator.Get<IVoiceManager>()?.OnLeaveVoiceChannel($"{_roomID}{(int)_teams[i].TeamNum}");
        }
        // 타이머 정지
        StopCoroutine(_timerCoroutine);
        StopCoroutine(_triggerTimerCoroutine);

        for (i = 0; i < _teams.Length; i++)
        {
            Destroy(_managementObject[i]);
        }

        Debug.Log("게임 종료 성공적으로 호출됌");

        // 게임 결과 화면
        _gameResultCanvas.enabled = true;

        // 게임 결과까지만 띄우고 결과창 넘어가기는 개인 단위로 넘어가기
    }
}