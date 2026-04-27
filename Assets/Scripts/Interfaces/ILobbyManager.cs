public interface ILobbyManager
{
	public void GetRoomList();
	public void RefreshRoomList();
    public void JoinRoom(string roomId);
    public void QuickJoinRoom(string roomId);
    public void CreateRoom(string roomId, string subject);
}