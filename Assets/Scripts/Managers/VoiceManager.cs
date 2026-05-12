using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using Action = System.Action;

public class VoiceManager : Manager<VoiceManager>, IVoiceManager
{
    private event Action OnLoginEndEvent;
    private int _volume;
    private IVoiceManager _voiceManagerImplementation;

    protected override async void Init()
    {
        await UnityServiceInitialize.Processing();
        if (VivoxService.Instance != null && 
            VivoxService.Instance.InitializationState == VivoxInitializationState.Uninitialized)
            await VivoxService.Instance.InitializeAsync();
        await LoginAsync();
        OnLoginEndEvent?.Invoke();
    }

    public void LoginEventAddListener(Action callback) => OnLoginEndEvent += callback;
    public void LoginEventRemoveListener(Action callback) => OnLoginEndEvent -= callback;
    
    private async Task LoginAsync()
    {
        LoginOptions options = new LoginOptions();
        options.DisplayName = Guid.NewGuid().ToString();
        if (VivoxService.Instance != null && !VivoxService.Instance.IsLoggedIn)
            await VivoxService.Instance.LoginAsync(options);
    }

    protected override void Register() => ServiceLocator.Register<IVoiceManager>(this);
    protected override void Unregister() => ServiceLocator.Unregister<IVoiceManager>();

    // channelName = roomId + teamNum
    public async void OnJoinVoiceChannel(string channelName)
    {
        await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);
        await VivoxService.Instance.SetChannelVolumeAsync(channelName, _volume);
    } 
    
    // channelName = roomId + teamNum
    public async void OnLeaveVoiceChannel(string channelName)
        => await VivoxService.Instance.LeaveChannelAsync(channelName);
    
    // channelName = roomId + teamNum, volume ; -50 ~ 50 (-50 is mute. default 0)
    public async void SetVolume(string channelName, int volume)
    {
        _volume = volume;
        if (channelName != null) await VivoxService.Instance.SetChannelVolumeAsync(channelName, _volume);
    }

    public int GetVolume() => _volume;
}
