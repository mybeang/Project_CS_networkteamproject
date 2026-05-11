using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SettingPanelController : MonoBehaviour
{
    private InputSystem_Actions _input; // 인풋시스템

    [SerializeField] private GameObject _panel; // UI판넬 갖고오기
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _returnButton;
    [SerializeField] private Button _exitButton;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Slider _voiceSlider;
    
    

    private void Awake()
    {
        _input = new InputSystem_Actions(); // 인풋시스템 초기화
        _input.UI.ToggleSetting.performed += OnSettingsPopup; // 인풋시스템 기능 구독
    }

    private void OnEnable()
    {
        _input.Enable();
        // 버튼 이벤트 구독
        _continueButton.onClick.AddListener(OnContinueButton); // 계속하기 버튼
        _returnButton.onClick.AddListener(OnReturnToLobbyButton); // 로비로 나가기 버튼
        _exitButton.onClick.AddListener(OnContinueButton); // X버튼 (계속하기와 동일한 동작)

        // 슬라이더 이벤트 구독
        _bgmSlider.onValueChanged.AddListener(OnChangeBGMVolume); // BGM 볼륨
        _sfxSlider.onValueChanged.AddListener(OnChangeSFXVolume); // SFX 볼륨
        _voiceSlider.onValueChanged.AddListener(OnChangeVoiceVol); // Voice 볼륨
    }

    private void OnDisable()
    {
        _input.Disable();
        // 버튼 이벤트 해제
        _continueButton.onClick.RemoveListener(OnContinueButton);
        _returnButton.onClick.RemoveListener(OnReturnToLobbyButton);
        _exitButton.onClick.RemoveListener(OnContinueButton);

        // 슬라이더 이벤트 해제
        _bgmSlider.onValueChanged.RemoveListener(OnChangeBGMVolume);
        _sfxSlider.onValueChanged.RemoveListener(OnChangeSFXVolume);
        _voiceSlider.onValueChanged.RemoveListener(OnChangeVoiceVol);
    }

    // Esc키 눌렀을때 팝업창 열고 닫기
    private void OnSettingsPopup(InputAction.CallbackContext ctx)
    {
        _panel.SetActive(!_panel.activeSelf);
    }


    // bgm 소리 조절하는 함수
    public void OnChangeBGMVolume(float vol)
    {
        ServiceLocator.Get<IAudioService>()?.SetBGMVolume(vol);
    }

    // Sfx 소리 조절하는 함수
    public void OnChangeSFXVolume(float vol)
    {
        ServiceLocator.Get<IAudioService>()?.SetSfxVolume(vol);
    }

    // voice 소리 조절하는 함수
    public void OnChangeVoiceVol(float vol)
    {
        // 유저의 모든정보 갖고오기
        var userInfo = ServiceLocator.Get<IUserInfoManager>()?.GetUserInfo();

        if (userInfo == null) return; // 유저정보가 없다면 리턴

        // 채널명은 유저id와 유저의 팀숫자가 합쳐진 것(VoiceManager 출처)
        string channelName = $"{userInfo.roomId}_{userInfo.teamNum}";

        ServiceLocator.Get<IVoiceManager>()?.SetVolume(channelName,(int)vol);
    }

    // 계속하기 버튼 (누르면 설정창 닫힘)
    public void OnContinueButton()
    {
        _panel.SetActive(false);
    }

    // 로비 돌아가기 버튼 누르면 로비로 돌아가는 기능 함수
    public void OnReturnToLobbyButton()
    {
        // 로비로 돌아갈 시 연결 먼저 끊어준다.
        ServiceLocator.Get<ILobbyManager>()?.LeaveRoom();
        ServiceLocator.Get<IRelayHostManager>()?.Disconnect();
        ServiceLocator.Get<ILocalSceneLoader>()?.LoadScene("LobbyList");
    }
}
