using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleMovement : NetworkBehaviour
{

    [Header("기본 설정")]
    [SerializeField] private PlayerableStatisticsSO _vehicleData; 
    [SerializeField] private Rigidbody _rb;

    [Header("UI")]
    [SerializeField] private Canvas _driverUICanvas;

    private Driver_UI_Tank _driverUI; // TODO : 나중에 상위 객체를 받아서 전환하게 바꾸기
    private InputSystem_Actions _inputActions;

    private bool canMove;
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

        if (!IsOwner)
            return;
        _driverUI = _driverUICanvas.GetComponent<Driver_UI_Tank>();
        _driverUICanvas.enabled = true;
    }

    private void Awake()
    {
        _inputActions = new InputSystem_Actions(); // TODO : 메니저 할당 후 재 편성
    }

    private void OnEnable()
    {
        StartCoroutine(Freeze());
        _inputActions.Player.Move.performed += Movement;
        _inputActions.Player.Move.canceled += Movement;
        _inputActions.Enable();
    }

    private void OnDisable()
    {
        canMove = false;
        _inputActions.Player.Move.performed -= Movement;
        _inputActions.Player.Move.canceled -= Movement;
        _inputActions.Disable();
    }

    IEnumerator Freeze()
    {
        canMove = false;
        yield return new WaitForSeconds(1f);
        canMove = true;
    }

    // 상위 객체에서 관리 되는
    public void SetDriverData(PlayerableStatisticsSO so)
    {
        _vehicleData = so;
    }

    public void Movement(InputAction.CallbackContext ctx) // TODO : TankController에서 사망 상태 
    {
        if (!IsOwner || !canMove) return;
        Vector2 input = ctx.ReadValue<Vector2>();
        lastInput = input;

        // 여기서 나중에 ServerRpc 혹은 ClientRpc로 전달하면 될듯...?
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !canMove) return;

        // TODO : 경사로에서 회전시 일정 회전이 후 회전 불가 현상 해결 필요.

        Vector3 tempVector = transform.forward * lastInput.y * _vehicleData.VechicleMoveSpeed;
        _rb.linearVelocity = new Vector3(tempVector.x, _rb.linearVelocity.y, tempVector.z);
        _rb.angularVelocity = _rb.angularVelocity + (transform.up * lastInput.x) * _vehicleData.VechicleRotationSpeed;

        if (lastInput.y == 0 && Physics.Raycast(transform.position, transform.up * -1, 1))
        {
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        }

        // 입력 부재 시 미끄럼 방지 (Sticky Friction) 필요한지 검증 후 적용
        /*
         if (lastInput.magnitude < 0.05f)
        {
            Vector3 vel = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
            rb.AddForce(-vel * 0.5f, ForceMode.VelocityChange);
        }*/
    }

    /// <summary>
    /// 여기에 물리 충격에 대한 설정
    /// </summary>
    public void ImpactPhysic(Vector3 pos, Vector3 dir, float power)
    {
        
    }
}
