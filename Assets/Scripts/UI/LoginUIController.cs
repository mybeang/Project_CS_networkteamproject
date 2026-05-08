using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LoginUIController : MonoBehaviour
{
    [SerializeField] private TMP_InputField _userIdInputField;
    [SerializeField] private GameObject _warningPanel;
    [SerializeField] private TextMeshProUGUI _warningText;
    // 버튼들
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _exitGameButton;
    [SerializeField] private Button _closeWarningPanel;
    
    private bool _openedWarningPanel;
    private string _warningTextFormat = " 은(는) 이미 사용중인 닉네임 입니다.";
    private string _nextSceneName = "LobbyList";

    private void OnEnable()
    {
        Init();
        ServiceLocator.Get<IAudioService>().PlayMainBGM();
        SubscribButtons();
    }
    private void OnDisable() => UnsubscribButtons();

    private void Init()
    {
        var userInfo = ServiceLocator.Get<IUserInfoManager>();
        Debug.Log($"I am {userInfo.GetUserInfo().userId}");
        if (userInfo.GetUserInfo().userId != "")
            _userIdInputField.text = userInfo.GetUserInfo().userId;
    }
    
    private void SubscribButtons()
    {
        _loginButton.onClick.AddListener(OnLogin);
        _exitGameButton.onClick.AddListener(OnExitGame);
        _closeWarningPanel.onClick.AddListener(OnCloseWarningMessage);
    }

    private void UnsubscribButtons()
    {
        _loginButton.onClick.RemoveListener(OnLogin);
        _exitGameButton.onClick.RemoveListener(OnExitGame);
        _closeWarningPanel.onClick.RemoveListener(OnCloseWarningMessage);
    }

    public async void OnLogin()
    {
        if (_openedWarningPanel) return;
        ServiceLocator.Get<IAudioService>().PlayButtonSfx();
        Debug.Log("[Login] Try Login ...");
        var db = ServiceLocator.Get<IDatabaseBackend>();
        if (db == null) return;

        var userInfo = ServiceLocator.Get<IUserInfoManager>();
        
        if (_userIdInputField.text == "")
        {
            OpenWarningMessage("닉네임을 입력 해 주시기 바랍니다.");
            return;
        }
        if (userInfo?.GetUserInfo().userId != _userIdInputField.text)
        {
            bool isDup = await db.ValidateDuplicateUserIdAsync(_userIdInputField.text);
            if (isDup)
            {
                OpenWarningMessage(_userIdInputField.text + _warningTextFormat);
                return;
            }
            // -> 중복 아니면 로그인
            userInfo?.SetUserId(_userIdInputField.text);
            string userId = userInfo?.GetUserInfo().userId;
            Debug.Log($"[Login] UserId: {userId}");
            ServiceLocator.Get<IDatabaseBackend>()?.SaveUserAsync(userId);
            ServiceLocator.Get<IDatabaseBackend>()?.RegisterUserDisconnectHandler(userId);  // 비 정상 종료 방어 코드
        }
        ServiceLocator.Get<ILocalSceneLoader>()?.LoadScene(_nextSceneName);  // Test 용 씬으로 전환
    }
    
    private void OpenWarningMessage(string text)
    {
        _openedWarningPanel = true;
        _warningPanel.SetActive(true);
        _warningText.text = text;
    }

    private void OnCloseWarningMessage()
    {
        ServiceLocator.Get<IAudioService>()?.PlayButtonSfx();
        _openedWarningPanel = false;
        _warningPanel.SetActive(false);
    }

    private void OnExitGame()
    {
        if (_openedWarningPanel) return;
        ServiceLocator.Get<IAudioService>()?.PlayButtonSfx();
        CleanUp();
        Application.Quit();
    }

    private void CleanUp()
    {
        Debug.Log("[LoginUIController] CleanUp");
        string userId = ServiceLocator.Get<IUserInfoManager>()?.GetUserInfo().userId;
        if (userId != "") ServiceLocator.Get<IDatabaseBackend>()?.RemoveUserAsync(userId);
    }

# if UNITY_EDITOR
    [ContextMenu("ChangeNextScene/ToTestScene")]
    public void ChangeNextSceneIsTestScene()
    {
        _nextSceneName = "NetworkTestScene";
    }
    
    [ContextMenu("ChangeNextScene/ToLiveScene")]
    public void ChangeNextSceneIsLiveScene()
    {
        _nextSceneName = "LobbyList";
    }
# endif
}
