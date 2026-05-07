using UnityEngine;

public class BootstrapDDOL : MonoBehaviour
{
    private void Awake()
    {
        transform.SetParent(null);
        var candidates = GameObject.FindGameObjectsWithTag("ddolObject");
        if (candidates.Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }
}
