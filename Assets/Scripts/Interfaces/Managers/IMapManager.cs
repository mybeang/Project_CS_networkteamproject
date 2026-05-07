using UnityEngine;

public interface IMapManager
{
    public void SelectMap(int mapId);
    public Vector3 GetStartPoint();
}