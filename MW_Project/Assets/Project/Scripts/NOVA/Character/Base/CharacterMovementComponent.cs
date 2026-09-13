using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class CharacterMovementComponent : NovaComponent
{
    [Header("Movement Settings")]
    [Tooltip("플레이어 이동 속도")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("플레이어 달리기 속도")] 
    [SerializeField] private float sprintSpeed = 8f;
    [Tooltip("캐릭터 회전 속도")] 
    [SerializeField] private float rotationSpeed = 10f; 

    [Header("Sprint Settings")]
    [Tooltip("달리기 시작 시 즉시 소모되는 스테미나")]
    [SerializeField] private float sprintStartStaminaCost = 10f;

    [Header("Jump Settings")]
    [Tooltip("플레이어 점프 파워")]
    [SerializeField] private float jumpPower = 7f;
    [Tooltip("추가 중력 배율")]
    [SerializeField] private float fallGravityMultiplier = 2f;
    [Tooltip("점프 상승 중 추가 중력")]
    [SerializeField] private float lowJumpGravityMultiplier = 2f;

    [Header("Ground Settings")]
    [Tooltip("지면 체크 레이어")]
    [SerializeField] private LayerMask groundMask;

    [Header("Animation Settings")]
    [Tooltip("애니메이터 컴포넌트")]
    [SerializeField] private Animator animator = null;

    private Rigidbody rb = null;
    private CapsuleCollider capsuleCollider = null;
    private AttributeComponent attributes = null;

    private Transform mainCameraTransform = null;
    private Vector2 moveInput = Vector2.zero;

    private bool jumpInput = false;
    private bool sprintHeld = false;
    private bool isSprinting = false;
    private bool isGrounded = false;
    private bool canMove = true;

    public bool CanMove => canMove;
    public bool IsGrounded => isGrounded;
    public bool IsSprinting => isSprinting;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        attributes = GetComponent<AttributeComponent>();

        if (!rb) Debug.LogError("Rigidbody를 찾을 수 없습니다.", this);
        if (!capsuleCollider) Debug.LogError("CapsuleCollider를 찾을 수 없습니다.", this);

        if (!attributes) Debug.LogError("AttributeComponent를 찾을 수 없습니다.", this);
    }

    private void Start()
    {
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;
        else Debug.LogWarning("Main Camera를 찾을 수 없습니다.", this);
    }

    private void Update()
    {
        if (!canMove) return;

        CheckGrounded();
        HandleJump();
        HandleJumpGravity();
        HandleStamina();
        HandleAnimation();
    }

    private void FixedUpdate()
    {
        if (!canMove) return;

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
        if (!rb) return;

        if (moveInput == Vector2.zero)
        {
            SetLinearVelocity(new Vector3(0f, GetLinearVelocityVector().y, 0f));

            return;
        }

        Vector3 moveDirection = GetMoveDirection();

        if (moveDirection == Vector3.zero) return;

        HandleRotation(moveDirection);

        float movementSpeed = isSprinting ? sprintSpeed : moveSpeed;

        Vector3 targetVelocity = moveDirection * movementSpeed;
        targetVelocity.y = GetLinearVelocityVector().y;

        SetLinearVelocity(targetVelocity);
    }

    private void HandleRotation(Vector3 moveDirection)
    {
        if (moveDirection == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
    }

    private void HandleJump()
    {
        if (!jumpInput) return;
        if (!isGrounded) return;

        Vector3 velocity = GetLinearVelocityVector();
        velocity.y = jumpPower;

        SetLinearVelocity(velocity);

        jumpInput = false;
    }

    private void HandleJumpGravity()
    {
        if (isGrounded) return;

        Vector3 velocity = GetLinearVelocityVector();

        if (velocity.y < 0f)
        {
            rb.AddForce(Physics.gravity * (fallGravityMultiplier - 1f), ForceMode.Acceleration);
        }
        else if (velocity.y > 0f && !jumpInput)
        {
            rb.AddForce(Physics.gravity * (lowJumpGravityMultiplier - 1f), ForceMode.Acceleration);
        }
    }

    private void HandleStamina() 
    {
        if (attributes == null) return;
        if (isSprinting)
        {
            if (attributes.CurrentStamina <= 0f)
            {
                isSprinting = false;
            }
            
            return;
        }
        
        attributes.RecoverStamina(Time.deltaTime);
    }
    
    private void HandleAnimation()
    {
        if (animator == null) return;

        bool isMoving = moveInput != Vector2.zero;
        bool isWalk = isMoving && !isSprinting;
        bool isRun = isMoving && isSprinting;
        bool isJump = !isGrounded;

        animator.SetBool("isWalk", isWalk);
        animator.SetBool("isRun", isRun);
        animator.SetBool("isJump", isJump);
    }
    
    private Vector3 GetMoveDirection()
    {
        if (moveInput == Vector2.zero) return Vector3.zero;
        if (mainCameraTransform == null) return Vector3.zero;
        
        Vector3 cameraForward = mainCameraTransform.forward;
        Vector3 cameraRight = mainCameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();
        
        Vector3 direction = cameraForward * moveInput.y + cameraRight * moveInput.x;
        
        return direction.normalized;
    }
    
    private Vector3 GetLinearVelocityVector()
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
    
    public void SetMovementEnabled(bool enabled)
    {
        canMove = enabled;

        if (!enabled)
        {
            Vector3 velocity = GetLinearVelocityVector();

            velocity.x = 0f;
            velocity.z = 0f;

            SetLinearVelocity(velocity);
            
            isSprinting = false;
            sprintHeld = false;
        }
    }
    
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!canMove) return;
        if (context.canceled)
        {
            moveInput = Vector2.zero;
            
            return;
        }
        
        moveInput = context.ReadValue<Vector2>();
    }
    
    public void OnJump(InputAction.CallbackContext context)
    {
        if (!canMove) return;

        if (context.performed)
        {
            jumpInput = true;
        }
        else if (context.canceled)
        {
            jumpInput = false;
        }
    }
    
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (!canMove) return;
        if (attributes == null) return;

        if (context.started)
        {
            if (attributes.CurrentStamina <= 0f)return;
            if (!attributes.TryConsumeStamina(sprintStartStaminaCost)) return;

            sprintHeld = true;
            isSprinting = true;
        }
        else if (context.canceled)
        {
            sprintHeld = false;
            isSprinting = false;
        }
    }
}
