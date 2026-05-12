using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleMovement : NetworkBehaviour, IImpactForce
{

    [Header("기본 설정")]
    [SerializeField] private PlayerableStatisticsSO _vehicleData;
    [SerializeField] private GameObject _driverCam;
    [SerializeField] private Rigidbody _rb;

    [Header("UI")]
    [SerializeField] private GameObject _driverUICanvas;

    private Coroutine _flipCounter;
    private Driver_UI _driverUI;

    private bool canMove;
    private bool _coroutineIsRunning;
    private bool _canFlip;
    private bool _isActiveScript;

    private Vector2 lastInput;

    public override void OnNetworkSpawn()
    {

        if (_rb == null)
            _rb = GetComponent<Rigidbody>();

        _rb.centerOfMass = new Vector3(0, -0.8f);

        _rb.angularVelocity = Vector3.zero;
        _rb.linearVelocity = Vector3.zero;

        if (IsOwner)
        {
            _rb.isKinematic = false;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        else
        {
            // 타인 차량은 물리 연산을 끄고 위치만 동기화받음 (호스트/클라이언트 공통 최적화)
            _rb.isKinematic = true;
        }
    }

    private void OnEnable()
    {
        if (!_isActiveScript && !IsClient) return;
        StartCoroutine(Freeze());
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.Move.performed += Movement;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.Move.canceled += Movement;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.Jump.performed += FlipVehicle;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.ScoreBoard.performed += OnScoreBoard;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Enable();

        _driverUICanvas.SetActive(true);
    }

    private void OnDisable()
    {
        if (!_isActiveScript && !IsClient) return;
        canMove = false;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.Move.performed -= Movement;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.Move.canceled -= Movement;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.Jump.performed -= FlipVehicle;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.ScoreBoard.performed -= OnScoreBoard;

        _coroutineIsRunning = false;
        if (_flipCounter != null)
            StopCoroutine(_flipCounter);
        _flipCounter = null;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Disable();

        _driverUICanvas.SetActive(false);
    }

    IEnumerator Freeze()
    {
        canMove = false;
        yield return new WaitForSeconds(1f);
        canMove = true;
    }

    // 상위 객체에서 관리 되는
    public void SetDriverData(PlayerableStatisticsSO so, TeamInfo teamInfo)
    {
        _vehicleData = so;
        foreach (var player in teamInfo.players)
        {
            if (player.role == PlayerRole.Driver && player.clientId == NetworkManager.Singleton.LocalClientId)
            {
                _isActiveScript = true;
                ActiveScript();
                break;
            }
        }
    }

    private void ActiveScript()
    {
        _driverUICanvas.SetActive(true);
        _driverCam.SetActive(true);
        StartCoroutine(Freeze());
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.Move.performed += Movement;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.Move.canceled += Movement;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.Jump.performed += FlipVehicle;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Player.ScoreBoard.performed += OnScoreBoard;
        ServiceLocator.Get<IInputSystem>().GetInputSystem().Enable();
    }
    private void OnScoreBoard(InputAction.CallbackContext ctx)
    {
        _driverUI.ShowScore();
    }

    public void Movement(InputAction.CallbackContext ctx)
    {
        if (!IsOwner || !canMove) return;
        Vector2 input = ctx.ReadValue<Vector2>();
        lastInput = input;
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !canMove) return;

        if (Physics.Raycast(transform.position, transform.up * -1, 1))
        {
            _canFlip = false;
            if (_coroutineIsRunning)
            {
                StopCoroutine(_flipCounter);
                _coroutineIsRunning = false;
            }

            if (lastInput.y == 0 && lastInput.x == 0)
            {
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
            }
            else
            {
                // TODO : 경사로에서 회전시 일정 회전이 후 회전 불가 현상 해결 필요.
                Vector3 tempVector = transform.forward * lastInput.y * _vehicleData.VechicleMoveSpeed;
                _rb.linearVelocity = new Vector3(tempVector.x, _rb.linearVelocity.y, tempVector.z);
                _rb.angularVelocity = _rb.angularVelocity + (transform.up * lastInput.x) * _vehicleData.VechicleRotationSpeed;
            }
        }
        else
        {
            if (!_coroutineIsRunning && !_canFlip)
            {
                _flipCounter = StartCoroutine(stuckChecker());
            }
        }

        // 입력 부재 시 미끄럼 방지 (Sticky Friction) 필요한지 검증 후 적용
        /*
         if (lastInput.magnitude < 0.05f)
        {
            Vector3 vel = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
            rb.AddForce(-vel * 0.5f, ForceMode.VelocityChange);
        }*/
    }

    IEnumerator stuckChecker()
    {
        _coroutineIsRunning = true;
        yield return new WaitForSeconds(3f);
        // 3초 이상 뜬 경우에는 높은 확률로 꼈거나 뒤집힌 거라고 판단.
        // UI 표시 및 키 입력으로 정상화
        Debug.Log("당신은 이제 일어설 수 있습니다.");
        _canFlip = true;

        _coroutineIsRunning = false;
        // 키 입력으로 뒤집기 실행 후 false로 전환
    }

    private void FlipVehicle(InputAction.CallbackContext ctx)
    {
        if (!_canFlip) return;
        _canFlip = false;
        transform.localRotation = Quaternion.EulerRotation(0, transform.localRotation.eulerAngles.y, 0);
        transform.position += Vector3.up * 2;
    }

    /// <summary>
    /// 여기에 물리 충격에 대한 설정
    /// </summary>
    public void ImpactPhysic(float explosionForce, Vector3 explosionPosition, float explosionRadius, float upwardsModifier)
    {
        // TODO : 반드시 ProjectileManager에서 호출 하는 부분 추가할 것.
        _rb.AddExplosionForce(explosionForce, explosionPosition, explosionRadius, upwardsModifier, ForceMode.Impulse);
    }
}
