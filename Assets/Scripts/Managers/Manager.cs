using System;
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

    private void OnEnable() => Register();
    private void OnDisable() => Unregister();

    protected abstract void Register();
    protected abstract void Unregister();
}
