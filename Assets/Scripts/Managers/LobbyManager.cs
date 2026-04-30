using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyPlayerDataKey
{
    public const string USER_ID = "UserID";
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
    
    protected override async void Init() => await UnityServiceInitialize.Processing();

    protected override void Register()
    {
        ServiceLocator.Register<ILobbyManager>(this);
        AddListenersForLobbyEventCallbacks();
    }

    protected override void Unregister()
    {
        ServiceLocator.Unregister<ILobbyManager>();
        RemoveListenersForLobbyEventCallbacks();
    }

    private void PrintError(string message) => Debug.LogError($"[LobbyManager]\n{message}");

    private void AddListenersForLobbyEventCallbacks()
    {
        _callbacks.PlayerJoined += PlayerJoinedHandler;
        _callbacks.PlayerLeft += PlayerLeftHandler;
        _callbacks.PlayerDataChanged += PlayerDataChangedHandler;
    }
    
    private void RemoveListenersForLobbyEventCallbacks()
    {
        _callbacks.PlayerJoined -= PlayerJoinedHandler;
        _callbacks.PlayerLeft -= PlayerLeftHandler;
        _callbacks.PlayerDataChanged -= PlayerDataChangedHandler;
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

    private Dictionary<string, T> GetMyDataFormat<T>() where T : class
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
        data.Add(LobbyPlayerDataKey.ROLE, CreateDataObject<T>(nameof(PlayerRole.None)));
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
            options.Player = new Player { Data = GetMyDataFormat<PlayerDataObject>() };
            _lobby = await LobbyService.Instance.JoinLobbyByIdAsync(roomId, options);
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
        options.Player = new Player { Data = GetMyDataFormat<PlayerDataObject>() };
        try
        {
            Debug.Log("[LobbyManager] Try Creating room...");
            _lobby = await LobbyService.Instance.CreateLobbyAsync(subject, MAX_PLAYERS, options);
            Debug.Log("[LobbyManager] Update _lobby");
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
    
    public async Task LeaveRoom()
    {
        try
        {
            string playerId = AuthenticationService.Instance.PlayerId;
            await LobbyService.Instance.RemovePlayerAsync(_lobby.Id, playerId);
            
            _lobby = null;
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

    public async void UpdatePlayerData(List<(string key, string value)> updateData)
    {
        foreach (Player player in _lobby.Players)
        {
            if (player.Id == AuthenticationService.Instance.PlayerId)
            {
                Dictionary<string, PlayerDataObject> data = new();
                foreach ((string key, string value) in updateData)
                {
                    player.Data[key].Value = value;
                    data.Add(key, player.Data[key]);
                }
                var options = new UpdatePlayerOptions { Data = data};
                await LobbyService.Instance.UpdatePlayerAsync(_lobby.Id, player.Id, options);
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

    private async void UpdateDataHandler() => Lobby = await LobbyService.Instance.GetLobbyAsync(_lobby.Id);
    
    private void PlayerJoinedHandler(List<LobbyPlayerJoined> _list) => UpdateDataHandler();
    private void PlayerLeftHandler(List<int> _list) => UpdateDataHandler();
    private void PlayerDataChangedHandler(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> _dict) => UpdateDataHandler();
    
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
}

