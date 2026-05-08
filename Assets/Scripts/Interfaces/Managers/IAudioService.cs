using UnityEngine;

public interface IAudioService
{
    // BGM
    public void PlayBGM(AudioClip clip);
    public void GetBGMVolume(out float volume);
    public void SetBGMVolume(float volume);
    
    // SFX 
    public void PlayOneShotSfx(AudioClip clip);
    public void PlaySfx(AudioClip clip);
    public void PlayStopSfx();
    public void GetSfxVolume(out float volume);
    public void SetSfxVolume(float volume);
    public void PlayButtonSfx();
}