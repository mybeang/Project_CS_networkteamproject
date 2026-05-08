
using System.Collections.Generic;

public class PlayerInfo
{
    public string userId;
    public ulong clientId;
    public PlayerRole role;
}

public class TeamInfo
{
    public List<PlayerInfo> players;
    private PlayerTeamEnum _teamNum;
    private PlayerableVehicleEnum _vehicle;
    private int _score;
    
    public TeamInfo(PlayerTeamEnum teamNum, PlayerableVehicleEnum vehicle)
    {
        _teamNum = teamNum;
        _vehicle = vehicle;
        _score = 0;
    }

    public void SetScore(int score) => _score = score;
    public int GetScore() => _score;
    public PlayerTeamEnum GetTeamNum() => _teamNum;
    public PlayerableVehicleEnum GetVehicle() => _vehicle;
    /// <summary>
    /// 특정 팀의 전체 정보가 필요한 경우 호출
    /// </summary>
    /// <returns></returns>
    public TeamInfo GetTeamInfo() => this;
}
