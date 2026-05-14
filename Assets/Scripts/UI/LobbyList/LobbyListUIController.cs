using System.Collections;
using System.Collections.Generic;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyListUIController : MonoBehaviour
{
    [Header("Lobby List Body")]
    [SerializeField] private TextMeshProUGUI _myIdText;
    [SerializeField] private Button _refeshButton;
    [SerializeField] private List<LobbyListItemUI> _lobbyListItems;
    
    [Header("Lobby List Menu")]
    [SerializeField] private Button _leftButton;
    [SerializeField] private Button _rightButton;
    [SerializeField] private Button _quickJoinLobbyButton;
    [SerializeField] private Button _createLobbyButton;
    [SerializeField] private Button _quitGameButton;
    
    [Header("Create Room Menu")]
    [SerializeField] private GameObject _createRoomUI;
    
    [Header("Etc")]
    [SerializeField] private MessagePopUpUIController _msgPopUp;
    
    private int offset = 0;
    private Coroutine _refreshCoroutine;
    private bool _isCreateRoomUIOpened => _createRoomUI.activeSelf;

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
        _quickJoinLobbyButton.onClick.AddListener(OnQuickJoinRoom);
        _quitGameButton.onClick.AddListener(GoToLoginPage);
    }

    private void UnbindingHandlersToButtons()
    {
        _refeshButton.onClick.RemoveListener(OnRefreshLobbyListItems);
        _leftButton.onClick.RemoveListener(OnGoLeftList);
        _rightButton.onClick.RemoveListener(OnGoRigtList);
        _createLobbyButton.onClick.RemoveListener(OnCreateRoom);
        _quickJoinLobbyButton.onClick.RemoveListener(OnQuickJoinRoom);
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
        if (_isCreateRoomUIOpened) return;
        ServiceLocator.Get<IAudioService>().PlayButtonSfx();
        offset = 0;
        GetLobbyListItems();
    }

    private void OnGoRigtList()
    {
        if (_isCreateRoomUIOpened) return;
        ServiceLocator.Get<IAudioService>().PlayButtonSfx();
        offset += 4;
        GetLobbyListItems();
    }
    
    private void OnGoLeftList()
    {
        if (_isCreateRoomUIOpened) return;
        ServiceLocator.Get<IAudioService>().PlayButtonSfx();
        if (offset == 0) return;
        offset -= 4;
        GetLobbyListItems();
    }

    private void OnCreateRoom()
    {
        if (_isCreateRoomUIOpened) return;
        ServiceLocator.Get<IAudioService>().PlayButtonSfx();
        _createRoomUI.SetActive(true);
    }

    private async void OnQuickJoinRoom()
    {
        if (_isCreateRoomUIOpened) return;
        ServiceLocator.Get<IAudioService>().PlayButtonSfx();
        bool result = await ServiceLocator.Get<ILobbyManager>().QuickJoinRoom();
        if (!result)
        {
            _msgPopUp?.Open(
                MessageType.Warning,
                "참여 가능한 방이 없습니다.");
        }
        else
        {
            ServiceLocator.Get<ILocalSceneLoader>().LoadScene("LobbyRoom");
        }
    }

    private void GoToLoginPage()
    {
        if (_isCreateRoomUIOpened) return;
        ServiceLocator.Get<IAudioService>().PlayButtonSfx();
        ServiceLocator.Get<ILocalSceneLoader>().LoadScene("Login");
    }
}
