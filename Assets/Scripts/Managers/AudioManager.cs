using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


[Serializable]
public class SfxData
{
    public SfxEnum sfxEnum;
    public AudioClip clip;
}

public class AudioManager : NetworkManager<AudioManager>, IAudioService
{
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;
    [SerializeField] private AudioClip _btSound;
    [SerializeField] private List<AudioClip> _mapBgmClips;
    [SerializeField] private AudioClip _mainBgmClips;
    [SerializeField] private List<SfxData> _sfxBgmClips = new();
    
    private Dictionary<PlayerTeamEnum, AudioSource> _playerSfxClips = new();
    
    private void OnEnable() => Register();
    private void OnDisable() => Unregister();
    
    protected override void Register() => ServiceLocator.Register<IAudioService>(this);
    protected override void Unregister() => ServiceLocator.Unregister<IAudioService>(this);

    protected override void Init()
    {
        _bgmSource.volume = 0.3f;
        _sfxSource.volume = 1f;
    }

    public void PlayMainBGM()
    {
        if (_mainBgmClips == null) return;
        _bgmSource.clip = _mainBgmClips;
        _bgmSource.Play();
    }

    public void PlayBGM(int mapNum)
    {
        if (mapNum > _mapBgmClips.Count) return;
        var clip = _mapBgmClips[mapNum];
        if (clip == null) return;
        _bgmSource.clip = clip;
        _bgmSource.Play();
    }

    public float GetBGMVolume() => _bgmSource.volume;
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
    public float GetSfxVolume() => _sfxSource.volume;
    public void SetSfxVolume(float volume)
    {
        _sfxSource.volume = volume;
        foreach (var ad in _playerSfxClips.Values)
            ad.volume = volume;
    }

    public void PlayButtonSfx() => _sfxSource.PlayOneShot(_btSound);
    public void AddAudioSource(PlayerTeamEnum team, AudioSource audioSource)
    {
        if (_playerSfxClips.ContainsKey(team)) return;
        audioSource.volume = _sfxSource.volume;
        _playerSfxClips.Add(team, audioSource);
    }

    public void RemoveAudioSource(PlayerTeamEnum team)
    {
        if (!_playerSfxClips.ContainsKey(team)) return;
        _playerSfxClips.Remove(team);
    }

    private AudioClip FindSfxData(SfxEnum sfxEnum)
    {
        foreach (SfxData sd in _sfxBgmClips)
        {
            if (sd.sfxEnum == sfxEnum) return sd.clip;
        }
        return null;
    }
    
    [ClientRpc(InvokePermission = RpcInvokePermission.Everyone)]
    public void PlayOneShotSfxClientRpc(PlayerTeamEnum team, SfxEnum sfxEnum)
    {
        Debug.Log($"[AudioManager] PlayOneShotSfxClientRpc - {team}:{sfxEnum}");
        var clip = FindSfxData(sfxEnum);
        if (clip != null && _playerSfxClips.TryGetValue(team, out var audioSource))
        {
            audioSource.PlayOneShot(clip);
            Debug.Log($"[AudioManager] PlayOneShotSfxClientRpc - clip: {clip.name}");
        }
    }

    [ClientRpc(InvokePermission = RpcInvokePermission.Everyone)]
    public void PlaySfxClientRpc(PlayerTeamEnum team, SfxEnum sfxEnum)
    {
        Debug.Log($"[AudioManager] PlaySfxClientRpc - {team}:{sfxEnum}");
        var clip = FindSfxData(sfxEnum);
        if (clip != null && _playerSfxClips.TryGetValue(team, out var audioSource))
        {
            audioSource.clip = clip;
            audioSource.Play();
            Debug.Log($"[AudioManager] PlaySfxClientRpc - clip: {clip.name}");
        }
    }

    [ClientRpc(InvokePermission = RpcInvokePermission.Everyone)]
    public void PlayStopSfxClientRpc(PlayerTeamEnum team)
    {
        Debug.Log($"[AudioManager] PlayStopClientRpc - {team}");
        if (_playerSfxClips.TryGetValue(team, out var audioSource))
        {
            audioSource.Stop();
            Debug.Log($"[AudioManager] PlayStopClientRpc - Stop!");
        }
    }
}
