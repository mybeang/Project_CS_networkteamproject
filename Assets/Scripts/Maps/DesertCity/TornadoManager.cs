using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
struct WayPoint
{
    public List<Vector3> wayPoint;
}

public class TornadoManager : MonoBehaviour
{
    [SerializeField] private GameObject _tornadoPrefab;
    [SerializeField] private List<WayPoint> _wayPoints;
    [SerializeField][Range(3, 20)] private int _numberOfTornados;
    private Queue<GameObject> _pool = new();
    private List<GameObject> _activeTorndos = new();
    private Vector3 _nxWayPoint;
    private int index;

    private void Awake() => Init();
    
    private void Init()
    {
        for (int i = 0; i < _numberOfTornados; i++)
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
        return _wayPoints[index].wayPoint;
    }
    
    private void SpawnTornado()
    {
        GameObject tornado = _pool.Dequeue();
        TornadoMovement tm = tornado.GetComponent<TornadoMovement>();
        tm.Init(SelectWayPoint());
        tornado.SetActive(true);
        _activeTorndos.Add(tornado);
    }

    private void DespawnAllTornado()
    {
        foreach (GameObject tornado in _activeTorndos)
        {
            tornado.SetActive(false);
            _pool.Enqueue(tornado);
        }
        _activeTorndos.Clear();
    }
    
    [ContextMenu("SpawnTornado")]
    public void TestSpawnTornado() => SpawnTornado();
    
    [ContextMenu("DespawnTornado")]
    public void TestDespawnTornado() => DespawnAllTornado();
}