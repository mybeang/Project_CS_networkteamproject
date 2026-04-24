using UnityEngine;

public abstract class Manager : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    protected abstract void Register();
    protected abstract void Unregister();
}
