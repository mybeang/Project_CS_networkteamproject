using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BlizzardEvent : EventTask
{
    [Header("틱 데미지 설정")]
    [SerializeField] private int _tickDamage = 10; // 틱당 데미지량
    [SerializeField] private float _tickInterval = 2f; // 데미지 쿨타임

    List<TankController> _tanks = new List<TankController>();
    BonfireArea[] _bonfires; // 씬에 있는 모든 모닥불
    private Coroutine _tickCoroutine; // 코루틴을 중단하기 위한 변수

    public override void OnEventSpawn() // 이벤트 발생
    {
        if (!IsServer) return; // 서버에서만 실행

        if (_tanks.Count == 0)
        {
            GetTankController();
            _bonfires = FindObjectsByType<BonfireArea>(FindObjectsSortMode.None);
        }

        if (_tickCoroutine == null)
        {
            _tickCoroutine = StartCoroutine(TickDamageCoroutine());
            ShowBlizzardEffectClientRpc();
        }

    }

    public override void OnEventDespawn() // 이벤트 종료
    {
        if (!IsServer) return; // 서버에서만 실행

        // 틱 데미지 코루틴 중단
        if (_tickCoroutine != null)
        {
            StopCoroutine(_tickCoroutine);
            _tickCoroutine = null;
            HideBlizzardEffectClientRpc();
        }
    }

    public override void OnNetworkSpawn() // 이벤트 동기화(EventManager 문서참고)
    {
        
    }

    [ClientRpc]
    private void ShowBlizzardEffectClientRpc()
    {
        foreach (var tank in _tanks)
        {
            Debug.Log($"{_tanks}가 탐색되었습니다.");
            tank.ViewEffectControl(true);
        }
    }

    [ClientRpc]
    private void HideBlizzardEffectClientRpc()
    {
        foreach (var tank in _tanks)
        {
            tank.ViewEffectControl(false);
        }
    }

    // 틱 데미지를 반복적으로 주는 코루틴
    private IEnumerator TickDamageCoroutine()
    {
        while (true) // 이벤트 종료 시 StopCoroutine으로 중단됨
        {
            yield return new WaitForSeconds(_tickInterval); // 쿨타임만큼 대기

            foreach (TankController tank in _tanks)
            {
                // 모닥불 안에 있는지 확인
                if (!IsTankInBonfire(tank))
                {
                    // 모닥불 바깥이면 틱데미지 (TankController의 데미지를 참조하여 데이터를 넘긴다)
                    tank.TakeDamaged(_tickDamage, PlayerTeamEnum.neutralObject);
                }
            }
        }
    }

    // 특정 탱크가 모닥불 범위 안에 있는지 확인
    private bool IsTankInBonfire(TankController tank)
    {
        foreach (BonfireArea bonfire in _bonfires)
        {
            if (bonfire.IsTankInArea(tank))
            {
                return true; // 하나라도 범위 안이면 true
            }
        }
        return false; // 어떤 모닥불에도 안 들어가 있으면 false
    }

    private void GetTankController()
    {
        var tanks = ServiceLocator.Get<IGameManager>().GetPlayableObjects();
        foreach (var tank in tanks.Values)
        {
            _tanks.Add(tank.GetComponent<TankController>());
        }
    }
}