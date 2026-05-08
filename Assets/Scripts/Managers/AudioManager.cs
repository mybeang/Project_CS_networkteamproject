using UnityEngine;

public class AudioManager : Manager<AudioManager>, IAudioService
{
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;

    private void OnEnable() => Register();
    private void OnDisable() => Unregister();
    
    protected override void Register() => ServiceLocator.Register<IAudioService>(this);
    protected override void Unregister() => ServiceLocator.Unregister<IAudioService>();
    
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        _bgmSource.clip = clip;
        _bgmSource.Play();
    }

    public void GetBGMVolume(out float volume) => volume = _bgmSource.volume;
    public void SetBGMVolume(float volume) => _bgmSource.volume = volume;
   
    public void PlayOneShotSfx(AudioClip clip)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip);
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        _sfxSource.clip = clip;
        _sfxSource.Play();
    }

    public void PlayStopSfx() => _sfxSource.Stop();
    public void GetSfxVolume(out float volume) => volume = _sfxSource.volume;
    public void SetSfxVolume(float volume) => _sfxSource.volume = volume;
}
