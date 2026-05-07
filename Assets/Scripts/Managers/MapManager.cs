using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


[Serializable]
public struct MapData
{
    public GameObject mapObject;
    public List<Transform> startPoints;
}


[RequireComponent(typeof(NetworkObject))]
public class MapManager : NetworkManager<MapManager>, IMapManager
{
    [SerializeField] private List<MapData> _mapObjects;
    private MapData _selectedMapData;
    private NetworkVariable<Vector3> _team1Pos = new(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<Vector3> _team2Pos = new(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<Vector3> _team3Pos = new(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<Vector3> _team4Pos = new(writePerm: NetworkVariableWritePermission.Owner);
    
    protected override void Register() => ServiceLocator.Register<IMapManager>(this);
    protected override void Unregister() => ServiceLocator.Unregister<IMapManager>();

    public void SelectMap(int mapId)
    {
        _selectedMapData = _mapObjects[mapId];
        _selectedMapData.mapObject.SetActive(true);
    }
    
    public Vector3 GetStartPoint(PlayerTeamEnum playerTeam)
    {
        var posList = _selectedMapData.startPoints;
        return new Vector3();
    }

    public void Restore()
    {
        _selectedMapData.mapObject.SetActive(false);
    }
}
