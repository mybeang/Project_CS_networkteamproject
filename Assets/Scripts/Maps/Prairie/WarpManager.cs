using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class WarpManager : NetworkBehaviour
{
    Transform _warpPoints; // 포인트 위치

    List<Transform> _warps = new List<Transform>(9);

    bool _isWarp = true; // 워프 가능한지?

    private void Awake()
    {
        OnWarpSearch();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (_isWarp) // 워프가 가능하다면
        {
            while(true)
            {
                int randomIndex = Random.Range(0, _warps.Count); // 랜덤 번호표

                Transform targetWarp = _warps[randomIndex]; // 랜덤값 해당 워프위치에 부여

                if (targetWarp == other.transform) // 만약 랜덤한 위치가 본인자리라면?
                {
                    continue; // 무시하고 다시 번호표 뽑기
                }

                else // 그외 위치 변경
                {
                    transform.position = targetWarp.position;
                    _isWarp = false; // 또 다시 워프되지 않게 false 반환
                    break;
                }

            }
            
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_isWarp) // 워프를 했다면
        {
            _isWarp = true; // 다시 워프를 가능하게 true 반환
        }
    }


    // 워프 포인트의 장소를 미리 찾고 초기화하는 역할을 수행
    private void OnWarpSearch()
    {
        // WarpPoints 오브젝트를 검색하여 transform을 반환한다.
        _warpPoints = GameObject.Find("WarpPoints").transform;

        foreach (Transform obj in _warpPoints) // 하위 오브젝트 검색
        {
            _warps.Add(obj);// 찾았으면 추가
        }
    }


}
