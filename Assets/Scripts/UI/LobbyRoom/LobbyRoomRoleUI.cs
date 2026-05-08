using System;
using System.Collections.Generic;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;

public class LobbyRoomRoleUI : MonoBehaviour
{
    [SerializeField] private Button _moveButton;
    [SerializeField] private TMP_InputField _userId;
    [SerializeField] private Toggle _ready;
    
    [SerializeField] private int _teamNumber;
    [SerializeField] private PlayerRole _playerRole;
    [SerializeField] private AudioClip _btSound;

    private void OnEnable()
    {
        _moveButton.onClick.AddListener(OnMove);
        ServiceLocator.Get<ILobbyManager>()?.LobbyDataOnChangedAddListener(OnRender);
    }

    private void OnDisable()
    {
        _moveButton.onClick.RemoveListener(OnMove);
        ServiceLocator.Get<ILobbyManager>()?.LobbyDataOnChangedRemoveListener(OnRender);
    }

    private void OnMove()
    {
        ServiceLocator.Get<IAudioService>().PlayOneShotSfx(_btSound);
        Debug.Log("[LobbyRoomRoleUI] On Move");
        AssignToRole();
    }

    public bool AssignToRole()
    {
        Debug.Log("[LobbyRoomRoleUI] Assign to role");
        if (_ready.isOn) return false;
        if (_userId.text != "") return false;
        Debug.Log($"[LobbyRoomTeamUI] Update Data ... r:{_playerRole} | t:{_teamNumber}");        
        var lobby = ServiceLocator.Get<ILobbyManager>();
        List<(string key, string value)> updateData = new ();
        updateData.Add((LobbyPlayerDataKey.TEAM, $"{_teamNumber}"));
        updateData.Add((LobbyPlayerDataKey.ROLE, $"{_playerRole}"));
        Debug.Log("[LobbyRoomTeamUI] Update Data ... Lobby Data Async Start");
        lobby?.UpdatePlayerData(updateData).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted) {
                Debug.Log("[LobbyRoomTeamUI] Update Data faulted");
                return;
            }
            var userInfo = ServiceLocator.Get<IUserInfoManager>();
            userInfo?.SetIsDriver(_playerRole);
            userInfo?.SetTeamNum(_teamNumber);
        });
        Debug.Log("[LobbyRoomTeamUI] Update Data ... User Info Clear");
        return true;
    }
    
    private void OnRender(Lobby lobby)
    {
        List<Player> players = lobby.Players;
        foreach (var player in players)
        {
            var teamId = player.Data[LobbyPlayerDataKey.TEAM].Value;
            var role = player.Data[LobbyPlayerDataKey.ROLE].Value;
            if ($"{_teamNumber}" == teamId && $"{_playerRole}" == role)
            {
                _userId.text = player.Data[LobbyPlayerDataKey.USER_ID].Value;
                _ready.isOn = player.Data[LobbyPlayerDataKey.READY].Value == "true";
                return;
            }
        }
        _userId.text = "";
    }
}

