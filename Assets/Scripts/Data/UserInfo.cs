using System;
using System.Collections.Generic;

[Serializable]
public class UserInfo
{
    public string userId;  // on DB
    public string roomId;
    public int teamNum;
    public bool isDriver;
    public int score;
    public ulong clientId;
}
