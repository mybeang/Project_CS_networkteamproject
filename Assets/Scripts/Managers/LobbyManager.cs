using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Extensions;
using Unity.Collections;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Multiplayer;
using UnityEngine;

public class LobbyPlayerDataKey
{
    public const string USER_ID = "UserID";
    public const string CLIENT_ID = "ClientID";
    public const string TEAM = "Team";
    public const string ROLE = "Role";
    public const string READY = "Ready";
}


public class LobbyManager : Manager<LobbyManager>, ILobbyManager
{
    private const int MAX_PLAYERS = 8;
    private const float HEART_BEAT_TIME = 15f;
    private Lobby _lobby;
    private Action<Lobby> _onChangeLobbyData;
    public Lobby Lobby
    {
        get => _lobby;
        set
        {
            _lobby = value;
            _onChangeLobbyData?.Invoke(value);
        }
    }
    private Coroutine _heartbeatCoroutine;
    private LobbyEventCallbacks _callbacks = new ();
    private ILobbyEvents _lobbyEvent;
    private bool _isUpdating;
    // MPPM 환경에서 SingleTone 은 모두 같은 Session? 을 공유하는 것 같다.
    // 이것 때문에 데이터 업데이트가 발생하지 않는 케이스가 있다.
    // 관련하여 문제 발생시 다시 실행하기 위한 queue 를 아래와 같이 정의한다.
    private Queue<bool> _retryQueue = new();
    private Coroutine _retryCoroutine;

    protected override async void Init()
    {
        Debug.Log("[LobbyManager] Initializing LobbyManager");
        await UnityServiceInitialize.Processing();
    }

    protected override void Register() => ServiceLocator.Register<ILobbyManager>(this);
    protected override void Unregister() => ServiceLocator.Unregister<ILobbyManager>();

    private void PrintError(string message) => Debug.LogError($"[LobbyManager]\n{message}");

    private async void AddListenersForLobbyEventCallbacks()
    {
        if (_retryCoroutine == null) _retryCoroutine = StartCoroutine(RetryQueueCoroutine());
        _callbacks.PlayerJoined += PlayerJoinedHandler;
        _callbacks.PlayerLeft += PlayerLeftHandler;
        _callbacks.PlayerDataChanged += PlayerDataChangedHandler;
        _callbacks.LobbyChanged += LobbyDataChangedHander;
        _lobbyEvent = await LobbyService.Instance.SubscribeToLobbyEventsAsync(_lobby.Id, _callbacks);
    }
    
    private async void RemoveListenersForLobbyEventCallbacks()
    {
        if (_retryCoroutine != null)
        {
            StopCoroutine(_retryCoroutine);
            _retryCoroutine = null;
        }
        _callbacks.PlayerJoined -= PlayerJoinedHandler;
        _callbacks.PlayerLeft -= PlayerLeftHandler;
        _callbacks.PlayerDataChanged -= PlayerDataChangedHandler;
        _callbacks.LobbyChanged -= LobbyDataChangedHander;
        await _lobbyEvent.UnsubscribeAsync();
    }
    
