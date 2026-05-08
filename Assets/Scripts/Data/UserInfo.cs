using System;
using System.Collections.Generic;

[Serializable]
public class UserInfo
{
    public string userId = "";  // on DB
    public string roomId;
    public PlayerTeamEnum teamNum;
    public PlayerRole Role;
    public int score;
    public ulong clientId;
}
