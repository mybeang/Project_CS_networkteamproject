using Unity.Netcode;

public interface IGameManager
{
    public void StartGame(teamInfo[] teams, in string roomID);
    [ServerRpc] public void OnDestoryVehicleServerRpc(playerTeamEnum self, playerTeamEnum enemy);
}
