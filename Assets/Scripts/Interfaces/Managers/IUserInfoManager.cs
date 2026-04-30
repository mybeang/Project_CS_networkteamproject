public interface IUserInfoManager
{
    public UserInfo GetUserInfo();   // User 정보 전체 가져오기
    public void SetUserId(string userId);
    public void SetRoomId(string roomId);
    public void SetTeamNum(int teamNum);
    public void SetIsDriver(bool isDriver);
    public bool IsDriver();
    public void AddScore(int score);
    public int GetScore();
    public void SetClientId(ulong clientId);
}