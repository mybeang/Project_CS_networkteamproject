using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateRoomUIController : MonoBehaviour
{
    [SerializeField] private GameObject _createRoomPanel;
    [SerializeField] private TMP_InputField _roomSubjectInputField;
    [SerializeField] private Button _createRoomButton;
    [SerializeField] private Button _closePanelButton;

    private void OnEnable()
    {
        _createRoomButton.onClick.AddListener(OnCreateRoom);
        _closePanelButton.onClick.AddListener(OnClosePanel);
    }

    private void OnDisable()
    {
        _createRoomButton.onClick.RemoveListener(OnCreateRoom);
        _closePanelButton.onClick.RemoveListener(OnClosePanel);
    }
    
    private void OnCreateRoom()
    {
        ServiceLocator.Get<ILobbyManager>().CreateRoom(_roomSubjectInputField.text);
        OnClosePanel();
        ServiceLocator.Get<ILocalSceneLoader>().LoadScene("LobbyRoom");
    }
    
    private void OnClosePanel() => _createRoomPanel.SetActive(false);
}