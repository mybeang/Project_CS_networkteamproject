using System;
using System.Collections.Generic;
using Firebase.Extensions;
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
    private bool _ready;

    private void Awake() => Init();

    private void OnEnable() => BindCallbackButtons();
    private void OnDisable() => UnbindCallbackButtons();

    private void Start()
    {
        // ToDo. 팀 및 롤 선택 가이드 팝업 하기.
    }
    
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
        Debug.Log("[LobbyRoomUIController] On ReSelect ... ");
        var lobby = ServiceLocator.Get<ILobbyManager>();
        if (lobby.GetMyPlayerData()[LobbyPlayerDataKey.READY] == "true")
        {
            Debug.Log("[LobbyRoomUIController] On ReSelect ... Canceled");
            // ToDo. 먼저 레디 풀라고 팝업 띄우기.
            return;
        }
        
        List<(string key, string value)> updateData = new();
        updateData.Add((LobbyPlayerDataKey.TEAM, "0"));
        updateData.Add((LobbyPlayerDataKey.ROLE, $"{PlayerRole.None}"));
        lobby?.UpdatePlayerData(updateData).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.Log("[LobbyRoomUIController] On ReSelect ... Fail");
                return;
            }
            var userInfo = ServiceLocator.Get<IUserInfoManager>();
            userInfo?.SetIsDriver(PlayerRole.None);
            userInfo?.SetTeamNum(0);
            Debug.Log($"[LobbyRoomUIController] On ReSelect ... Done");
        });
    }

    private void OnReady()
    {
        Debug.Log("[LobbyRoomUIController] Ready ... ");
        var lobbyManager = ServiceLocator.Get<ILobbyManager>();
        var player = lobbyManager.GetMyPlayerData();
        if (player[LobbyPlayerDataKey.ROLE] == $"{PlayerRole.None}" ||
            player[LobbyPlayerDataKey.TEAM] == "0")
        {
            Debug.Log("[LobbyRoomUIController] Ready ... Fail");
            // ToDo. 팀 및 롤 선택 가이드 팝업 하기.
            return;
        }
        
        _ready = !_ready;
        var lobby = ServiceLocator.Get<ILobbyManager>();
        List<(string key, string value)> updateData = new();
        updateData.Add((LobbyPlayerDataKey.READY, _ready ? "true" : "false" ));
        lobby?.UpdatePlayerData(updateData);
        Debug.Log("[LobbyRoomUIController] Ready ... Done");
    }

    private void OnStartGame()
    {
        if (CheckAllReady())
        {
            Debug.Log("Start Game !!");
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
        var lobbyManager = ServiceLocator.Get<ILobbyManager>();
        int[] playersPerTeam = new int[4];
        foreach (var player in lobbyManager.GetPlayerList())
        {
            if (player.Data[LobbyPlayerDataKey.TEAM].Value == "0" || // 모든 유저가 팀에 속해야함.
                player.Data[LobbyPlayerDataKey.ROLE].Value == $"{PlayerRole.None}" ||  // 모든 유저가 Role 이 부여 되어있어야 함.
                player.Data[LobbyPlayerDataKey.READY].Value == "false")  // 모든 유저가 Ready 를 해야함.
                return false;
            int teamNum = int.Parse(player.Data[LobbyPlayerDataKey.TEAM].Value) - 1;
            playersPerTeam[teamNum]++;
        }
        foreach (var i in playersPerTeam)
        {   // 모든 팀이 0명 혹은 2명이 배속 되어있어야함.
            if (i % 2 == 0) return false;
        }
        return true;
    }
}
