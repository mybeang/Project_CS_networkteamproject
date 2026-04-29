using System.Collections.Generic;
using Firebase.Extensions;
using Unity.Android.Gradle.Manifest;
using UnityEngine;
using UnityEngine.UI;
using Application = UnityEngine.Application;

public class LobbyListUIController : MonoBehaviour
{
    [Header("Lobby List Body")]
    [SerializeField] private Button _refeshButton;
    [SerializeField] private List<LobbyListItemUI> _lobbyListItems;
    
    [Header("Lobby List Menu")]
    [SerializeField] private Button _leftButton;
    [SerializeField] private Button _rightButton;
    [SerializeField] private Button _joinLobbyButton;
    [SerializeField] private Button _createLobbyButton;
    [SerializeField] private Button _quitGameButton;
    
    [Header("Create Room Menu")]
    [SerializeField] private CreateRoomUIController _createRoomUI;
    
    private int offset = 0;

    private void Start()
    {
        DiableAllLobbyListItems();
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
        _quitGameButton.onClick.AddListener(OnQuitGame);
    }

    private void UnbindingHandlersToButtons()
    {
        _refeshButton.onClick.RemoveListener(OnRefreshLobbyListItems);
        _leftButton.onClick.RemoveListener(OnGoLeftList);
        _rightButton.onClick.RemoveListener(OnGoRigtList);
        _createLobbyButton.onClick.RemoveListener(OnCreateRoom);
        _joinLobbyButton.onClick.RemoveListener(OnJoinRoom);
        _quitGameButton.onClick.RemoveListener(OnQuitGame);
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
        offset = 0;
        GetLobbyListItems();
    }

    private void OnGoRigtList()
    {
        offset += 4;
        GetLobbyListItems();
    }
    
    private void OnGoLeftList()
    {
        if (offset == 0) return;
        offset -= 4;
        GetLobbyListItems();
    }

    private void OnCreateRoom() => _createRoomUI.gameObject.SetActive(true);

    private void OnJoinRoom()
    {
        foreach (LobbyListItemUI item in _lobbyListItems)
        {
            if (item.IsSelected)
            {
                ServiceLocator.Get<ILobbyManager>().JoinRoom(item.RoomId);
                return;
            }
        }
    }
    
    private void OnQuitGame() => Application.Quit();
}
