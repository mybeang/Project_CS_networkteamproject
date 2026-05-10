using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyListItemUI : MonoBehaviour
{
    [SerializeField] private Toggle _toggle;
    [SerializeField] private TextMeshProUGUI _creator;
    [SerializeField] private TextMeshProUGUI _subject;
    [SerializeField] private TextMeshProUGUI _currentUser;
    [SerializeField] private Button _clickButton;

    private string _roomId;
    public string RoomId => _roomId;
    public bool IsSelected => _toggle.isOn;

    private void OnEnable() => _clickButton.onClick.AddListener(OnClicked);
    private void OnDisable() => _clickButton.onClick.RemoveListener(OnClicked);
    private void OnClicked()
    {
        Debug.Log($"[{gameObject.name}] Clicked !!");
        _toggle.isOn = !_toggle.isOn;
    }

    public void SetData(Lobby lobby)
    {
        _roomId = lobby.Id;
        _creator.text = FindHostUserId(lobby.Players, lobby.HostId);
        _subject.text = lobby.Name;
        _currentUser.text = $"({lobby.Players.Count} / {lobby.MaxPlayers})"; 
    }

    private string FindHostUserId(List<Player> players, string hostId)
    {
        foreach (var player in players)
        {
            if (hostId == player.Id) return player.Data[LobbyPlayerDataKey.USER_ID].Value;
        }
        return "";
    }
}