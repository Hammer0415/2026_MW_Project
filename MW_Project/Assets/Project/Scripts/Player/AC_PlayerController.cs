using UnityEngine;
using UnityEngine.InputSystem;

public class AC_PlayerController : MonoBehaviour
{
    //=====Components=====//
    private Rigidbody rb = null;
    private CapsuleCollider collider = null;
    private AC_Player player = null;
    private AC_TargetingController targeting = null;

    [Header("Animation Settings")]
    [Tooltip("애니메이터 컴포넌트")]
    public Animator animator = null;
    //====================//

    //=====ReferenceVars=====//
    [ReadOnly]
    [SerializeField] private bool canMove = true;

    public bool CanMove => canMove;
    //=======================//

    //=====InputValues=====//
    private Vector2 moveInput = Vector2.zero;
    private bool jumpInput = false;
    //=====================//

    //=====PlayerSetting=====//
    [Header("Player Movement Settings")]
    [Tooltip("플레이어 이동 속도")]
    [SerializeField] private float moveSpeed = 0.0f;
    [Tooltip("플레이어 달리기 속도")]
    [SerializeField] private float sprintSpeed = 0.0f;
    [Tooltip("캐릭터 회전 속도")]
    [SerializeField] private float rotationSpeed = 0.0f;

    [Header("Sprint Settings")]
    [Tooltip("달리기 시작 시 즉시 소모되는 스테미나")]
    [SerializeField] private float sprintStartStaminaCost = 0.0f;
    [Tooltip("달리기 시작 전 회피 거리")]
    [SerializeField] private float dodgeDistance = 0.0f;
    [Tooltip("회피 지속 시간")]
    [SerializeField] private float dodgeDuration = 0.0f;

    [Header("Jump Settings")]
    [Tooltip("플레이어 점프 파워")]
    [SerializeField] private float jumpPower = 0.0f;
    [Tooltip("추가 중력 배율")]
    [SerializeField] private float fallGravityMultiplier = 0.0f;
    [Tooltip("점프 상승 중 추가 중력")]
    [SerializeField] private float lowJumpGravityMultiplier = 0.0f;

    [Header("Layers")]
    [Tooltip("지면 체크 레이어")]
    [SerializeField] private LayerMask groundMask;
    //======================//

    //=====CameraSettings=====//
    private Transform mainCameraTransform = null;
    //========================//

    //=====CheckingVars=====//
    private bool isGrounded = false;
    private bool sprintHeld = false;
    private bool isDodging = false;
    //======================//

    //=====OtherSettings=====//
    private float dodgeTimer = 0.0f;
    private Vector3 dodgeDirection = Vector3.zero;
    //===================//

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
        if (canMove)
        {
            CheckGrounded();
            HandleJump();
            HandleJumpGravity();
        }
        
        HandleAnimation();
    }

    private void FixedUpdate()
    {
        if (canMove)
        {
            if (isDodging)
            {
                HandleDodge();
            }
            else
            {
                HandleMovement();
            }
        }
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
        if (targeting != null && targeting.IsTargeting && targeting.CurrentTarget != null)
        {
            Vector3 targetDirection = targeting.CurrentTarget.position - transform.position;
            targetDirection.y = 0.0f;

            if (targetDirection.sqrMagnitude > 0.01f)
            {
                targetDirection.Normalize();

                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }
        else
        {
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
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

    private void HandleJumpGravity()
    {
        if (isGrounded) return;

        Vector3 velocity = GetLinearVelocity();

        if (velocity.y < 0.0f)
        {
            rb.AddForce(Physics.gravity * (fallGravityMultiplier - 1.0f), ForceMode.Acceleration);
        }
        else if (velocity.y > 0.0f && !jumpInput)
        {
            rb.AddForce(Physics.gravity * (lowJumpGravityMultiplier - 1.0f), ForceMode.Acceleration);
        }
    }

    private void StartDodge()
    {
        if (isDodging)
            return;

        isDodging = true;
        dodgeTimer = dodgeDuration;

        if (moveInput != Vector2.zero)
        {
            Vector3 camForward = mainCameraTransform.forward;
            Vector3 camRight = mainCameraTransform.right;

            camForward.y = 0.0f;
            camRight.y = 0.0f;

            camForward.Normalize();
            camRight.Normalize();

            dodgeDirection =
                (camForward * moveInput.y +
                camRight * moveInput.x).normalized;
        }
        else
        {
            dodgeDirection = transform.forward;
        }
    }

    private void HandleDodge()
    {
        dodgeTimer -= Time.fixedDeltaTime;

        Vector3 velocity = dodgeDirection *
                        (dodgeDistance / dodgeDuration);

        velocity.y = GetLinearVelocity().y;

        SetLinearVelocity(velocity);

        if (dodgeTimer <= 0.0f)
        {
            isDodging = false;

            // Shift를 계속 누르고 있으면 달리기
            if (sprintHeld &&
                player.Stemina() > 0.0f &&
                !player.isStaminaExhausted)
            {
                player.isSprint = true;
            }
        }
    }

    private void HandleAnimation()
    {
        if (animator == null) return;

        bool isMoving = moveInput != Vector2.zero;

        bool isWalk = isMoving && !player.isSprint;
        bool isRun = isMoving && player.isSprint;

        bool isJump = !isGrounded;

        animator.SetBool("isWalk", isWalk);
        animator.SetBool("isRun", isRun);
        animator.SetBool("isJump", isJump);
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
        if (targeting == null) targeting = GetComponent<AC_TargetingController>();

        if (player == null) Debug.LogError("AC_Player를 찾을 수 없습니다.");
        if (targeting == null) Debug.LogError("AC_TargetingController를 찾을 수 없습니다.");
        //====================//

        canMove = true;
    }
    //======================================//

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

        if (context.started)
        {
            if (player.isStaminaExhausted)
                return;

            if (player.Stemina() <= 0.0f)
                return;

            // 시작 스테미나 즉시 소모
            if (!player.UseStamina(sprintStartStaminaCost))
                return;

            sprintHeld = true;

            StartDodge();
        }

        else if (context.canceled)
        {
            sprintHeld = false;
            player.isSprint = false;
        }
    }
    //======================================//
}
