using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class BaseVehicle : NetworkBehaviour
{
    [Header("기본 설정")]
    [SerializeField] protected PlayerableStatisticsSO _vehicleData;
    [SerializeField] protected Rigidbody _rb;

    [Header("객체 설정")]
    [SerializeField] protected NetworkVariable<int> _currentHP = new NetworkVariable<int>();
    [SerializeField] protected NetworkVariable<bool> _isInvulnerable = new NetworkVariable<bool>(false);
    

    protected bool canMove;
    private WaitForSeconds _uploadTick;

    public override void OnNetworkSpawn()
    {
        _currentHP.Value = _vehicleData.VechicleMaximumHP;
        /*
        if (IsOwner)
        {
            _uploadTick = new WaitForSeconds(0.2f);
            StartCoroutine(DataUpLoadTick());
        }*/
        canMove = true;
    }

    // 특정 주기 마다 물리 기반 상태를 업로드를 위해 준비해 둔 것
    IEnumerator DataUpLoadTick()
    {
        CallFuntionsServerRpc();
        yield return _uploadTick;
    }

    [ServerRpc]
    public void CallFuntionsServerRpc()
    {
        //ProcessMovement();

    }

    // 폭발, 일반, 
    public abstract void ProcessMovement(InputAction.CallbackContext ctx);

    public abstract void ProcessTurret(InputAction.CallbackContext ctx);

    public abstract void ApplyImpulse(Vector3 force, Vector3 position);
}
