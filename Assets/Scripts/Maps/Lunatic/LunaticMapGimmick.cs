using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class LunaticMapGimmick : EventTask
{
    #region Variables_Exposed_in_Inspector
    [Header("메테오 애니메이션용\n임시로 게임 오브젝트")]
    [SerializeField] private MeteorSO _smallMeteor;
    [SerializeField] private MeteorSO _mediumMeteor;
    [SerializeField] private MeteorSO _largeMeteor;

    //[SerializeField] private int _

    [SerializeField] private ParticleSystem _meteor;

    // 후에 추가될 부분
    [SerializeField] private ParticleSystem _wind;
    [SerializeField] private ParticleSystem _tail;
    [SerializeField] private ParticleSystem _explosionSize;
    // 여기까지

    [SerializeField] private GameObject _particleSystem;
    [SerializeField] private Transform _parent;

    [SerializeField] private LayerMask _targetObject;
    #endregion

    #region Private_Variables

    private List<ParticleSystem> _particles = new List<ParticleSystem>(24);

    //private Vector3[] _meteorSpawnPos;
    private ParticleSystem.EmitParams[] _emitParams;

    private Vector3[] _meteorSpawnPos;

    private int _currentStage;
    private MeteorSO _currentSO;

    private bool _isInit = false;
    #endregion

    private void Awake()
    {
        _currentSO = new MeteorSO();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }
    public void ChangeStage()
    {
        switch (_currentStage)
        {
            case 0: case 1: case 2:
                _currentSO = _smallMeteor.UpMeteor(_currentSO,_currentStage);
                break;
            case 3: case 4: case 5:
                _currentSO = _mediumMeteor.UpMeteor(_currentSO, _currentStage % 3);
                break;
            case 6:
                _currentSO = _largeMeteor.UpMeteor(_currentSO, _currentStage % 3);
                break;
        }
        _currentStage++;
    }

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        _currentStage = 0;
        _currentSO = _smallMeteor;
        GameObject obj;

        for (int i = 0; i < _particles.Capacity; i++)
        {
            obj = Instantiate(_particleSystem, _parent);
            _particles.Add(obj.GetComponent<ParticleSystem>());
        }
        Debug.Log($"[{name}] {_particles.Count}개 초기화 완료");
    }

    private  void GeneratePos()
    {
        Debug.Log($"[{name}] Is Server : {IsServer}");

        _meteorSpawnPos = new Vector3[_currentSO.meteorMaxSpawnMeteor];

        for (int i = 0; i < 16; i++)
        {
            if(i < _currentSO.meteorMaxSpawnMeteor)
            {
                _meteorSpawnPos[i] = new Vector3(
                Random.Range(_currentSO.meteorMinHorizontalRange, _currentSO.meteorMaxHorizontalRange),
                300,
                Random.Range(_currentSO.meteorMinVerticalRange, _currentSO.meteorMaxVerticalRange));
            }
        }

        Debug.Log($"[{name}] 좌표 측정 완료");
        SpawnMeteorClientRpc(_meteorSpawnPos, _currentSO.meteorMaxSpawnMeteor);
    }

    [ClientRpc]
    private void SpawnMeteorClientRpc(Vector3[] meteorSpawnPos, int maxSpawn)
    {
        _meteorSpawnPos = meteorSpawnPos;
        if (_meteorSpawnPos == null) return;
        _emitParams = new ParticleSystem.EmitParams[maxSpawn];
        Debug.Log($"{name} : {_meteorSpawnPos.Length}");
        for (int i = 0; i < _meteorSpawnPos.Length; i++)
        {
            Debug.Log($"[{name}] {i} 번째 메테오 소환 준비");
            _particles[i].transform.position = _meteorSpawnPos[i];

            if (Physics.Raycast(_particles[i].transform.position, _particles[i].transform.up * -1, out RaycastHit hit, 330))
            {
                Vector3 point = hit.point;
                Debug.Log($"[{hit.transform.name}] 대상 위치 : {point}");


                //emitParams.position = new Vector3(Random.Range(_minHorizontalRange,_maxHorizontalRange),0,Random.Range(_minVerticalRange,_maxVerticalRange));
                _emitParams[i].velocity = (point - _particles[i].transform.position).normalized * 50;

                _particles[i].Emit(_emitParams[i], 1);
                StartCoroutine(Waiter((point - _meteorSpawnPos[i]).magnitude / _emitParams[i].velocity.magnitude, point));

                Debug.Log($"[{name}] {i} 번째 메테오 소환 성공");
            }
        }
    }

    private IEnumerator Waiter(float targetTime, Vector3 point)
    {
        Debug.Log($"시간 시작 됌 / {targetTime}");
        float _startTime = Time.time;
        float _currentTime = 0;
        while (_currentTime < targetTime)
        {
            _currentTime = Time.time - _startTime;
            yield return null;
        }
        Debug.Log("붐!");
        if (!IsServer) yield break;
        DesignatDamageableGroundServerRpc(point, PlayerTeamEnum.neutralObject);
    }

    [ServerRpc]
    private void DesignatDamageableGroundServerRpc(Vector3 point, PlayerTeamEnum self)
    {
        RaycastHit[] _hitedTargets = new RaycastHit[5];
        // 지정된 위치에 구형 범위를 측정 및 일정 시간(짧은 시간) 후에 범위 안에 들어간 객체(피해를 입을 수 있는 객체)들 판정( 판정할 때 조심해야될 부분이 닿은 부위를 기준으로 해야됌, 중심으로 받으면 안됌)
        int count = Physics.SphereCastNonAlloc(
            point,
            30,
            Vector3.forward,
            _hitedTargets,
            0.001f,
            _targetObject);

        Debug.Log($"적중 위치 기반 탐지 된 대상 : {count}");

        if (count < 1 || _hitedTargets == null) return;

        for (int i = 0; i < count; i++)
        {
            Debug.Log($"검출된 대상 : {_hitedTargets[i].collider.name}");
            Debug.Log($"폭발 중심지에서 대상까지의 거리 : {Vector3.Distance(_hitedTargets[i].collider.ClosestPoint(point), point)}");

            (_hitedTargets[i].collider.GetComponent<TankController>() as IDamageableObject)
            .TakeDamaged(
                    (int)Mathf.Lerp( // 거리에 따라 피해를 다를 게 주기 위해(선형 보간 처리를 위해) Mathf.Lerp로 처리
                        100,
                        (100 / 4),
                        Vector3.Distance(_hitedTargets[i].collider.ClosestPoint(point), point) / 10),self); // 폭심지를 기준으로 콜라이더의 접촉부위 중 가장 가까운 지점과 거리 비교 후 피해량 측정
            Debug.Log($"TakeDamage: {100} , {100 / 4} , {Vector3.Distance(_hitedTargets[i].collider.ClosestPoint(point), point) / 5}");
        }
    }

    public override void OnEventSpawn()
    {
        if (!IsServer) return;
        ChangeStage();
        GeneratePos();
    }

    public override void OnEventDespawn()
    {
        
    }
}
