using System;
using Unity.Netcode;

public interface IGameManager
{
    public void StartGame(teamInfo[] teams, in string roomID, int mapNumber);
    [ServerRpc] public void OnDestoryVehicleServerRpc(PlayerTeamEnum self, PlayerTeamEnum enemy);

    /// <summary>
    /// self, enemy 순서
    /// 받아올 때 주의할 것
    /// </summary>
    public event Action<PlayerTeamEnum, PlayerTeamEnum> OnKillLog;
}
