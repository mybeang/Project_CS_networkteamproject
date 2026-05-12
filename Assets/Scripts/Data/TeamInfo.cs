using System.Collections.Generic;

[System.Serializable]
public class PlayerInfo
{
    public string userId;
    public ulong clientId;
    public PlayerRole role;
}

[System.Serializable]
public class TeamInfo
{
    public List<PlayerInfo> players;
    public PlayerTeamEnum teamNum;
    public PlayerableVehicleEnum vehicle;
    public int score;
    
    public TeamInfo(PlayerTeamEnum teamNum, PlayerableVehicleEnum vehicle)
    {
        this.teamNum = teamNum;
        this.vehicle = vehicle;
        score = 0;
    }
    /// <summary>
    /// 특정 팀의 전체 정보가 필요한 경우 호출
    /// </summary>
    /// <returns></returns>
    public TeamInfo GetTeamInfo() => this;

    public string ToPrettyString()
    {
        string text = "---- Team Info ----";
        text += $"\nteam: {teamNum}\n";
        text += $"vehicle: {vehicle}\n";
        text += $"score: {score}\n";
        for (int i = 0; i < players.Count; i++)
            text += $"player{i + 1}: id:{players[i].userId} | cid:{players[i].clientId} | role:{players[i].role}\n";
        text += "============";
        return text;
    }
}
