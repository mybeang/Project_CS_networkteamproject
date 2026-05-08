using Firebase.Extensions;
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
        // ToDo. 최소 5글자 이상의 문자열 입력 받도록 수정.
        // ToDo. 5글자 미만일 경우 PopupMessage 보일 수 있게 하기.
        if (_roomSubjectInputField.text.Length == 0) return;
        ServiceLocator.Get<IAudioService>().PlayButtonSfx();
        Debug.Log("[CreateRoomUIController] Processing Create the room.");
        var lobby = ServiceLocator.Get<ILobbyManager>();
        lobby.CreateRoom(_roomSubjectInputField.text)
            .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError(task.Exception);
                        return;
                    }
                    Debug.Log("[CreateRoomUIController] Go To Lobby Room.");
                    OnClosePanel();
                    ServiceLocator.Get<IDatabaseBackend>().RegisterRemoveRoomHandler(lobby.GetRoomID());
                    ServiceLocator.Get<ILocalSceneLoader>().LoadScene("LobbyRoom");
                });
    }
    
    private void OnClosePanel()
    {
        ServiceLocator.Get<IAudioService>().PlayButtonSfx();
        _createRoomPanel.SetActive(false);
    }
}