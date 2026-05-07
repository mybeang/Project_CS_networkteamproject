using System;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
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

    private Vector3 CandidateStartPos(int teamNum)
    {
        var teamPosArray = new Vector3[] {_team1Pos.Value, _team2Pos.Value, _team3Pos.Value, _team4Pos.Value};
        int maxTryCnt = _selectedMapData.startPoints.Count;
        for (int cnt = 0; cnt < maxTryCnt; cnt++)
        {
            var index = UnityEngine.Random.Range(0, _selectedMapData.startPoints.Count);
            var selPos = _selectedMapData.startPoints[index].position;
            bool hit = false;
            for (int i = 0; i < teamPosArray.Length; i++)
            {
                if (teamNum == i) continue;
                if (Vector3.Distance(teamPosArray[i], selPos) <= 0.1f)
                {
                    hit = true;
                    break;
                }
            }
            if (!hit) return selPos;    
        }
        return teamPosArray[teamNum];
    }
    
    public Vector3 GetStartPoint(PlayerTeamEnum playerTeam)
    {
        Vector3 pos = Vector3.zero;
        switch (playerTeam)
        {
            case PlayerTeamEnum.firstTeam:
                pos = CandidateStartPos(0);
                _team1Pos.Value = pos;
                break;
            case PlayerTeamEnum.secondTeam:
                pos = CandidateStartPos(1);
                _team1Pos.Value = pos;
                break;
            case PlayerTeamEnum.thirdTeam:
                pos = CandidateStartPos(2);
                _team1Pos.Value = pos;
                break;
            case PlayerTeamEnum.fourthTeam:
                pos = CandidateStartPos(3);
                _team1Pos.Value = pos;
                break;
        }
        return pos;
    }

    public void Restore()
    {
        _selectedMapData.mapObject.SetActive(false);
    }
}
