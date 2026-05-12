using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BlizzardEvent : EventTask
{
    // 탱크별 모닥불 범위 진입 여부를 관리하는 딕셔너리
    Dictionary<TankController, bool> _isWarm = new Dictionary<TankController, bool>();

    TankController[] _tanks; // 런타임 중 탱크를 갖고와야 하는 변수
    private bool _isSearchDic; // 키값을 갖고왔는지 판별하는 변수

    public override void OnEventSpawn() // 이벤트 발생
    {
        if (!_isSearchDic)
        {
            SearchBlizzardTargets();
            _isSearchDic = true;
        }

        // TODO: 틱 데미지 반복 시작
    }

    public override void OnEventDespawn() // 이벤트 종료
    {
        // TODO: 틱 데미지 반복 중단
    }

    public override void OnNetworkSpawn() // 이벤트 동기화(EventManager 문서참고)
    {

    }

    // 씬에 존재하는 탱크를 탐색하여 딕셔너리에 등록
    private void SearchBlizzardTargets()
    {
        _tanks = FindObjectsByType<TankController>(FindObjectsSortMode.None);
        foreach (TankController obj in _tanks)
        {
            _isWarm.Add(obj, false); // 초기값: 모닥불 밖
        }
    }
}