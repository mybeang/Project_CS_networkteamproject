using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


[Serializable]
public struct MapData
{
    public GameObject _mapObject;
    public List<Transform> _startPoints;
}


[RequireComponent(typeof(NetworkObject))]
public class MapManager : NetworkManager<MapManager>, IMapManager
{
    [SerializeField] private List<MapData> _mapObjects;
    private MapData _selectedMapData;
    
    protected override void Register() => ServiceLocator.Register<IMapManager>(this);
    protected override void Unregister() => ServiceLocator.Unregister<IMapManager>();

    public void SelectMap(int mapId)
    {
        _selectedMapData = _mapObjects[mapId];
    }

    public Vector3 GetStartPoint()
    {
        // 
        return new Vector3();
    }
}
