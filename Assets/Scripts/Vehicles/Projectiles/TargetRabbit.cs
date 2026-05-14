using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.LowLevelPhysics2D;

[RequireComponent(typeof(AudioSource))]
public class TargetRabbit : NetworkBehaviour
{
    [SerializeField] private AudioClip _boomSfx;
    [SerializeField] private GameObject _boomVfxPrefab;
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    public void BoomStart()
    {
        Debug.Log("[TargetRabbit] Boom Start");
        // Effect 할거 여기서
        PlaySoundClientRpc();
    }

    public void BoomStop()
    {
        Debug.Log("[TargetRabbit] Boom Stop");
        // Effect 끌거 여기서
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.greenYellow;
        Gizmos.DrawSphere(transform.position, 1);
    }
    
    [ClientRpc(InvokePermission = RpcInvokePermission.Everyone)]
    private void PlaySoundClientRpc()
    {
        Debug.Log("[TargetRabbit] Play Boom Sound");
        _audioSource.volume = ServiceLocator.Get<IAudioService>().GetSfxVolume();
        _audioSource.PlayOneShot(_boomSfx);
        StartCoroutine(BoomCoroutine());
    }

    private IEnumerator BoomCoroutine()
    {
        _boomVfxPrefab.SetActive(true);
        yield return new WaitForSeconds(3f);
        _boomVfxPrefab.SetActive(false);
    }
}