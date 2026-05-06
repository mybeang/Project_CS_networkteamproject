using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public abstract class EventTask : NetworkBehaviour
{
    [Header("단순 식별용 이름")]
    [SerializeField] protected string eventName;
    
    [Header("이벤트를 호출 시간")]
    [SerializeField] public double[] excuteTime;
    
    [Header("이벤트 시작 몇초 후 종료 시킬 것인지")]
    [SerializeField, Tooltip("0초는 Stop 호출 안함"), Range(0,float.MaxValue)] public double[] stopTriggerTime;

    public abstract void OnEventSpawn();

    public abstract void OnEventDespawn();
}
