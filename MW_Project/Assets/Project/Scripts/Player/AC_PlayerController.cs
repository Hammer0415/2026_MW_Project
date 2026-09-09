using UnityEngine;
using UnityEngine.InputSystem;

public class AC_PlayerController : MonoBehaviour
{
    //=====Components=====//
    private Rigidbody rb = null;
    private CapsuleCollider collider = null;
    private AC_Player player = null;
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
    [SerializeField] private float moveSpeed = 0.0f;
    [Tooltip("플레이어 달리기 속도")]
    [SerializeField] private float sprintSpeed = 0.0f;
    [Tooltip("플레이어 점프 파워")]
    [SerializeField] private float jumpPower = 0.0f;
    [Tooltip("캐릭터 회전 속도")]
    [SerializeField] private float rotationSpeed = 0.0f;
    [Tooltip("지면 체크 레이어")]
    [SerializeField] private LayerMask groundMask;
    //======================//

    //=====CameraSettings=====//
    private Transform mainCameraTransform = null;
    //========================//

    //=====CheckingVars=====//
    private bool isGrounded = false;
    //======================//

    private void Awake()
    {
        InitSetup();
    }

    private void Start()
    {
        StartInitSetup();
    }
    
    private void Update()
    {
        CheckGrounded();
        HandleJump();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void CheckGrounded()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        float rayDistance = 0.25f;
        isGrounded = Physics.Raycast(rayOrigin, Vector3.down, rayDistance, groundMask);
    }

    private void HandleMovement()
    {
        if (!player) return;

        // Idle
        if (moveInput == Vector2.zero)
        {
            SetLinearVelocity(new Vector3(0f, GetLinearVelocity().y, 0.0f));
            return;
        }

        // Movement
        Vector3 camForward = mainCameraTransform.forward;
        Vector3 camRight = mainCameraTransform.right;
        camForward.y = 0.0f;
        camRight.y = 0.0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;

        // Rotation
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        // Speed Apply
        float movementSpeed = (player.isSprint) ? sprintSpeed : moveSpeed;
        Vector3 targetVelocity = moveDirection * movementSpeed;
        targetVelocity.y = GetLinearVelocity().y;

        SetLinearVelocity(targetVelocity);
    }

    private void HandleJump()
    {
        if (jumpInput && isGrounded)
        {
            Vector3 vel = GetLinearVelocity();
            vel.y = jumpPower;
            SetLinearVelocity(vel);
            jumpInput = false;
        }
    }

    //======================================//
    private Vector3 GetLinearVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    private void SetLinearVelocity(Vector3 velocity)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = velocity;
#else
        rb.velocity = velocity;
#endif
    }
    //======================================//

    //======================================//
    private void InitSetup()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (collider == null) collider = GetComponent<CapsuleCollider>();

        if (rb == null) Debug.LogError("Rigidbody를 찾을 수 없습니다.");
        if (collider == null) Debug.LogError("CapsuleCollider를 찾을 수 없습니다.");
    }
    
    private void StartInitSetup()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        //=====Components=====//
        if (player == null) player = AC_Player.Instance;

        if (player == null) Debug.LogError("AC_Player를 찾을 수 없습니다.");
        //====================//
    }
    //======================================//

    #region Input Value Functions
    //======================================//
    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.canceled) moveInput = Vector2.zero;
        else moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed) jumpInput = true;
        else if (context.canceled) jumpInput = false;
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (player == null) return;

        if (context.performed)
        {
            if (player.Stemina() > 0.0f && !player.isStaminaExhausted)
            {
                sprintInput = context.ReadValue<float>();
                player.isSprint = true;
            }
        }
        else if (context.canceled)
        {
            sprintInput = 0.0f;
            player.isSprint = false;
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed) attackInput = true;
        else if (context.canceled) attackInput = false;
    }
    //======================================//
    #endregion
}
