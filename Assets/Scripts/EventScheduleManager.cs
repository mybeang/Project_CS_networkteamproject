using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class EventScheduleManager : NetworkBehaviour
{
    [SerializeField] private EventTask _events;

    public override void OnNetworkSpawn()
    {
        
    }

    private void Start()
    {
        StartCoroutine(waiter());
    }

    IEnumerator waiter()
    {
        while(true)
        {
            yield return new WaitForSeconds(3f);
            InergrityCheck();
            ServiceLocator.Get<IGameManager>().AddEventSchedule(this);
        }
    }

    public double[] GetTimer() => _events.excuteTime;
    public double[] GetStopTimer()
    {
        return _events.stopTriggerTime != null ? _events.stopTriggerTime : null;
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
