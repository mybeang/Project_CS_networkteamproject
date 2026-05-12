using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public interface IGameManager
{
    public void StartGame();
    [ServerRpc] public void OnDestoryVehicleServerRpc(PlayerTeamEnum myTeam, PlayerTeamEnum enemy);

    public void AddEventSchedule(EventScheduleManager eventSchedulemanager);

    /// <summary>
    /// self, enemy 순서
    /// 받아올 때 주의할 것
    /// </summary>
    public event Action<PlayerTeamEnum, PlayerTeamEnum> OnKillLog;
    
    /// <summary>
    /// 게임 시간을 감소형태로 받아옴
    /// 초단위로 넘겨줌.
    /// </summary>
    public event Action<int> OnChangeTime;

    /// <summary>
    /// 스코어가 바뀐 경우 바뀐 스코어를 전달하기 위한 함수.
    /// int 배열 4개가 들어올 예정 (int[4])
    /// </summary>
    public event Action<int[]> OnChangeScore;

    public void SetData(TeamInfo[] teams, in string roomID, int mapNumber);
    
    public TeamInfo[] GetTeams();
    public TeamInfo GetMyTeamInfo(PlayerTeamEnum myTeamNum);
    public Dictionary<PlayerTeamEnum, GameObject> GetPlayableObjects();
}
