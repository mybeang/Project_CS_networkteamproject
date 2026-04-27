public interface IRoomManager  
{   
    public void StartGame();  // (only Host) 
    public void FinishGame();  // go to Room 
    public void LeaveRoom();  // go to Lobby
    public void Ready(bool isReady);  // Ready
    public void MoveTeam(int teamNum);   // Team Change
    public void SetPart(bool isDriver);   // Move Driver / Attacker
    public void ChangeRoomSubject(string subject);  // (only Host) 방제 변경
    public void ChangeMap();  // ToDo. Map 이 여러개 일때 구현하기
}
