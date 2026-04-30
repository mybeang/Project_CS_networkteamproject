using System;
using Unity.Netcode;

public interface IGameManager
{
    public void StartGame(TeamInfo[] teams, in string roomID, int mapNumber);
    [ServerRpc] public void OnDestoryVehicleServerRpc(PlayerTeamEnum self, PlayerTeamEnum enemy);

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
}
