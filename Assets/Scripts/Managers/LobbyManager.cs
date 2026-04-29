using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyManager : Manager<LobbyManager>, ILobbyManager
{
    private const int MAX_PLAYERS = 8;
    private const float HEART_BEAT_TIME = 15f;
    private Lobby _lobby;
    public string RoomID => _lobby.Id;
    private Coroutine _heartbeatCoroutine;

    protected override async void Init() => await UnityServiceInitialize.Processing();
    protected override void Register() => ServiceLocator.Register<ILobbyManager>(this);
    protected override void Unregister() => ServiceLocator.Unregister<ILobbyManager>();

    private void PrintError(string message) => Debug.LogError($"[LobbyManager]\n{message}");
    
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
        
        data.Add("UserID", CreateDataObject<T>(userInfo.userId));
        data.Add("Team", CreateDataObject<T>(""));
        data.Add("Role", CreateDataObject<T>(""));
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
            options.Player = new Player{Data = GetMyDataFormat<PlayerDataObject>()};
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
        options.Data = GetMyDataFormat<DataObject>();
        try
        {
            _lobby = await LobbyService.Instance.CreateLobbyAsync(subject, MAX_PLAYERS, options);
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

    public void UpdatePlayerData(string key, string value)
    {
        foreach (Player player in _lobby.Players)
        {
            if (player.Id == AuthenticationService.Instance.PlayerId)
            {
                player.Data[key].Value = value;
                return;
            }
        }
    }

    private IEnumerator HeartBeatCoroutine()
    {
        var delay = new WaitForSecondsRealtime(HEART_BEAT_TIME);
        while (_lobby != null)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(_lobby.Id);
            yield return delay;
        }
    }
}
