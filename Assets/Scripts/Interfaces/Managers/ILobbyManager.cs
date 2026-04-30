using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;

public interface ILobbyManager
{
	public Task<List<Lobby>> GetRoomList(int offset);
	public Task<List<Lobby>> RefreshRoomList();
    public Task JoinRoom(string roomId);
    public Task QuickJoinRoom();
    public Task CreateRoom(string subject);
    public Task LeaveRoom();
    public List<Player> GetPlayerList();
    public void UpdatePlayerData(string key, string value);
}