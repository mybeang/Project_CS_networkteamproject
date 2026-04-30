public class UserInfoManager : Manager<UserInfoManager>, IUserInfoManager
{
    private UserInfo _userInfo;
    
    protected override void Init()
    {
        _userInfo = new UserInfo();
    }
    
    protected override void Register() => ServiceLocator.Register<IUserInfoManager>(this);
    protected override void Unregister() => ServiceLocator.Unregister<IUserInfoManager>();

    public UserInfo GetUserInfo() => _userInfo;
    public void SetUserId(string userId) => _userInfo.userId = userId;
    public void SetRoomId(string roomId) => _userInfo.roomId = roomId;
    public void SetTeamNum(int teamNum) => _userInfo.teamNum = teamNum;
    public void SetIsDriver(bool isDriver) => _userInfo.isDriver = isDriver;
    public bool IsDriver() => _userInfo.isDriver;
    public void AddScore(int score) => _userInfo.score += score;
    public int GetScore() => _userInfo.score;
    public void SetClientId(ulong clientId) => _userInfo.clientId = clientId;
}