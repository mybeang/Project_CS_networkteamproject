using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class TankWarp : NetworkBehaviour
{
    [SerializeField] LayerMask _warpMask;
    Rigidbody _rigidbody;
    Transform _warpPoints; // 포인트 위치
    private Vector3 _warpLerf = new (0, 1, 0);

    // List<Transform> _warps = new List<Transform>(9);
    
    bool _isWarp = true; // 워프 가능한지?

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        // OnWarpSearch();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[TankWarp] Compare Layer {Utils.CompareLayer(other.gameObject.layer, _warpMask)}");
        if (!Utils.CompareLayer(other.gameObject.layer, _warpMask)) return;
        Debug.Log("[TankWarp] Connected Warp Point");
        if (_isWarp && IsOwner) // 워프가 가능하다면
        {
            while (true)
            {
                // int randomIndex = Random.Range(0, _warps.Count); // 랜덤 번호표
                // Transform targetWarp = _warps[randomIndex]; // 랜덤값 해당 워프위치에 부여
                Transform targetWarp = ServiceLocator.Get<IWarpManager>()?.GetWarpPoint();
                Debug.Log($"[TankWarp] Target Warp Point = {targetWarp?.position}");
                if (targetWarp == null)
                {
                    Debug.LogError("[TankWarp] Not Found Warp Point");
                    return;
                }
                if (targetWarp.position != other.transform.position) // 만약 랜덤한 위치가 본인자리가 아닐경우만
                {
                    Debug.Log("[TankWarp] Warp Success");
                    _rigidbody.linearVelocity = Vector3.zero;
                    _rigidbody.position = targetWarp.position + _warpLerf; // 대상의 위치를 워프시킨다.
                    _isWarp = false; // 또 다시 워프되지 않게 false 반환
                    return;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!Utils.CompareLayer(other.gameObject.layer, _warpMask)) return;
        if (!_isWarp) // 워프를 했다면
        {
            StartCoroutine(WaitCoolTime()); // 2초후에 다시 가능
        }
    }

    private IEnumerator WaitCoolTime() // 워프의 쿨타임
    {
        yield return new WaitForSeconds(2.0f); // 쿨타임 2초
        _isWarp = true; // 다시 워프를 가능하게 true 반환
        Debug.Log("[TankWarp] CoolTime 활성화");
    }

    
    // // 워프 포인트의 장소를 미리 찾고 초기화하는 역할을 수행
    // private void OnWarpSearch()
    // {
    //     // WarpPoints 오브젝트를 검색하여 transform을 반환한다.
    //     _warpPoints = GameObject.Find("WarpPoints").transform;
    //
    //     foreach (Transform obj in _warpPoints) // 하위 오브젝트 검색
    //     {
    //         _warps.Add(obj);// 찾았으면 추가
    //     }
    // }
}
