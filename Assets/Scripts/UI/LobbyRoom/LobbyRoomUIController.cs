using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Extensions;
using TMPro;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct MapPreview
{
    public Sprite image;
    public string name;
}

public class LobbyRoomUIController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _lobbySubject;
    [SerializeField] private Image _selectedMapImage;
    [SerializeField] private MessagePopUpUIController _msgPopUp;
    
    [Header("Buttons")]
    [SerializeField] private Button _leftMapButton;
    [SerializeField] private Button _rightMapButton;
    [SerializeField] private Button _startGameButton;
    [SerializeField] private Button _readyButton;
    [SerializeField] private Button _reSelectButton;
    [SerializeField] private Button _leaveRoomButton;
    
    [Header("Data")]
    [SerializeField] private List<MapPreview> _mapImages = new();
    [SerializeField] private TextMeshProUGUI _mapName;
    
    private int _selectedMapNumber;
    private Action OnSelectedMapNumberChanged; 
    private int SelectedMapNumber
    {
        get => _selectedMapNumber;
        set
        {
            _selectedMapNumber = value;
            OnSelectedMapNumberChanged?.Invoke();
        }
    }
    private bool _ready;
    private bool IsHost => 
        ServiceLocator.Get<ILobbyManager>().GetHostId() == 
        ServiceLocator.Get<IUserInfoManager>().GetUserInfo().userId;

    private string _joinCode = string.Empty;
    private const string RELAY_SYNC = "--Syncing--";
    
    private void Awake() => Init();

    private void OnEnable()
    {
        BindCallbackButtons();
        OnSelectedMapNumberChanged += OnRenderMap;
        var lobby = ServiceLocator.Get<ILobbyManager>();
        ServiceLocator.Get<IDatabaseBackend>()?.RegisterMapNumberValueChangedHandler(lobby.GetRoomID(), GetMapNumberFromDB);
        ServiceLocator.Get<IRelayHostManager>()?.OnHostDisconnectedAddListener(HostMigrationProcessHandler);
    }
    private void OnDisable()
    {
        UnbindCallbackButtons();
        OnSelectedMapNumberChanged -= OnRenderMap;
        var lobby = ServiceLocator.Get<ILobbyManager>();
        ServiceLocator.Get<IDatabaseBackend>()?.UnregisterMapNumberValueChangedHandler(lobby.GetRoomID(), GetMapNumberFromDB);
        ServiceLocator.Get<IRelayHostManager>()?.OnHostDisconnectedRemoveListener(HostMigrationProcessHandler);
    }

    private void Start()
    {
        if (IsHost)
        {   // Lobby Create
            CreateRelayHost();
        }
        else
        {
            var db = ServiceLocator.Get<IDatabaseBackend>();
            var relay = ServiceLocator.Get<IRelayHostManager>();
            var lobby = ServiceLocator.Get<ILobbyManager>();
            db.GetJoinCodeAsync(lobby.GetRoomID()).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogWarning("[LobbyRoomUIController] Get JoinCodeAsync task faulted.");
                    return;
                }
                _joinCode = task.Result;
                relay.StartClient(_joinCode);
            });
        }
        InitMapNumber();
        UpdateReadyState();
        _msgPopUp.Open(
            MessageType.Info, 
            "'Team #', '포수' 혹은 '운전자'를 클릭하여\n팀 및 역할 이동이 가능합니다.", 
            "닫기");
    }

    private void InitMapNumber()
    {
        var db =  ServiceLocator.Get<IDatabaseBackend>();
        string roomId = ServiceLocator.Get<ILobbyManager>().GetRoomID();
        if (IsHost)
        {
            db.SetMapNumberAsync(roomId, SelectedMapNumber);
            return;
        }

        db.GetMapNumberAsync(roomId).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("[LobbyRoomUIController] Get MapNumberAsync task faulted.");
                return;
            }
            SelectedMapNumber = int.Parse(task.Result);
        });
    }

    private void Init()
    {
        _lobbySubject.text = ServiceLocator.Get<ILobbyManager>()?.GetRoomName();
        if (_mapImages.Count != 0)
        {
            _selectedMapImage.sprite = _mapImages[0].image;
            _mapName.text = _mapImages[0].name;
        }
        _ready = false;
        ChangeButtonVisibility();
    }

    private void BindCallbackButtons()
    {
        _leftMapButton.onClick.AddListener(OnLeftMap);
        _rightMapButton.onClick.AddListener(OnRightMap);
        _startGameButton.onClick.AddListener(OnStartGame);
        _readyButton.onClick.AddListener(OnReady);
        _reSelectButton.onClick.AddListener(OnReSelect);
        _leaveRoomButton.onClick.AddListener(OnLeaveRoom);
    }

    private void UnbindCallbackButtons()
    {
        _leftMapButton.onClick.RemoveListener(OnLeftMap);
        _rightMapButton.onClick.RemoveListener(OnRightMap);
        _startGameButton.onClick.RemoveListener(OnStartGame);
        _readyButton.onClick.RemoveListener(OnReady);
        _reSelectButton.onClick.RemoveListener(OnReSelect);
        _leaveRoomButton.onClick.RemoveListener(OnLeaveRoom);
    }

    private void ChangeButtonVisibility()
    {
        _startGameButton.gameObject.SetActive(IsHost);
        _leftMapButton.gameObject.SetActive(IsHost);
        _rightMapButton.gameObject.SetActive(IsHost);
    }

    private void OnLeaveRoom()
    {
        if (_msgPopUp.IsOpen) return;
        OnReSelect();  // UI 사운드는 OnReSelect 에서 진행됨.
        ServiceLocator.Get<ILobbyManager>()?.LeaveRoom();
        if (IsHost) ServiceLocator.Get<IRelayHostManager>()?.Disconnect();
        ServiceLocator.Get<ILocalSceneLoader>()?.LoadScene("LobbyList");
    }

    private void OnReSelect()
    {
        if (_msgPopUp.IsOpen) return;
        ServiceLocator.Get<IAudioService>().PlayButtonSfx();
        Debug.Log("[LobbyRoomUIController] On ReSelect ... ");
        var lobby = ServiceLocator.Get<ILobbyManager>();
        if (lobby.GetMyPlayerData()[LobbyPlayerDataKey.READY] == "true")
        {
            Debug.Log("[LobbyRoomUIController] On ReSelect ... Canceled");
            _msgPopUp.Open(
                MessageType.Warning,
                "먼저 게임 준비 상태를 풀어주세요.");
            return;
        }
        
        List<(string key, string value)> updateData = new();
        updateData.Add((LobbyPlayerDataKey.TEAM, "0"));
        updateData.Add((LobbyPlayerDataKey.ROLE, $"{PlayerRole.None}"));
        lobby?.UpdatePlayerData(updateData).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.Log("[LobbyRoomUIController] On ReSelect ... Fail");
                return;
            }
            var userInfo = ServiceLocator.Get<IUserInfoManager>();
            userInfo?.SetIsDriver(PlayerRole.None);
            userInfo?.SetTeamNum(PlayerTeamEnum.neutralObject);
            Debug.Log($"[LobbyRoomUIController] On ReSelect ... Done");
        });
    }

    private void UpdateReadyState()
    {
        var lobby = ServiceLocator.Get<ILobbyManager>();
        List<(string key, string value)> updateData = new();
        updateData.Add((LobbyPlayerDataKey.READY, _ready ? "true" : "false" ));
        lobby?.UpdatePlayerData(updateData);
    }

    private void OnReady()
    {
        if (_msgPopUp.IsOpen) return;
        ServiceLocator.Get<IAudioService>().PlayButtonSfx();
        Debug.Log("[LobbyRoomUIController] Ready ... ");
        var lobbyManager = ServiceLocator.Get<ILobbyManager>();
        var player = lobbyManager.GetMyPlayerData();
        if (player[LobbyPlayerDataKey.ROLE] == $"{PlayerRole.None}" ||
            player[LobbyPlayerDataKey.TEAM] == "0")
        {
            Debug.Log("[LobbyRoomUIController] Ready ... Fail");
            _msgPopUp.Open(
                MessageType.Warning,
                "먼저 팀 및 역할을 정해주세요.");
            return;
        }
        _ready = !_ready;
        UpdateReadyState();
        UpdateClientIDToLobbyBackend();
        Debug.Log("[LobbyRoomUIController] Ready ... Done");
    }

    private List<TeamInfo> LobbyDataToTeamInfo()
    {
        var lobbyManager = ServiceLocator.Get<ILobbyManager>();
        var players = lobbyManager?.GetPlayerList();
        Dictionary<PlayerTeamEnum, TeamInfo> teams = new();
        if (players == null) return null;
        foreach (var player in players)
        {
            int index = int.Parse(player.Data[LobbyPlayerDataKey.TEAM].Value) - 1;
            var teamNum = (PlayerTeamEnum)index;
            if (!teams.ContainsKey(teamNum))
            {
                PlayerableVehicleEnum pv = PlayerableVehicleEnum.tank; // ToDo. 더 만들어지면 추가하기
                teams[teamNum] = new TeamInfo(teamNum, pv);
                teams[teamNum].players = new List<PlayerInfo>();
            }
            var temp = player.Data[LobbyPlayerDataKey.ROLE].Value.Split('.').Last();
            PlayerRole playerRole = (PlayerRole)Enum.Parse(typeof(PlayerRole), temp);

            PlayerInfo playerInfo = new PlayerInfo()
            {
                userId = player.Data[LobbyPlayerDataKey.USER_ID].Value,
                clientId = ulong.Parse(player.Data[LobbyPlayerDataKey.CLIENT_ID].Value),
                role = playerRole
            };
            teams[teamNum].players.Add(playerInfo);
        }
        return teams.Values.ToList();
    }
    
    private void OnStartGame()
    {
        if (_msgPopUp.IsOpen) return;
        ServiceLocator.Get<IAudioService>().PlayButtonSfx();
        Debug.Log("[LobbyRoomUIController] Start Game ... ");
        if (CheckAllReady())
        {
            Debug.Log("[LobbyRoomUIController] Start Game ... Can start the game");
            Debug.Log("[LobbyRoomUIController] Start Game ... Ready Data for GameManger");
            List<TeamInfo> teams = LobbyDataToTeamInfo();
            if (teams == null)
            {
                _msgPopUp.Open(
                    MessageType.Error,
                    "게임 시작에 실패하였습니다.\n다시 시도 해주세요.");
                Debug.LogWarning("[LobbyRoomUIController] Start Game ... Could not make team data.");
                return;
            }
            string roomId = ServiceLocator.Get<ILobbyManager>().GetRoomID();
            ServiceLocator.Get<IGameManager>().SetData(teams.ToArray(), roomId, _selectedMapNumber);
            if (IsHost)
            {
                Debug.Log("[LobbyRoomUIController] Start Game ... Change Scene");
                ServiceLocator.Get<ILobbyManager>().Lock(true);
                ServiceLocator.Get<INetworkSceneLoader>().LoadScene("InGame");
            }
        }
        else
        {
            _msgPopUp.Open(
                MessageType.Warning,
                "플레이어들의 상태를 확인해 주시기 바랍니다.");
            Debug.Log("[LobbyRoomUIController] Start Game ... Fail; CheckAllReady is False");
        }
    }

    private void CreateRelayHost()
    {
        Debug.Log("[LobbyRoomUIController] Create Relay Host ... ");
        var db = ServiceLocator.Get<IDatabaseBackend>();
        var lobby = ServiceLocator.Get<ILobbyManager>();
        var relay = ServiceLocator.Get<IRelayHostManager>();
        relay.StartHost().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogWarning("[LobbyRoomUIController] Could not create room !");
                _msgPopUp.Open(
                    MessageType.Error, 
                    "방 생성이 실패되었습니다.", 
                    "돌아가기", 
                    OnLeaveRoom);
            }
            _joinCode = task.Result;
            Debug.Log($"[LobbyRoomUIController] Create Relay Host ... Success ; JoinCode: {_joinCode}");
            UpdateClientIDToLobbyBackend();
            db.SetJoinCodeAsync(lobby.GetRoomID(), _joinCode);
        });
    }
    
    private void OnRightMap()
    {   // 방장만 제어 가능
        if (_msgPopUp.IsOpen) return;
        if (!IsHost) return;
        ServiceLocator.Get<IAudioService>().PlayButtonSfx();
        var db = ServiceLocator.Get<IDatabaseBackend>();
        var lobby = ServiceLocator.Get<ILobbyManager>();
        if (_mapImages.Count != 0)
        {
            int tempNumber = _selectedMapNumber + 1;
            if (tempNumber > _mapImages.Count - 1) tempNumber = 0;
            db.SetMapNumberAsync(lobby.GetRoomID(), tempNumber);
        }
    }

    private void OnLeftMap()
    {   // 방장만 제어 가능
        if (_msgPopUp.IsOpen) return;
        if (!IsHost) return;
        ServiceLocator.Get<IAudioService>().PlayButtonSfx();
        var db = ServiceLocator.Get<IDatabaseBackend>();
        var lobby = ServiceLocator.Get<ILobbyManager>();
        if (_mapImages.Count != 0)
        {
            int tempNumber = _selectedMapNumber + 1;
            if (tempNumber < 0) tempNumber = _mapImages.Count - 1;
            db.SetMapNumberAsync(lobby.GetRoomID(), tempNumber);
        }
    }

    private void GetMapNumberFromDB(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) {
            Debug.LogError($"[LobbyRoomUIController] Get MapNumber From DB Error: {args.DatabaseError.Message}");
            return;
        }

        if (args.Snapshot.Exists)
        {
            if (!int.TryParse(args.Snapshot.Value.ToString(), out int result))
            {
                Debug.LogError($"[LobbyRoomUIController] MapNumber Parsing Failed {args.Snapshot.Value}");
                return;
            }
            SelectedMapNumber = result;    
        }
        else
        {
            Debug.LogWarning("[LobbyRoomUIController] Get MapNumber Result Snapshot is not exist");
        }
    }
    
    private void OnRenderMap()
    {
        _selectedMapImage.sprite = _mapImages[_selectedMapNumber].image;
        _mapName.text = _mapImages[_selectedMapNumber].name;
    }
    
    private bool CheckAllReady()
    {
        var lobbyManager = ServiceLocator.Get<ILobbyManager>();
        int[] playersPerTeam = new int[4];
        foreach (var player in lobbyManager.GetPlayerList())
        {
            if (player.Data[LobbyPlayerDataKey.TEAM].Value == "0" || // 모든 유저가 팀에 속해야함.
                player.Data[LobbyPlayerDataKey.ROLE].Value == $"{PlayerRole.None}" || // 모든 유저가 Role 이 부여 되어있어야 함.
                player.Data[LobbyPlayerDataKey.READY].Value == "false") // 모든 유저가 Ready 를 해야함.
            {
                Debug.LogWarning($"[LobbyRoomUIController] CheckAllReady is False; {player.Data}");
                return false;
            }
            int teamNum = int.Parse(player.Data[LobbyPlayerDataKey.TEAM].Value) - 1;
            playersPerTeam[teamNum]++;
        }
        foreach (var i in playersPerTeam)
        {   // 모든 팀이 0명 혹은 2명이 배속 되어있어야함.
            if (i % 2 != 0)
            {
                Debug.LogWarning($"[LobbyRoomUIController] CheckAllReady is False; [{String.Join(", ", playersPerTeam)}]");
                return false;
            }
        }
        return true;
    }

    private async void HostMigrationProcessHandler()
    {
        Debug.Log("[LobbyRoomUIController] Change Relay Host ... ");
        var relay = ServiceLocator.Get<IRelayHostManager>();
        var lobby = ServiceLocator.Get<ILobbyManager>();
        var db = ServiceLocator.Get<IDatabaseBackend>();
        relay.Disconnect();
        _joinCode = string.Empty;
        db.SetJoinCodeAsync(lobby.GetRoomID(), RELAY_SYNC);
        try
        {
            bool updateDone = await WaitUntilSyncLobbyDataAsync();
            if (updateDone)
            {
                if (IsHost)
                {
                    Debug.Log("[LobbyRoomUIController] Change Relay Host ... I am a host");
                    CreateRelayHost();
                    ChangeButtonVisibility();
                }
                else
                {
                    Debug.Log("[LobbyRoomUIController] Change Relay Host ... I am not a host");
                    bool createRelay = await WaitUntilCreateRelayHostAsync();
                    if (createRelay)
                    {
                        _joinCode = await db.GetJoinCodeAsync(lobby.GetRoomID());
                        Debug.Log($"[LobbyRoomUIController] Change Relay Host ... Try connect to host. {_joinCode}");
                        relay.StartClient(_joinCode);
                    }
                }
                Debug.Log("[LobbyRoomUIController] Change Relay Host ... Success");
            } 
        }
        catch (Exception e)
        {
            Debug.LogError($"[LobbyRoomUIController] Change Relay Host ... Fail {e.Message}");
        }
    }

    private async Task<bool> WaitUntilSyncLobbyDataAsync()
    {
        Debug.Log("[LobbyRoomUIController] Lobby Data Sync ... ");
        var lobby = ServiceLocator.Get<ILobbyManager>();
        int retryCount = 0;
        int maxRetryCount = 5;
        while (lobby.IsUpdating())
        {
            if (retryCount++ > maxRetryCount)
            {
                Debug.LogWarning("[LobbyRoomUIController] Lobby Data Sync ... Fail");
                return false;
            }
            await Task.Delay(1000);
        }
        Debug.Log("[LobbyRoomUIController] Lobby Data Sync ... Success");
        return true;
    }

    private async Task<bool> WaitUntilCreateRelayHostAsync()
    {
        var lobby = ServiceLocator.Get<ILobbyManager>();
        var db = ServiceLocator.Get<IDatabaseBackend>();
        int retryCount = 0;
        int maxRetryCount = 5;
        while (true)
        {
            var code = await db.GetJoinCodeAsync(lobby.GetRoomID());
            if (retryCount++ > maxRetryCount) return false;
            if (code != RELAY_SYNC) return true;
            await Task.Delay(1000);
        }
    }

    private void UpdateClientIDToLobbyBackend()
    {
        var userInfo = ServiceLocator.Get<IUserInfoManager>().GetUserInfo();
        var relay = ServiceLocator.Get<IRelayHostManager>();
        var lobby = ServiceLocator.Get<ILobbyManager>();
        List<(string key, string value)> updateData = new();
        Debug.Log($"[LobbyRoomUIController] {userInfo.userId} ClientID is {relay.GetClientId()}");
        updateData.Add((LobbyPlayerDataKey.CLIENT_ID, $"{relay.GetClientId()}"));
        lobby.UpdatePlayerData(updateData).ContinueWithOnMainThread(task =>
        {
            if (!task.IsFaulted) return;
            Debug.LogWarning($"[LobbyRoomUIController] Update {userInfo.userId} ClientID ... Fail");
            Debug.LogError(task.Exception);
        });
    }
}
