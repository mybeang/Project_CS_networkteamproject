using Unity.Netcode;
using UnityEngine;

public abstract class NetworkManager<T> : NetworkBehaviour
{
    [SerializeField] protected bool isDonDestoryOnLoad; 
    
    private void Awake()
    {
        NetworkManager<T>[] managers = FindObjectsByType<NetworkManager<T>>(FindObjectsSortMode.None);
        if (isDonDestoryOnLoad && managers.Length == 0) 
            DontDestroyOnLoad(gameObject);

        Init();
    }

    protected virtual void Init()
    {
    }
    
    private void OnEnable() => Register();
    private void OnDisable() => Unregister();

    protected abstract void Register();
    protected abstract void Unregister();
}