    public async Task<List<Lobby>> GetRoomList(int offset = 0)
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 4;
            options.Skip = offset;
            options.Filters = new List<QueryFilter>()
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    value: "0",
                    op: QueryFilter.OpOptions.GT)
            };
            
            options.Order = new List<QueryOrder>
            {
                new QueryOrder(asc: false, field: QueryOrder.FieldOptions.Created)
            };
            
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
            return response.Results;
        }
        catch (LobbyServiceException e)
        {
            PrintError(e.Message);
            return null;
        }
    }
    
    public Task<List<Lobby>> RefreshRoomList() => GetRoomList(0);

    private Dictionary<string, T> MyDataFormat<T>() where T : class
    {
        Dictionary<string, T> data = new();
        UserInfo userInfo = ServiceLocator.Get<IUserInfoManager>()?.GetUserInfo();
        if (userInfo == null)
        {
            Debug.LogError("[LobbyManager] UserInfo not found");
            return data;
        }
        
        data.Add(LobbyPlayerDataKey.USER_ID, CreateDataObject<T>(userInfo.userId));
        data.Add(LobbyPlayerDataKey.TEAM, CreateDataObject<T>("0"));
        data.Add(LobbyPlayerDataKey.CLIENT_ID, CreateDataObject<T>("0"));
        data.Add(LobbyPlayerDataKey.ROLE, CreateDataObject<T>($"{PlayerRole.None}"));
        data.Add(LobbyPlayerDataKey.READY, CreateDataObject<T>("false"));  // string false/true
        return data;
    }
    
    private T CreateDataObject<T>(string value) where T : class
    {
        if (typeof(T) == typeof(DataObject))
            return new DataObject(DataObject.VisibilityOptions.Public, value) as T;
        
        if (typeof(T) == typeof(PlayerDataObject))
            return new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, value) as T;

        return null;
    }
    
    public async Task JoinRoom(string roomId)
    {
        try
        {
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions();
            options.Player = new Player { Data = MyDataFormat<PlayerDataObject>() };
            _lobby = await LobbyService.Instance.JoinLobbyByIdAsync(roomId, options);
            AddListenersForLobbyEventCallbacks();
            ServiceLocator.Get<IUserInfoManager>()?.SetRoomId(_lobby.Id);
            if (_heartbeatCoroutine != null)
            {
                StopCoroutine(_heartbeatCoroutine);
                _heartbeatCoroutine = null;
            }
            StartCoroutine(HeartBeatCoroutine());
        } 
        catch (LobbyServiceException e)
        {
            PrintError(e.Message);
        }
    }

    public async Task QuickJoinRoom()
    {
        // ToDo. 필요시 추후 구현.
    }

    public async Task CreateRoom(string subject)
    {
        CreateLobbyOptions options = new CreateLobbyOptions();
        options.IsPrivate = false;  // 공개방
        options.Player = new Player { Data = MyDataFormat<PlayerDataObject>() };
        try
        {
            Debug.Log("[LobbyManager] Try Creating room...");
            _lobby = await LobbyService.Instance.CreateLobbyAsync(subject, MAX_PLAYERS, options);
            AddListenersForLobbyEventCallbacks();
            Debug.Log("[LobbyManager] Update _lobby");
            ServiceLocator.Get<IUserInfoManager>()?.SetRoomId(_lobby.Id);
            if (_heartbeatCoroutine != null)
            {
                StopCoroutine(_heartbeatCoroutine);
                _heartbeatCoroutine = null;
            }
            StartCoroutine(HeartBeatCoroutine());
            Debug.Log("[LobbyManager] Try Creating room ... Done");
        }
        catch (LobbyServiceException e)
        {
            PrintError(e.Message);
        }
    }
    
    public async Task LeaveRoom()
    {
        try
        {
            string playerId = AuthenticationService.Instance.PlayerId;
            RemoveListenersForLobbyEventCallbacks();
            await LobbyService.Instance.RemovePlayerAsync(_lobby.Id, playerId);
            bool isExistLobby = await CheckLobbyExist(_lobby.Id);
            if (!isExistLobby)
            {
                var db = ServiceLocator.Get<IDatabaseBackend>();
                db.RemoveJoinCodeAsync(_lobby.Id);
                db.RemoveMapNumberAsync(_lobby.Id);
            }
            _lobby = null;
            _lobbyEvent = null;
            ServiceLocator.Get<IUserInfoManager>()?.SetRoomId(null);
            if (_heartbeatCoroutine != null)
            {
                StopCoroutine(_heartbeatCoroutine);
                _heartbeatCoroutine = null;
            }
        }
        catch (LobbyServiceException e)
        {
            PrintError(e.Message);
        }
    }

    private async Task<bool> CheckLobbyExist(string lobbyId)
    {
        var lobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
        return lobby != null;
    }
    
    public List<Player> GetPlayerList() => _lobby.Players;

    public Dictionary<string, string> GetMyPlayerData()
    {
        foreach (Player player in _lobby.Players)
        {
            if (player.Id == AuthenticationService.Instance.PlayerId)
            {
                Dictionary<string, string> data = new();
                foreach ((string key, PlayerDataObject value) in player.Data)
                    data.Add(key, value.Value);
                return data;
            }
        }
        return null;
    }

    public async Task UpdatePlayerData(List<(string key, string value)> updateData)
    {
        foreach (Player player in _lobby.Players)
        {
            Debug.Log("[LobbyManager] Find Player ...");
            if (player.Id == AuthenticationService.Instance.PlayerId)
            {
                Debug.Log("[LobbyManager] Find Player ... Matched");
                Dictionary<string, PlayerDataObject> data = new();
                foreach ((string key, string value) in updateData)
                {
                    player.Data[key].Value = value;
                    data.Add(key, player.Data[key]);
                }
                var options = new UpdatePlayerOptions { Data = data};
                Debug.Log("[LobbyManager] Player Update ...");
                await LobbyService.Instance.UpdatePlayerAsync(_lobby.Id, player.Id, options);
                Debug.Log("[LobbyManager] Player Update ... Done");
                return;
            }
        }
    }

    private IEnumerator HeartBeatCoroutine()
    {
        Debug.Log("[LobbyManager] Start to heart beat");
        var delay = new WaitForSecondsRealtime(HEART_BEAT_TIME);
        while (_lobby != null)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(_lobby.Id);
            yield return delay;
        }
    }

    private async void UpdateDataHandler()
    {
        Debug.Log("[LobbyManager] Start to update data ... ");
        if (_isUpdating)
        {
            Debug.Log("[LobbyManager] Start to update data ... Already Updating");
            return;
        }
        _isUpdating = true;
        try
        {
            Lobby = await LobbyService.Instance.GetLobbyAsync(_lobby.Id);
            _retryQueue.Clear();  // 정상 Update 시 queue 를 지운다.
        }
        catch (Exception e)
        {
            _retryQueue.Enqueue(true);
            Debug.LogWarning(e.Message);
        }
        
        _isUpdating = false;
        Debug.Log("[LobbyManager] Start to update data ... Done");
    }
    
    public bool IsUpdating() => _isUpdating;
    
    private void PlayerJoinedHandler(List<LobbyPlayerJoined> _list) => UpdateDataHandler();
    private void PlayerLeftHandler(List<int> _list) => UpdateDataHandler();
    private void PlayerDataChangedHandler(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> _dict) => UpdateDataHandler();
    private void LobbyDataChangedHander(ILobbyChanges chg) => UpdateDataHandler();
    
    public string GetRoomID() => _lobby.Id;
    public string GetRoomName() => _lobby.Name;

    public string GetHostId()
    {
        foreach (Player player in _lobby.Players)
        {
            if (player.Id == _lobby.HostId)
            {
                return player.Data[LobbyPlayerDataKey.USER_ID].Value;
            }
        }
        return "";
    }
    
    public void LobbyDataOnChangedAddListener(Action<Lobby> callback) => _onChangeLobbyData += callback;
    public void LobbyDataOnChangedRemoveListener(Action<Lobby> callback) => _onChangeLobbyData -= callback;

    public void Lock(bool isLock)
    {
        UpdateLobbyOptions options = new UpdateLobbyOptions
        {
            IsLocked = isLock,
            IsPrivate = isLock
        };
        LobbyService.Instance.UpdateLobbyAsync(_lobby.Id, options).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Lobby State Change ... Fail");
            }
            Debug.Log($"Lobby State Change to {(isLock ? "lock" : "unlock")}... Success");
        });
    }

    private IEnumerator RetryQueueCoroutine()
    {
        while (true)
        {
            if (_retryQueue.Count > 0)
            {
                Debug.Log("[LobbyManager] Exist Update Data for retry.");
                UpdateDataHandler();
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
}

