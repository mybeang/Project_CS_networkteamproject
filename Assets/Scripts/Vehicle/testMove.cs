using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class testMove : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float rotateSpeed = 100f;

    private CharacterController controller;
    private InputSystem_Actions input;
    private Vector2 moveInput;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = new InputSystem_Actions();

        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += ctx => moveInput = Vector2.zero;
    }

    void OnEnable() => input.Enable();
    void OnDisable() => input.Disable();

    void Update()
    {
        // 회전
        transform.Rotate(0, moveInput.x * rotateSpeed * Time.deltaTime, 0);

        // 이동
        Vector3 move = transform.forward * moveInput.y;
        controller.Move(move * speed * Time.deltaTime);
    }
}