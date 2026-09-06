using UnityEngine;
using UnityEngine.InputSystem;

public class AC_PlayerController : MonoBehaviour
{
    //=====Components=====//
    private Rigidbody rb = null;
    private CapsuleCollider collider = null;
    private PlayerInput playerInput = null;
    //====================//

    //=====InputValues=====//
    private Vector2 moveInput = Vector2.zero;
    private bool jumpInput = false;
    private float sprintInput = 0.0f;
    private bool attackInput = false;
    //=====================//

    //=====PlayerValues=====//
    [Header("Player Settings")]
    [Tooltip("플레이어 이동 속도")]
    public float moveSpeed = 0.0f;
    [Tooltip("플레이어 달리기 속도")]
    public float sprintSpeed = 0.0f;
    [Tooltip("플레이어 점프 파워")]
    public float jumpPower = 0.0f;
    //======================//

    private void Awake()
    {
        InitSetup();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        moveDirection.Normalize();

        float movementSpeed = (sprintInput > 0.0f) ? sprintSpeed : moveSpeed;

        Vector3 targetVelocity = moveDirection * movementSpeed;

#if UNITY_6000_0_OR_NEWER
        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVelocity;
#else
        targetVelocity.y = rb.velocity.y;
        rb.velocity = targetVelocity;
#endif
    }

    //======================================//
    public void OnMovement(InputAction.CallbackContext context)
    {
        if (context.canceled) moveInput = Vector2.zero;
        else moveInput = context.ReadValue<Vector2>();

        Debug.Log($"현재 입력값: {moveInput}");
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed) jumpInput = true;
        else if (context.canceled) jumpInput = false;
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        sprintInput = context.ReadValue<float>();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed) attackInput = true;
        else if (context.canceled) attackInput = false;
    }
    //======================================//

    //======================================//
    private void InitSetup()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (collider == null) collider = GetComponent<CapsuleCollider>();
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();

        if (rb == null) Debug.LogError("Rigidbody를 찾을 수 없습니다.");
        if (collider == null) Debug.LogError("CapsuleCollider를 찾을 수 없습니다.");
        if (playerInput == null) Debug.LogError("PlayerInput을 찾을 수 없습니다.");
    }
    //======================================//
}
