using UnityEngine;

public interface IMapManager
{
    public void SelectMap(int mapId);
    public void Restore();
    public Vector3 GetStartPoint(PlayerTeamEnum playerTeam);
}