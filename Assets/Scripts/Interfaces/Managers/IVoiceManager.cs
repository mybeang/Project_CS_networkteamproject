
public interface IVoiceManager
{
    public void OnJoinVoiceChannel(string channelName);
    public void OnLeaveVoiceChannel(string channelName);
    public void SetVolume(string channelName, int volume);
}