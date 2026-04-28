using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkTestController : MonoBehaviour
{
    [SerializeField] private Button startHostButton;
    [SerializeField] private Button startClientButton;
    [SerializeField] private Button goNextButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TextMeshProUGUI userName;
    [SerializeField] private TMP_InputField _joinCode;
    [SerializeField] private TMP_InputField _nextSceneName;

    private void OnEnable()
    {
        string userId = ServiceLocator.Get<IUserInfoManager>()?.GetUserInfo().userId;
        userName.text = $"Your ID: {userId}";
        
        startHostButton.onClick.AddListener(OnStartHost);
        startClientButton.onClick.AddListener(OnStartClient);
        goNextButton.onClick.AddListener(OnGoNextScene);
        quitButton.onClick.AddListener(OnQuit);
    }

    private void OnDisable()
    {
        startHostButton.onClick.RemoveListener(OnStartHost);
        startClientButton.onClick.RemoveListener(OnStartClient);
        goNextButton.onClick.RemoveListener(OnGoNextScene);
        quitButton.onClick.RemoveListener(OnQuit);
    }

    private async void OnStartHost()
    {
        string joinCode = await ServiceLocator.Get<IRelayHostManager>()?.StartHost();
        _joinCode.text = joinCode;
    }

    private void OnStartClient() => ServiceLocator.Get<IRelayHostManager>()?.StartClient(_joinCode.text);

    private void OnQuit()
    {
        ServiceLocator.Get<IDatabaseBackend>()?.RemoveUserAsync(userName.text);
        Application.Quit();
    }

    private void OnGoNextScene()
    {
        Debug.Log($"GoNextScene!! {_nextSceneName.text}");
        if (!_nextSceneName.text.Equals(""))
            ServiceLocator.Get<INetworkSceneLoader>()?.LoadScene(_nextSceneName.text);
    }
    
}
