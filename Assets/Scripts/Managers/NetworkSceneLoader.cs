using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkSceneLoader : NetworkManager<NetworkSceneLoader>, INetworkSceneLoader
{
    protected override void Register() => ServiceLocator.Register<INetworkSceneLoader>(this);
    protected override void Unregister() => ServiceLocator.Unregister<INetworkSceneLoader>();

    public void LoadScene(string sceneName)
    {
        if (!IsServer) return;
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);   
    }
}
