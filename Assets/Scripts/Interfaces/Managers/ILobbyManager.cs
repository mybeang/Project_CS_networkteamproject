using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;

public interface ILobbyManager
{
	public Task<List<Lobby>> GetRoomList(int offset);
	public Task<List<Lobby>> RefreshRoomList();
    public Task JoinRoom(string roomId);
    public Task<bool> QuickJoinRoom();
    public Task CreateRoom(string subject);
    public Task LeaveRoom();
    public List<Player> GetPlayerList();
    public Dictionary<string, string> GetMyPlayerData();
    public Task UpdatePlayerData(List<(string key, string value)> updateData);
    public string GetRoomID();
    public string GetRoomName();
    public string GetHostId();
    public void LobbyDataOnChangedAddListener(Action<Lobby> callback);
    public void LobbyDataOnChangedRemoveListener(Action<Lobby> callback);
    public void Lock(bool isLock);
    public bool IsUpdating();
}