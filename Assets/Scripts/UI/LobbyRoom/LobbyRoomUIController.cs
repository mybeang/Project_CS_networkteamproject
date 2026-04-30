using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyRoomUIController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _lobbySubject;
    [SerializeField] private Image _selectedMapImage;
    
    [Header("Buttons")]
    [SerializeField] private Button _leftMapButton;
    [SerializeField] private Button _rightMapButton;
    [SerializeField] private Button _startGameButton;
    [SerializeField] private Button _readyButton;
    [SerializeField] private Button _reSelectButton;
    [SerializeField] private Button _goToLobbyListButton;
    
    [Header("Data")]
    [SerializeField] private List<Sprite> _mapImages = new();
    private int _selectedMapNumber;

    private void Awake() => Init();

    private void OnEnable() => BindCallbackButtons();
    private void OnDisable() => UnbindCallbackButtons();

    private void Init()
    {
        _lobbySubject.text = ServiceLocator.Get<ILobbyManager>()?.GetRoomName();
        if (_mapImages.Count != 0) _selectedMapImage.sprite = _mapImages[0];
        ChangeButtonVisibility();
    }

    private void BindCallbackButtons()
    {
        _leftMapButton.onClick.AddListener(OnLeftMap);
        _rightMapButton.onClick.AddListener(OnRightMap);
        _startGameButton.onClick.AddListener(OnStartGame);
        _readyButton.onClick.AddListener(OnReady);
        _reSelectButton.onClick.AddListener(OnReSelect);
        _goToLobbyListButton.onClick.AddListener(OnGoToLobby);
    }

    private void UnbindCallbackButtons()
    {
        _leftMapButton.onClick.RemoveListener(OnLeftMap);
        _rightMapButton.onClick.RemoveListener(OnRightMap);
        _startGameButton.onClick.RemoveListener(OnStartGame);
        _readyButton.onClick.RemoveListener(OnReady);
        _reSelectButton.onClick.RemoveListener(OnReSelect);
        _goToLobbyListButton.onClick.RemoveListener(OnGoToLobby);
    }

    private void ChangeButtonVisibility()
    {
        string userId = ServiceLocator.Get<IUserInfoManager>()?.GetUserInfo().userId;
        string hostId = ServiceLocator.Get<ILobbyManager>()?.GetHostId();
        if (userId == hostId)
        {
            _startGameButton.gameObject.SetActive(true);
            _readyButton.gameObject.SetActive(false);
        }
        else
        {
            _startGameButton.gameObject.SetActive(false);
            _readyButton.gameObject.SetActive(true);
        }
    }

    private void OnGoToLobby()
    {
        OnReSelect();
        ServiceLocator.Get<ILobbyManager>()?.LeaveRoom();
        ServiceLocator.Get<ILocalSceneLoader>()?.LoadScene("LobbyScene");
    }

    private void OnReSelect()
    {
        
    }

    private void OnReady()
    {
        
    }

    private void OnStartGame()
    {
        if (CheckAllReady())
        {
            
        }
    }

    private void OnRightMap()
    {
        if (_mapImages.Count != 0)
        {
            _selectedMapNumber++;
            if (_selectedMapNumber > _mapImages.Count) _selectedMapNumber = 0;
            
            _selectedMapImage.sprite = _mapImages[_selectedMapNumber];
        }
    }

    private void OnLeftMap()
    {
        if (_mapImages.Count != 0)
        {
            _selectedMapNumber--;
            if (_selectedMapNumber < 0) _selectedMapNumber = _mapImages.Count - 1;
            
            _selectedMapImage.sprite = _mapImages[_selectedMapNumber];
        }
    }
    
    private bool CheckAllReady()
    {
        // 모든 유저가 팀에 속해야함.
        // 모든 유저가 Role 이 부여 되어있어야 함.
        // 모든 유저가 Ready 를 해야함.
        return false;
    }
}
