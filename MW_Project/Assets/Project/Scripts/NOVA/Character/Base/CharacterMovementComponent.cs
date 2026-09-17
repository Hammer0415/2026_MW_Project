using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

[AddComponentMenu("NOVA/Character/Character Movement Component")]
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

    [Header("Step Settings")]
    [Tooltip("오를 수 있는 계단의 최대 높이")]
    [Min(0f)]
    [SerializeField] private float maxStepHeight = 0.4f;
    [Tooltip("계단으로 처리할 최소 높이. 이보다 낮으면 일반 이동으로 처리한다.")]
    [Min(0f)]
    [SerializeField] private float minStepHeight = 0.05f;
    [Tooltip("계단을 오를 때 전방으로 확인할 거리")]
    [Min(0.05f)]
    [SerializeField] private float stepCheckDistance = 0.35f;

    [Header("Animation Settings")]
    [Tooltip("애니메이터 컴포넌트")]
    [SerializeField] private Animator animator = null;

    [Header("Dodge Settings")]
    [Tooltip("대쉬 중 플레이어 메쉬 숨김")]
    [SerializeField] private Renderer[] characterRenderers;
    [Tooltip("대쉬 중 메쉬를 숨기는 시간 비율")]
    [Range(0f, 1f)]
    [SerializeField] private float dodgeHideRatio = 0.7f;
    [Tooltip("회피 중 기본 무적 시간 비율. Ability에서 값을 넘기면 그쪽을 우선한다.")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultInvincibleRatio = 0.7f;
    [Tooltip("퍼펙트 회피 판정 시간")]
    [Min(0f)]
    [SerializeField] private float defaultPerfectDodgeWindow = 0.12f;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private AttributeComponent attributes;
    private AbilitySystemComponent abilitySystem;
    private CharacterAnimationComponent animationComponent;

    private Transform mainCameraTransform;
    private Vector2 moveInput;

    private bool jumpInput;
    private bool sprintHeld;
    private bool isSprinting;
    private bool isGrounded;
    private bool canMove = true;
    private bool isDodging;
    private bool dodgeDirectionReady;
    private bool canPerfectDodge;

    private Vector3 dodgeDirection;

    private float dodgeTimer;
    private float dodgeInputBufferTimer;
    private float dodgeSpeed;
    private float perfectDodgeTimer;
    private float jumpGroundCheckIgnoreTimer;
    private Coroutine dodgeRoutine;

    public bool CanMove => canMove;
    public bool IsGrounded => isGrounded;
    public bool IsSprinting => isSprinting;
    public bool IsDodging => isDodging;
    public Vector2 MoveInput => moveInput;
    public bool HasMoveInput => moveInput.sqrMagnitude > 0.01f;
    public bool HasJumpInput => jumpInput;
    public bool DidJump => jumpGroundCheckIgnoreTimer > 0f;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        attributes = GetComponent<AttributeComponent>();
        abilitySystem = GetComponent<AbilitySystemComponent>();
        animationComponent = GetComponent<CharacterAnimationComponent>();

        if (!rb) Debug.LogError("Rigidbody를 찾을 수 없습니다.", this);
        if (!capsuleCollider) Debug.LogError("CapsuleCollider를 찾을 수 없습니다.", this);
        if (!attributes) Debug.LogError("AttributeComponent를 찾을 수 없습니다.", this);
        if (!abilitySystem) Debug.LogError("AbilitySystemComponent를 찾을 수 없습니다.", this);
        if (!animator) animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;
        else Debug.LogWarning("Main Camera를 찾을 수 없습니다.", this);
    }

    private void Update()
    {
        TickPerfectDodge(Time.deltaTime);
        TickJumpGroundCheckIgnore(Time.deltaTime);
        CheckGrounded();
        HandleJumpGravity();

        if (canMove && !isDodging)
        {
            HandleJump();
        }

        HandleStamina();
        HandleAnimation();
    }

    private void FixedUpdate()
    {
        if (isDodging)
        {
            HandleDodge();
            return;
        }

        if (!canMove) return;

        HandleMovement();
    }

    // 캡슐 하단으로 지면 레이캐스트를 쏴 착지 여부를 확인한다.
    private void CheckGrounded()
    {
        if (jumpGroundCheckIgnoreTimer > 0f)
        {
            isGrounded = false;
            return;
        }

        if (!capsuleCollider)
        {
            isGrounded = false;
            return;
        }

        Vector3 origin = capsuleCollider.bounds.center;
        float rayDistance = capsuleCollider.bounds.extents.y + 0.1f;

        isGrounded = Physics.Raycast(origin, Vector3.down, rayDistance, groundMask);
    }

    // 점프 직후 일정 시간 동안은 착지 판정을 하지 않는다.
    private void TickJumpGroundCheckIgnore(float deltaTime)
    {
        if (jumpGroundCheckIgnoreTimer <= 0f) return;

        jumpGroundCheckIgnoreTimer = Mathf.Max(jumpGroundCheckIgnoreTimer - deltaTime, 0f);
    }

    // 카메라 기준 이동 입력을 속도로 변환하고 캐릭터를 회전시킨다.
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
        HandleStepClimb(moveDirection);

        float inputMagnitude = Mathf.Clamp01(moveInput.magnitude);
        float movementSpeed = isSprinting ? sprintSpeed : moveSpeed * inputMagnitude;

        Vector3 targetVelocity = moveDirection * movementSpeed;
        targetVelocity.y = GetLinearVelocityVector().y;

        SetLinearVelocity(targetVelocity);
    }

    // 이동 방향을 향해 회전한다. 적 바라보기는 공격 중에만 CombatComponent가 처리한다.
    private void HandleRotation(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
    }

    // 이동 방향 앞에 오를 수 있는 단차가 있으면 캐릭터를 계단 위로 올린다.
    private void HandleStepClimb(Vector3 moveDirection)
    {
        if (!isGrounded) return;
        if (!capsuleCollider) return;
        if (maxStepHeight <= 0f) return;
        if (moveDirection.sqrMagnitude <= 0.001f) return;

        float radius = capsuleCollider.radius;
        Vector3 footOrigin = transform.position + Vector3.up * (minStepHeight + 0.02f);
        Vector3 forward = moveDirection.normalized;

        if (!Physics.Raycast(footOrigin, forward, stepCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        Vector3 highOrigin = transform.position + Vector3.up * (maxStepHeight + 0.05f);

        if (Physics.Raycast(highOrigin, forward, stepCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        Vector3 probeOrigin = transform.position + forward * (radius + 0.05f) + Vector3.up * (maxStepHeight + 0.05f);

        if (!Physics.Raycast(probeOrigin, Vector3.down, out RaycastHit stepHit, maxStepHeight + 0.1f, groundMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        float stepHeight = stepHit.point.y - transform.position.y;

        if (stepHeight < minStepHeight || stepHeight > maxStepHeight)
        {
            return;
        }

        Vector3 position = rb.position;
        position.y = stepHit.point.y;
        rb.MovePosition(position);
    }

    // 점프 입력이 들어왔고 땅에 있으면 수직 속도를 준다.
    private void HandleJump()
    {
        if (!jumpInput) return;
        if (!isGrounded) return;
        if (isDodging) return;

        Vector3 velocity = GetLinearVelocityVector();
        velocity.y = jumpPower;

        SetLinearVelocity(velocity);

        isGrounded = false;
        jumpGroundCheckIgnoreTimer = 0.1f;
        jumpInput = false;

        PlayJumpAnimation();
    }

    // 낙하/점프 상승 구간에 추가 중력을 적용한다.
    private void HandleJumpGravity()
    {
        if (isGrounded) return;
        if (!rb) return;

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

    // 달리지 않을 때 스테미나를 회복한다.
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

    // 회피를 시작하고 무적/퍼펙트 회피 창을 연다.
    public void StartDodge(float distance, float duration, float inputBufferTime)
    {
        StartDodge(distance, duration, inputBufferTime, duration * defaultInvincibleRatio, defaultPerfectDodgeWindow);
    }

    // 회피 거리, 시간, 무적, 퍼펙트 회피 창을 지정해 회피를 시작한다.
    public void StartDodge(float distance, float duration, float inputBufferTime, float invincibleDuration, float perfectDodgeWindow)
    {
        if (isDodging) return;
        if (distance <= 0f) return;
        if (duration <= 0f) return;

        SetRootMotionEnabled(false);

        isDodging = true;
        dodgeTimer = duration;
        dodgeSpeed = distance / duration;
        dodgeInputBufferTimer = Mathf.Max(inputBufferTime, 0f);

        Vector3 moveDirection = GetMoveDirection();

        if (moveDirection != Vector3.zero)
        {
            dodgeDirection = moveDirection;
            dodgeDirectionReady = true;
        }
        else
        {
            dodgeDirection = transform.forward;
            dodgeDirectionReady = false;
        }

        isSprinting = false;
        jumpInput = false;
        canMove = true;
        canPerfectDodge = perfectDodgeWindow > 0f;
        perfectDodgeTimer = perfectDodgeWindow;

        if (attributes) attributes.SetInvincible(invincibleDuration);

        ApplyDodgeVelocity();

        if (dodgeRoutine != null)
        {
            StopCoroutine(dodgeRoutine);
        }

        dodgeRoutine = StartCoroutine(DodgeRoutine(duration));
    }

    // 퍼펙트 회피 강공격처럼 회피를 중간에 끊고 메쉬를 다시 보여준다.
    public void CancelDodge()
    {
        if (dodgeRoutine != null)
        {
            StopCoroutine(dodgeRoutine);
            dodgeRoutine = null;
        }

        ShowCharacterMesh();

        if (isDodging)
        {
            EndDodge();
        }
    }

    // 회피 방향으로 빠르게 이동하고, 입력 보정 시간 동안은 방향을 갱신한다.
    private void HandleDodge()
    {
        UpdateDodgeDirectionFromBuffer();
        ApplyDodgeVelocity();

        dodgeTimer -= Time.fixedDeltaTime;

        if (dodgeTimer <= 0f)
        {
            EndDodge();
        }
    }

    // 입력 보정 시간 안에 들어온 이동 방향으로 회피 방향을 맞춘다.
    private void UpdateDodgeDirectionFromBuffer()
    {
        if (dodgeInputBufferTimer <= 0f) return;

        dodgeInputBufferTimer -= Time.fixedDeltaTime;

        Vector3 moveDirection = GetMoveDirection();

        if (moveDirection != Vector3.zero)
        {
            dodgeDirection = moveDirection;
            dodgeDirectionReady = true;
            return;
        }

        if (dodgeInputBufferTimer > 0f) return;

        if (!dodgeDirectionReady)
        {
            dodgeDirection = transform.forward;
            dodgeDirectionReady = true;
        }
    }

    // 현재 회피 방향으로 속도를 적용하고 그 쪽을 바라본다.
    private void ApplyDodgeVelocity()
    {
        if (dodgeDirection == Vector3.zero)
        {
            dodgeDirection = transform.forward;
        }

        Vector3 velocity = dodgeDirection * dodgeSpeed;
        velocity.y = GetLinearVelocityVector().y;
        SetLinearVelocity(velocity);

        Quaternion targetRotation = Quaternion.LookRotation(dodgeDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
    }

    // 회피 중에는 공격 애니메이션의 Root Motion이 회피 이동을 덮지 않게 한다.
    private void OnAnimatorMove()
    {
        if (!animator) return;

        if (isDodging) return;
        if (!animator.applyRootMotion) return;

        Vector3 deltaPosition = animator.deltaPosition;
        Quaternion deltaRotation = animator.deltaRotation;

        if (rb)
        {
            rb.MovePosition(rb.position + deltaPosition);
            rb.MoveRotation(rb.rotation * deltaRotation);
            return;
        }

        transform.position += deltaPosition;
        transform.rotation = deltaRotation * transform.rotation;
    }

    // 회피 상태를 해제하고 달리기를 이어갈 수 있으면 이어간다.
    private void EndDodge()
    {
        isDodging = false;
        canPerfectDodge = false;
        dodgeDirection = Vector3.zero;
        dodgeDirectionReady = false;
        dodgeTimer = 0f;
        dodgeInputBufferTimer = 0f;
        dodgeSpeed = 0f;
        perfectDodgeTimer = 0f;

        Vector3 velocity = GetLinearVelocityVector();
        velocity.x = 0f;
        velocity.z = 0f;
        SetLinearVelocity(velocity);

        if (sprintHeld)
        {
            isSprinting = true;
        }
    }

    // 퍼펙트 회피 가능 시간을 감소시킨다.
    private void TickPerfectDodge(float deltaTime)
    {
        if (!canPerfectDodge) return;

        perfectDodgeTimer -= deltaTime;

        if (perfectDodgeTimer <= 0f)
        {
            canPerfectDodge = false;
            perfectDodgeTimer = 0f;
        }
    }

    // 퍼펙트 회피 창 안에서 적의 공격을 흘리면 true를 반환한다.
    public bool TryConsumePerfectDodge()
    {
        if (!canPerfectDodge) return false;

        canPerfectDodge = false;
        perfectDodgeTimer = 0f;

        GameplayEventBus.Raise(new GameplayTag("Combat.PerfectDodge"), Owner, Owner);

        return true;
    }

    // 이동 블렌드와 점프/공중 상태를 Animator에 전달한다.
    private void HandleAnimation()
    {
        float move = 0f;

        if (canMove && !isDodging)
        {
            move = moveInput.magnitude;

            if (move > 0f)
            {
                if (isSprinting) move = 1f;
                else move = Mathf.Lerp(0.5f, 0.75f, move);
            }
        }

        SetMoveAnimation(move);
        SetInAirAnimation(!isGrounded);
    }

    // 점프 Trigger를 재생한다.
    private void PlayJumpAnimation()
    {
        if (animationComponent)
        {
            animationComponent.PlayJump();
            return;
        }

        if (!animator) return;

        animator.SetBool("isInAir", true);
        animator.SetTrigger("isJump");
    }

    // 공중 상태를 Animator에 전달한다.
    private void SetInAirAnimation(bool inAir)
    {
        if (animationComponent)
        {
            animationComponent.SetInAir(inAir);
            return;
        }

        if (!animator) return;

        animator.SetBool("isInAir", inAir);
    }

    // 이동 애니메이션 값을 애니메이션 컴포넌트 또는 Animator에 전달한다.
    private void SetMoveAnimation(float moveValue)
    {
        if (animationComponent)
        {
            animationComponent.SetMove(moveValue);
            return;
        }

        if (!animator) return;

        animator.SetFloat("Move", moveValue, 0.1f, Time.deltaTime);
    }

    // 카메라 기준으로 이동 방향을 계산한다.
    private Vector3 GetMoveDirection()
    {
        if (!HasMoveInput) return Vector3.zero;
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
        return RigidbodyVelocity.Get(rb);
    }

    private void SetLinearVelocity(Vector3 velocity)
    {
        RigidbodyVelocity.Set(rb, velocity);
    }

    // 이동 가능 여부를 설정한다. 잠그면 수평 속도를 멈춘다.
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

    // Root Motion 사용 여부를 설정한다.
    public void SetRootMotionEnabled(bool enabled)
    {
        if (animationComponent)
        {
            animationComponent.SetRootMotion(enabled);
            return;
        }

        if (!animator) return;

        animator.applyRootMotion = enabled;
    }

    // 회피 동안 캐릭터 메쉬를 숨긴다.
    private void HideCharacterMesh()
    {
        if (characterRenderers == null) return;

        foreach (Renderer renderer in characterRenderers)
        {
            if (renderer) renderer.enabled = false;
        }
    }

    // 숨겼던 캐릭터 메쉬를 다시 보이게 한다.
    private void ShowCharacterMesh()
    {
        if (characterRenderers == null) return;

        foreach (Renderer renderer in characterRenderers)
        {
            if (renderer) renderer.enabled = true;
        }
    }

    // 회피 연출용으로 메쉬를 숨겼다가 일정 비율이 지나면 다시 보여준다.
    private IEnumerator DodgeRoutine(float duration)
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

        dodgeRoutine = null;
    }

    // 공격 애니메이션 Trigger를 재생한다.
    public void HandleAttackAnimation(string TriggerName)
    {
        if (animationComponent)
        {
            animationComponent.PlayTrigger(TriggerName, true);
            return;
        }

        if (!animator) return;

        animator.applyRootMotion = true;
        animator.SetTrigger(TriggerName);
    }

    // 이동 입력을 받는다.
    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = context.ReadValue<Vector2>();
    }

    // 점프 입력을 받는다. 콤보 중에도 입력을 기억해 두고, 이동이 풀리면 점프한다.
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            jumpInput = true;
        }
        else if (context.canceled)
        {
            jumpInput = false;
        }
    }

    // 스프린트 입력을 받는다. 누르면 먼저 회피 Ability를 시전한다.
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (attributes == null) return;

        if (context.started)
        {
            sprintHeld = true;

            PerfectDodgeComponent perfectDodge = GetComponent<PerfectDodgeComponent>();

            if (perfectDodge && perfectDodge.BlocksOtherActions) return;

            if (abilitySystem != null)
            {
                abilitySystem.TryActivateAbility(abilitySystem.DodgeTag);
            }
        }
        else if (context.canceled)
        {
            sprintHeld = false;
            isSprinting = false;
        }
    }
}
