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
    [SerializeField][Range(3, 20)] private int _maxTornados;
    [SerializeField][Range(3, 20)] private int[] _numOfSpawns;
    [SerializeField][Range(1f, 2f)] private float _tornadoMinSpeed;
    [SerializeField][Range(5f, 20f)] private float _tornadoMaxSpeed;
    private Queue<GameObject> _pool = new();
    private List<GameObject> _activeTorndos = new();
    private Vector3 _nxWayPoint;
    private int index;

    private void Awake() => Init();
    
    private void OnDisable() => CleanUp();
    
    private void Init()
    {
        index = 0;
        for (int i = 0; i < _maxTornados; i++)
        {
            GameObject newTornado = Instantiate(_tornadoPrefab, transform);
            newTornado.transform.parent = transform;
            newTornado.SetActive(false);
            _pool.Enqueue(newTornado);
        }
    }

    private void CleanUp()
    {
        for (int i = 0; i < _maxTornados; i++)
        {
            var go = _pool.Dequeue();
            Destroy(go);
        }
        _pool.Clear();
    }
        
    private List<Vector3> SelectWayPoint()
    {
        int index = UnityEngine.Random.Range(0, _wayPoints.Count);
        return _wayPoints[index].positions;
    }
    
    private void SpawnTornado()
    {
        Debug.Log($"[TornadoEvent] Spawning Tornado - {index + 1}");
        if (!IsServer) return;
        for (int i = 0; i < _numOfSpawns[index]; i++)
        {
            if (i > _maxTornados) break;
            GameObject tornado = _pool.Dequeue();
            TornadoMovement tm = tornado.GetComponent<TornadoMovement>();
            float speed = UnityEngine.Random.Range(_tornadoMinSpeed, _tornadoMaxSpeed);
            var wayPoint = new WayPoint() { positions = SelectWayPoint() };
            string wayPointJson =  JsonUtility.ToJson(wayPoint);
            int startIndex = UnityEngine.Random.Range(0, wayPoint.positions.Count - 1);
            Debug.Log($"[TornadoEvent] Spawning Tornado[{i}] ... Start Index: {startIndex}");
            Debug.Log($"[TornadoEvent] Spawning Tornado[{i}] ... Speed : {speed}");
            tornado.SetActive(true);
            tornado.GetComponent<NetworkObject>().Spawn();
            tm.InitClientRpc(wayPointJson, startIndex, speed);
            _activeTorndos.Add(tornado);
        }
        index++;
    }

    private void DespawnAllTornado()
    {
        Debug.Log("[TornadoEvent] Despawning Tornado");
        if (!IsServer) return;
        foreach (GameObject tornado in _activeTorndos)
        {
            tornado.GetComponent<NetworkObject>().Despawn(false);
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