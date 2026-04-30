using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyRoomTeamUI : MonoBehaviour
{
    [SerializeField] private Button _moveButton;
    [SerializeField] private LobbyRoomRoleUI _gunnerUI;
    [SerializeField] private LobbyRoomRoleUI _driverUI;
    
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
        if (!_gunnerUI.AssignToRole())
            _driverUI.AssignToRole();
    }
    
}

