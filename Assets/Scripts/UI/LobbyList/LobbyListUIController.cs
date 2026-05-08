using System.Collections.Generic;
using Firebase.Extensions;
using TMPro;
using Unity.Android.Gradle.Manifest;
using UnityEngine;
using UnityEngine.UI;
using Application = UnityEngine.Application;

public class LobbyListUIController : MonoBehaviour
{
    [Header("Lobby List Body")]
    [SerializeField] private TextMeshProUGUI _myIdText;
    [SerializeField] private Button _refeshButton;
    [SerializeField] private List<LobbyListItemUI> _lobbyListItems;
    
    [Header("Lobby List Menu")]
    [SerializeField] private Button _leftButton;
    [SerializeField] private Button _rightButton;
    [SerializeField] private Button _joinLobbyButton;
    [SerializeField] private Button _createLobbyButton;
    [SerializeField] private Button _quitGameButton;
    
    [Header("Create Room Menu")]
    [SerializeField] private GameObject _createRoomUI;
    
    private int offset = 0;

    private void Start()
    {
        DiableAllLobbyListItems();
        GetLobbyListItems();
        _myIdText.text = ServiceLocator.Get<IUserInfoManager>().GetUserInfo().userId;
    }
    
    private void OnEnable() => BindingHandlersToButtons();
    private void OnDisable() => UnbindingHandlersToButtons();

    private void BindingHandlersToButtons()
    {
        _refeshButton.onClick.AddListener(OnRefreshLobbyListItems);
        _leftButton.onClick.AddListener(OnGoLeftList);
        _rightButton.onClick.AddListener(OnGoRigtList);
        _createLobbyButton.onClick.AddListener(OnCreateRoom);
        _joinLobbyButton.onClick.AddListener(OnJoinRoom);
        _quitGameButton.onClick.AddListener(GoToLoginPage);
    }

    private void UnbindingHandlersToButtons()
    {
        _refeshButton.onClick.RemoveListener(OnRefreshLobbyListItems);
        _leftButton.onClick.RemoveListener(OnGoLeftList);
        _rightButton.onClick.RemoveListener(OnGoRigtList);
        _createLobbyButton.onClick.RemoveListener(OnCreateRoom);
        _joinLobbyButton.onClick.RemoveListener(OnJoinRoom);
        _quitGameButton.onClick.RemoveListener(GoToLoginPage);
    }
    

    private void DiableAllLobbyListItems()
    {
        foreach (LobbyListItemUI lobbyListItem in _lobbyListItems)
        {
            lobbyListItem.gameObject.SetActive(false);
        }
    }
    
    private void GetLobbyListItems()
    {
        DiableAllLobbyListItems();
        ServiceLocator.Get<ILobbyManager>()?.GetRoomList(offset).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                if (task.Result.Count <= 0)
                {
                    if (offset >= 4) offset -= 4;
                    return;
                }
        
                for (int i = 0; i < task.Result.Count; i++)
                {
                    _lobbyListItems[i].SetData(task.Result[i]);
                    _lobbyListItems[i].gameObject.SetActive(true);
                }        
            }
        });
    }
    
    private void OnRefreshLobbyListItems()
    {
        ServiceLocator.Get<IAudioService>().PlayButtonSfx();
        offset = 0;
        GetLobbyListItems();
    }

    private void OnGoRigtList()
    {
        ServiceLocator.Get<IAudioService>().PlayButtonSfx();
        offset += 4;
        GetLobbyListItems();
    }
    
    private void OnGoLeftList()
    {
        ServiceLocator.Get<IAudioService>().PlayButtonSfx();
        if (offset == 0) return;
        offset -= 4;
        GetLobbyListItems();
    }

    private void OnCreateRoom()
    {
        ServiceLocator.Get<IAudioService>().PlayButtonSfx();
        _createRoomUI.SetActive(true);
    }

    private void OnJoinRoom()
    {
        ServiceLocator.Get<IAudioService>().PlayButtonSfx();
        foreach (LobbyListItemUI item in _lobbyListItems)
        {
            if (item.IsSelected)
            {
                ServiceLocator.Get<ILobbyManager>().JoinRoom(item.RoomId).ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted)
                    {
                        ServiceLocator.Get<ILocalSceneLoader>().LoadScene("LobbyRoom");
                    }
                });
                return;
            }
        }
    }

    private void GoToLoginPage()
    {
        ServiceLocator.Get<IAudioService>().PlayButtonSfx();
        ServiceLocator.Get<ILocalSceneLoader>().LoadScene("Login");
    }
}
