using UnityEngine;

public interface IAudioService
{
    // BGM
    public void PlayMainBGM();
    public void PlayBGM(int mapNum);
    public float GetBGMVolume();
    public void SetBGMVolume(float volume);
    
    // SFX 
    public void PlayOneShotSfx(AudioClip clip);
    public void PlaySfx(AudioClip clip);
    public void PlayStopSfx();
    public float GetSfxVolume();
    public void SetSfxVolume(float volume);
    public void PlayButtonSfx();
}