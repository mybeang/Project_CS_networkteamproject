using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyRoomRoleUI : MonoBehaviour
{
    [SerializeField] private Button _moveButton;
    [SerializeField] private TMP_InputField _userId;
    [SerializeField] private Toggle _ready;
    
    [SerializeField] private int _teamNumber;
    [SerializeField] private bool _isDriver;
    
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
        _userId.text = ServiceLocator.Get<IUserInfoManager>()?.GetUserInfo().userId;
        // UserInfo Update
        ServiceLocator.Get<IUserInfoManager>().SetIsDriver(_isDriver);
        ServiceLocator.Get<IUserInfoManager>().SetTeamNum(_teamNumber);
        // Lobby Update
        return true;
    }
    
    public void UnassignFromRole()
    {
        _userId.text = "";
        // UserInfo Update
        // Lobby Update
    }
}

