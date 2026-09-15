using UnityEngine;
using System.Collections;
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

    [Header("Dodge Settings")]
    [Tooltip("대쉬 중 플레이어 메쉬 숨김")]
    [SerializeField] private Renderer[] characterRenderers;
    [Tooltip("대쉬 중 메쉬를 숨기는 시간 비율")]
    [Range(0f, 1f)]
    [SerializeField] private float dodgeHideRatio = 0.7f;

    private Rigidbody rb = null;
    private CapsuleCollider capsuleCollider = null;
    private AttributeComponent attributes = null;
    private AbilitySystemComponent abilitySystem;

    private Transform mainCameraTransform = null;
    private Vector2 moveInput = Vector2.zero;

    private bool jumpInput = false;
    private bool sprintHeld = false;
    private bool isSprinting = false;
    private bool isGrounded = false;
    private bool canMove = true;
    private bool isDodging = false;
    private bool dodgeDirectionReady = false;

    private Vector3 dodgeDirection = Vector3.zero;

    private float dodgeTimer = 0f;
    private float dodgeInputBufferTimer = 0f;
    private float dodgeSpeed = 0f;

    public bool CanMove => canMove;
    public bool IsGrounded => isGrounded;
    public bool IsSprinting => isSprinting;
    public bool IsDodging => isDodging;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        attributes = GetComponent<AttributeComponent>();
        abilitySystem = GetComponent<AbilitySystemComponent>();

        if (!rb) Debug.LogError("Rigidbody를 찾을 수 없습니다.", this);
        if (!capsuleCollider) Debug.LogError("CapsuleCollider를 찾을 수 없습니다.", this);

        if (!attributes) Debug.LogError("AttributeComponent를 찾을 수 없습니다.", this);
        if (!abilitySystem) Debug.LogError("AbilitySystemComponent를 찾을 수 없습니다.", this);
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
        
        if (isDodging)
        {
            HandleDodge();
            return;
        }

        HandleMovement();
    }

    private void CheckGrounded()
    {
        if (!capsuleCollider)
        {
            isGrounded = false;
            return;
        }

        Vector3 origin = capsuleCollider.bounds.center;
        float rayDistance = capsuleCollider.bounds.extents.y + 0.1f;

        isGrounded = Physics.Raycast(origin, Vector3.down, rayDistance, groundMask);
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

        float inputMagnitude = Mathf.Clamp01(moveInput.magnitude);
        float movementSpeed;

        if (isSprinting) movementSpeed = sprintSpeed;
        else movementSpeed = moveSpeed * inputMagnitude;

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

    public void StartDodge(float distance, float duration, float inputBufferTime)
    {
        if (isDodging) return;
        if (!canMove) return;

        if (distance <= 0f) return;
        if (duration <= 0f) return;

        isDodging = true;

        dodgeTimer = duration;
        dodgeSpeed = distance / duration;

        Vector3 moveDirection = GetMoveDirection();

        if (moveDirection != Vector3.zero)
        {
            dodgeDirection = moveDirection;
            dodgeDirectionReady = true;
            dodgeInputBufferTimer = 0f;
        }
        else
        {
            dodgeDirection = transform.forward;
            dodgeDirectionReady = false;
            dodgeInputBufferTimer = inputBufferTime;
        }

        isSprinting = false;
        jumpInput = false;

        StartCoroutine(DodgeRoutine(distance, duration, inputBufferTime));
    }

    private void HandleDodge()
    {
        if (!dodgeDirectionReady)
        {
            dodgeInputBufferTimer -= Time.fixedDeltaTime;

            Vector3 moveDirection = GetMoveDirection();

            if (moveDirection != Vector3.zero)
            {
                dodgeDirection = moveDirection;
                dodgeDirectionReady = true;
            }
            else if (dodgeInputBufferTimer <= 0f)
            {
                dodgeDirection = transform.forward;
                dodgeDirectionReady = true;
            }
        }

        if (!dodgeDirectionReady) return;

        dodgeTimer -= Time.fixedDeltaTime;

        Vector3 velocity = dodgeDirection * dodgeSpeed;

        velocity.y = GetLinearVelocityVector().y;
        SetLinearVelocity(velocity);

        if (dodgeDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dodgeDirection);

            transform.rotation = Quaternion.Slerp(transform.rotation,targetRotation,rotationSpeed * Time.fixedDeltaTime);
        }

        if (dodgeTimer <= 0f)
        {
            EndDodge();
        }
    }

    private void EndDodge()
    {
        isDodging = false;

        dodgeDirection = Vector3.zero;
        dodgeDirectionReady = false;

        dodgeTimer = 0f;
        dodgeInputBufferTimer = 0f;
        dodgeSpeed = 0f;

        Vector3 velocity = GetLinearVelocityVector();
        velocity.x = 0f;
        velocity.z = 0f;

        SetLinearVelocity(velocity);

        if (sprintHeld)
        {
            isSprinting = true;
        }
    }
    
    private void HandleAnimation()
    {
        if (animator == null) return;

        float move = moveInput.magnitude;

        if (move <= 0f)
        {
            animator.SetFloat("Move", 0f, 0.1f, Time.deltaTime);
            return;
        }

        if (isSprinting) move = 1f;
        else move = Mathf.Lerp(0.5f, 0.75f, move);

        animator.SetFloat("Move", move, 0.1f, Time.deltaTime);
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

    private void HideCharacterMesh()
    {
        if (characterRenderers == null) return;

        foreach (Renderer renderer in characterRenderers)
        {
            if (renderer) renderer.enabled = false;
        }
    }

    private void ShowCharacterMesh()
    {
        if (characterRenderers == null) return;

        foreach (Renderer renderer in characterRenderers)
        {
            if (renderer) renderer.enabled = true;
        }
    }

    private IEnumerator DodgeRoutine(float distance, float duration, float inputBufferTime)
    {
        float elapsed = 0f;

        HideCharacterMesh();

        bool meshShown = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (!meshShown && elapsed >= duration * dodgeHideRatio)
            {
                ShowCharacterMesh();
                meshShown = true;
            }

            yield return null;
        }

        if (!meshShown) ShowCharacterMesh();
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
            sprintHeld = true;

            if (abilitySystem != null)
            {
                abilitySystem.TryActivateAbility(new GameplayTag("Ability.Dodge"));
            }
        }
        else if (context.canceled)
        {
            sprintHeld = false;
            isSprinting = false;
        }
    }
}
