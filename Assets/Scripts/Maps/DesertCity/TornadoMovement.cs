using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(NetworkObject))]
public class TornadoMovement : NetworkBehaviour
{
    [SerializeField] private float _speed;
    [SerializeField] private List<WayPoint> _wayPoints;
    private List<Vector3> _selectedWayPoint;
    private Vector3 _nxWayPoint;
    private int index;
    private bool _moveable;

    private void OnDisable() => _moveable = false;
    
    private void Update()
    {
        ChangeNxWayPoint();
        if (_moveable) Move();
    }
    
    [ClientRpc]
    public void InitClientRpc(string wayPointsJson, int startIndex, float speed)
    {
        var wayPoint = JsonUtility.FromJson<WayPoint>(wayPointsJson);
        Debug.Log($"[TornadoMovement] way points = ${wayPoint}");
        _selectedWayPoint = wayPoint.positions;
        if (UnityEngine.Random.value > 0.5f) _selectedWayPoint.Reverse();
        index = startIndex;
        Debug.Log($"[TornadoMovement] start index = ${index}");
        transform.position = _selectedWayPoint[index++];
        if (index > _selectedWayPoint.Count - 1) index = 0;
        _nxWayPoint = _selectedWayPoint[index];
        _speed = speed;
        _moveable = true;
    }
    
    private void ChangeNxWayPoint()
    {
        if (Vector3.Distance(_nxWayPoint, transform.position) <= 0.1f)
        {
            if (index == _selectedWayPoint.Count - 1) index = -1;
            _nxWayPoint = _selectedWayPoint[++index];
        }
    }

    private void Move()
    {
        if (_nxWayPoint == Vector3.zero) return; 
        transform.position = Vector3.MoveTowards(transform.position, _nxWayPoint, _speed * Time.deltaTime);
    }
}
