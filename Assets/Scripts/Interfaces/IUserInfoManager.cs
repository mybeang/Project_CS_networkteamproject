public interface IUserInfoManager
{
    public void GetUserInfo();   // User 정보 전체 가져오기
    public bool IsDriver();
    public void AddScore(int score);
    public int GetScore();
}