using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyRoomRoleUI : MonoBehaviour
{
    [SerializeField] private Button _moveButton;
    [SerializeField] private TMP_InputField _userId;
    [SerializeField] private Toggle _ready;
    
    [SerializeField] private int _teamNumber;
    [SerializeField] private PlayerRole _playerRole;
    
    private void OnEnable() => BindCallbackToButtons();
    private void OnDisable() => UnbindCallbackToButtons();

    private void BindCallbackToButtons()
    {
        _moveButton.onClick.AddListener(OnMove);
    }

    private void UnbindCallbackToButtons()
    {
        _moveButton.onClick.RemoveListener(OnMove);
    }

    private void OnMove()
    {
        AssignToRole();
    }

    public bool AssignToRole()
    {
        if (_userId.text != "") return false;
        // UserInfo Update
        UpdateData(_playerRole, _teamNumber);
        return true;
    }
    
    public void UnassignFromRole()
    {
        _userId.text = "";
        UpdateData(PlayerRole.None, 0);
    }

    private void UpdateData(PlayerRole playerRole, int teamNumber)
    {
        var userInfo = ServiceLocator.Get<IUserInfoManager>();
        userInfo?.SetIsDriver(playerRole);
        userInfo?.SetTeamNum(teamNumber);
        
        // Lobby Update
        var lobby = ServiceLocator.Get<ILobbyManager>();
        List<(string key, string value)> updateData = new ();
        updateData.Add((LobbyPlayerDataKey.TEAM, $"{teamNumber}"));
        updateData.Add((LobbyPlayerDataKey.ROLE, nameof(playerRole)));
        lobby?.UpdatePlayerData(updateData);
    }
}

