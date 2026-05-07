using System;
using UnityEngine;
using UnityEngine.UI;

public class TempResultController : MonoBehaviour
{
    [SerializeField] private Button _OkButton;
    private bool IsHost => 
        ServiceLocator.Get<ILobbyManager>().GetHostId() == 
        ServiceLocator.Get<IUserInfoManager>().GetUserInfo().userId;

    private void OnEnable() => _OkButton.onClick.AddListener(GoToLobby);
    private void OnDisable() => _OkButton.onClick.RemoveListener(GoToLobby);

    private void LeaveToRelayHost()
    {
        if (IsHost) return;
        var relay = ServiceLocator.Get<IRelayHostManager>();
        relay.Disconnect();
    }
    
    private void GoToLobby()
    {
        var loader = ServiceLocator.Get<ILocalSceneLoader>();
        loader.LoadScene("LobbyRoom");
    }
}
