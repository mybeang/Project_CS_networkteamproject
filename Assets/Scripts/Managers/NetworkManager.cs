using Unity.Netcode;
using UnityEngine;

public abstract class NetworkManager<T> : NetworkBehaviour
{
    private void Awake() => Init();
    protected virtual void Init()
    {
    }
    
    private void OnEnable() => Register();
    private void OnDisable() => Unregister();

    protected abstract void Register();
    protected abstract void Unregister();
}
