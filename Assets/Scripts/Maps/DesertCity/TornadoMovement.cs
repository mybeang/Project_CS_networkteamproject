using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(NetworkObject))]
public class TornadoMovement : NetworkBehaviour
{
    [SerializeField][Range(0.5f, 2f)] private float _minSpeed;
    [SerializeField][Range(5f, 20f)] private float _maxSpeed;
    [SerializeField] private float _speed;
    [SerializeField] private List<WayPoint> _wayPoints;
    private List<Vector3> _selectedWayPoint;
    private Vector3 _nxWayPoint;
    private int index;
    private bool _moveable;

    private void OnEnable() => _moveable = true;
    private void OnDisable() => _moveable = false;
    
    private void Update()
    {
        ChangeNxWayPoint();
        if (_moveable) Move();
    }
    
    public void Init(List<Vector3> wayPoints)
    {
        SetSpeed();
        _selectedWayPoint = wayPoints;
        if (UnityEngine.Random.value > 0.5f) _selectedWayPoint.Reverse();
        index = UnityEngine.Random.Range(0, _selectedWayPoint.Count - 1);
        transform.position = _selectedWayPoint[index];
        _nxWayPoint = _selectedWayPoint[++index];
    }

    private void SetSpeed()
    {
        _speed = UnityEngine.Random.Range(_minSpeed, _maxSpeed);
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
