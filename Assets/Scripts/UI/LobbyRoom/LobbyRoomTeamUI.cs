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
        // 먼저 거너쪽을 보고, 거너가 이미 차 있으면 드라이버에게 넣고.
        // 둘다 차 있으면 아무짓 안함.
        Debug.Log("[LobbyRoomTeamUI] On Move");
        if (!_gunnerUI.AssignToRole())
            _driverUI.AssignToRole();
    }
    
}

