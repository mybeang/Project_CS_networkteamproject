public interface IUserInfoManager
{
    public UserInfo GetUserInfo();   // User 정보 전체 가져오기
    public void SetUserId(string userId);
    public void SetRoomId(string roomId);
    public void SetTeamNum(PlayerTeamEnum teamNum);
    public void SetIsDriver(PlayerRole isDriver);
    public void AddScore(int score);
    public int GetScore();
    public void SetClientId(ulong clientId);
}