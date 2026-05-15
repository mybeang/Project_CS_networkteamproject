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
    
    public Vector3 MyPosition
    {
        get { return transform.position; }
        set
        {
            transform.position = value; 
            Debug.Log($"[TargetRabbit] {name} Position: " + value);
        }
    }
    
    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }
    
    public void BoomStart(Vector3 pos)
    {
        Debug.Log($"[TargetRabbit] Boom Start - {transform.position}");
        // Effect 할거 여기서
        PlaySoundClientRpc();
        BoomEffectClientRpc(pos);
    }

    public void BoomStop()
    {
        Debug.Log($"[TargetRabbit] {name} Boom Stop");
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
        Debug.Log($"[TargetRabbit] {name} Play Boom Sound - {transform.position}");
        _audioSource.volume = ServiceLocator.Get<IAudioService>().GetSfxVolume();
        _audioSource.PlayOneShot(_boomSfx);
    }
    
    [ClientRpc(InvokePermission = RpcInvokePermission.Everyone)]
    private void BoomEffectClientRpc(Vector3 pos)
    {
        Debug.Log($"[TargetRabbit] {name} Effect Boom - {pos}");
        Instantiate(_boomVfxPrefab, pos, transform.rotation);
    }
}