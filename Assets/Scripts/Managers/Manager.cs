using UnityEngine;

public abstract class Manager<T> : MonoBehaviour
{
    [SerializeField] protected bool isDonDestoryOnLoad; 
    
    private void Awake()
    {
        Manager<T>[] managers = FindObjectsByType<Manager<T>>(FindObjectsSortMode.None);
        if (isDonDestoryOnLoad && managers.Length == 0) 
            DontDestroyOnLoad(gameObject);

        Init();
    }

    protected virtual void Init()
    {
    }

    protected abstract void Register();
    protected abstract void Unregister();
}
