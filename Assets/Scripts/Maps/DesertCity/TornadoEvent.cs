using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class WayPoint
{
    public List<Vector3> positions;
}

public class TornadoEvent : EventTask
{
    [SerializeField] private GameObject _tornadoPrefab;
    [SerializeField] private List<WayPoint> _wayPoints;
    [SerializeField][Range(3, 20)] private int _MaxTornados;
    [SerializeField][Range(3, 20)] private int[] _numOfSpawns;
    [SerializeField][Range(0.5f, 2f)] private float _tornadoMinSpeed;
    [SerializeField][Range(5f, 20f)] private float _tornadoMaxSpeed;
    private Queue<GameObject> _pool = new();
    private List<GameObject> _activeTorndos = new();
    private Vector3 _nxWayPoint;
    private int index;

    private void Awake() => Init();
    
    private void Init()
    {
        for (int i = 0; i < _MaxTornados; i++)
        {
            GameObject newTornado = Instantiate(_tornadoPrefab, transform);
            newTornado.transform.parent = transform;
            newTornado.SetActive(false);
            _pool.Enqueue(newTornado);
        }
    } 
        
    private List<Vector3> SelectWayPoint()
    {
        int index = UnityEngine.Random.Range(0, _wayPoints.Count);
        return _wayPoints[index].positions;
    }
    
    private void SpawnTornado()
    {
        if (!IsServer) return;
        Debug.Log("[TornadoEvent] Spawning Tornado");
        for (int i = 0; i < _numOfSpawns[index++]; i++)
        {
            if (i > _MaxTornados) break;
            GameObject tornado = _pool.Dequeue();
            TornadoMovement tm = tornado.GetComponent<TornadoMovement>();
            float speed = UnityEngine.Random.Range(_tornadoMinSpeed, _tornadoMaxSpeed);
            var wayPoint = new WayPoint() { positions = SelectWayPoint() };
            string wayPointJson =  JsonUtility.ToJson(wayPoint);
            int startIndex = UnityEngine.Random.Range(0, wayPoint.positions.Count - 1);
            tornado.SetActive(true);
            tornado.GetComponent<NetworkObject>().Spawn();
            tm.InitClientRpc(wayPointJson, startIndex, speed);
            _activeTorndos.Add(tornado);    
        }
    }

    private void DespawnAllTornado()
    {
        if (!IsServer) return;
        Debug.Log("[TornadoEvent] Despawning Tornado");
        foreach (GameObject tornado in _activeTorndos)
        {
            tornado.GetComponent<NetworkObject>().Despawn();
            tornado.SetActive(false);
            _pool.Enqueue(tornado);
        }
        _activeTorndos.Clear();
    }

    public override void OnEventSpawn() => SpawnTornado();
    public override void OnEventDespawn() => DespawnAllTornado();
        
    [ContextMenu("SpawnTornado")]
    public void TestSpawnTornado() => SpawnTornado();
    
    [ContextMenu("DespawnTornado")]
    public void TestDespawnTornado() => DespawnAllTornado();

}