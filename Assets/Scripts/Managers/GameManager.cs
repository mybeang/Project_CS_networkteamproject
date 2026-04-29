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
    [SerializeField] private GameObject[] _playerablePrefabs;
    [SerializeField] private Material[] _PlayerableMaterials;
    [Header("소환 위치")]
    [SerializeField] private List<SpawnPoint> _spanwPoints;
    [Header("그 외")]
    [SerializeField] private Canvas _gameResultCanvas;
    [SerializeField][Range(60, 1800)] private int _gamePlayableTime;
#if UNITY_EDITOR
    [Header("디버기용")]
    [SerializeField] private bool _OnLoadedLog;
    [SerializeField] private bool _OnTimerStartLog;
    [SerializeField] private bool _OnSpawnLog;
    [SerializeField] private bool _OnReSpawnLog;
#endif

    #endregion
    // TODO : 맵 선택 enum으로 

    #region Private_Variable
    // 내부 변수 
    private teamInfo[] _teams;

    private string _roomID;

    private double _startTime;
    private double _currentTime;

    private WaitForSeconds _tick;
    private Coroutine _coroutine;

    private GameObject[] _managementObject;

    #endregion

    #region Network_Variable
    // 네트워크 변수들
    private NetworkVariable<int> _firstTeamScore;
    private NetworkVariable<int> _secondTeamScore;
    private NetworkVariable<int> _thirdTeamScore;
    private NetworkVariable<int> _fourTeamScore;
    #endregion

    private void Start()
    {
        _tick = new WaitForSeconds(0.25f);
    }

    protected override void Register() => ServiceLocator.Register<IGameManager>(this);
    protected override void Unregister() => ServiceLocator.Unregister<IGameManager>();

    [ClientRpc(Delivery = RpcDelivery.Reliable)]

    IEnumerator Timer()
    {
        _startTime = NetworkManager.Singleton.ServerTime.Time;
        _currentTime = 0;
        while(_currentTime <= _gamePlayableTime)
        {
            _currentTime = NetworkManager.Singleton.ServerTime.Time - _startTime;
            yield return _tick;
        }

        GameEnd();
    }

    // 로비에서 게임 시작 시 호출하여, 팀 정보 받아오기
    public void StartGame(teamInfo[] teams, in string roomID)
    {
        _teams = teams;
        _roomID = roomID;

        ResetGameData();

        for (int i = 0; i < _teams.Length; i++)
        {
            ServiceLocator.Get<IVoiceManager>()?.OnJoinVoiceChannel($"{_roomID}{_teams[i].TeamNum}"); // TODO : teamNum 바뀌면 그에 맞게 대응 필요
        }

#if UNITY_EDITOR
        if (_OnLoadedLog)
            Debug.Log($"{name}에서 게임 시작 함수 정상 작동됌");
#endif
    }

    private void ResetGameData()
    {
        _firstTeamScore.Value = 0;
        _secondTeamScore.Value = 0;
        _thirdTeamScore.Value = 0;
        _fourTeamScore.Value = 0;

        InstantiateVehicleClientRpc();

        _coroutine = StartCoroutine(Timer());
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
#if UNITY_EDITOR
            if (_OnSpawnLog)
                Debug.Log($"{obj.name} 생성 완료");
#endif

            obj = Instantiate(_playerObject, transform);
            obj.SetActive(true);
            obj.name = $"{_teams[i].TeamNum.ToString()} + Gunner";
            obj.GetComponent<NetworkObject>().SpawnAsPlayerObject(_teams[i].GunnerID, true);
            _managementObject[(i * 3) + 1] = obj;

#if UNITY_EDITOR
            if (_OnSpawnLog)
                Debug.Log($"{obj.name} 생성 완료");
#endif

            //obj = Instantiate(_playerablePrefabs[여기에]); TODO : 여기에 에 어떤 전차를 갖고 올지.
            obj.SetActive(true);
            obj.name = $"{_teams[i].TeamNum.ToString()} + {_teams[i].VehicleNum.ToString()}";
            obj.GetComponent<NetworkObject>().SpawnAsPlayerObject(_teams[i].DriverID, true);
            _managementObject[(i * 3) + 2] = obj;

#if UNITY_EDITOR
            if (_OnSpawnLog)
                Debug.Log($"{obj.name} 생성 완료");
#endif
        }
    }

    // 소환된 경우 모든 Client 들에게 알려야함.
    [ClientRpc]
    private void SpawnVehicleClientRpc(playerTeamEnum team) // TODO : 재소환 시 체력, 위치 재설정 가능하게 열려 있어야함.
    {
        int teamNum = (int)team * 3 + 2;
        _managementObject[teamNum].SetActive(true);

#if UNITY_EDITOR
        if (_OnReSpawnLog)
            Debug.Log($"{_managementObject[teamNum].name} 리스폰 완료");
#endif
    }

    /// <summary>
    /// 체력이 0 이하가 되어 파괴 판정이 된 경우 호출
    /// 체력이 0인 경우 호출 방법 :  self(자신)이 enemy(적)에게 파괴되었습니다.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="enemy"></param>
    [ServerRpc]
    public void OnDestoryVehicleServerRpc(playerTeamEnum self, playerTeamEnum enemy)
    {
        // TODO : 킬로그, 점수, 파괴된 이동 수단 비활성화 및 플레그 호출

        //_managementObject[]

        // 점수
        switch(enemy)
        {
            case playerTeamEnum.firstTeam:
                _firstTeamScore.OnValueChanged(_firstTeamScore.Value,_firstTeamScore.Value + 1);
                break;
            case playerTeamEnum.secondTeam:
                _secondTeamScore.OnValueChanged(_secondTeamScore.Value, _secondTeamScore.Value + 1);
                break;
            case playerTeamEnum.thirdTeam:
                _thirdTeamScore.OnValueChanged(_thirdTeamScore.Value, _thirdTeamScore.Value + 1);
                break;
            case playerTeamEnum.fourthTeam:
                _fourTeamScore.OnValueChanged(_fourTeamScore.Value, _fourTeamScore.Value + 1);
                break;
        }

        // 킬로그 호출

        
        // 파괴된 이동 수단 비활성 화 및 플레그 호출(아마 호출은 내부적으로 하면 좋을 듯)


    }

    private void GameEnd()
    {
        // 게임 종료 시 호출 될 것들

        // 음성 채널 탈퇴
        for (int i = 0; i < _teams.Length; i++)
        {
            ServiceLocator.Get<IVoiceManager>()?.OnLeaveVoiceChannel($"{_roomID}{_teams[i].TeamNum}");
        }
        // 타이머 정지
        StopCoroutine(_coroutine);

        for (byte i = 0; i < _teams.Length; i++)
        {
            Destroy(_managementObject[i]);
        }

        // 게임 결과 화면
        _gameResultCanvas.enabled = true;

        // 게임 결과까지만 띄우고 결과창 넘어가기는 개인 단위로 넘어가기
    }
}