using NUnit;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class EventScheduleManager : NetworkBehaviour
{
    [SerializeField] private EventTask _events;

    public override void OnNetworkSpawn()
    {

    }

    private void OnEnable()
    {
        if (!IsServer) return;
        InergrityCheck();

        //gameManager.AddTimes;
    }

    private void InergrityCheck()
    {
        if (_events == null)
        {
            Debug.LogError($"[{name}] EventTask가 등록되어 있지 않습니다.");
            return;
        }

        if (_events.excuteTime == null)
        {
            Debug.LogError($"[{name}] 등록된 EventTask의 excuteTime이 null 입니다.");
            return;
        }

    }

    [ServerRpc]
    public void OnEventSpawnServerRpc() => _events.OnEventSpawn();

    [ServerRpc]
    public void OnEventDespawnServerRpc() => _events.OnEventDespawn();
}
