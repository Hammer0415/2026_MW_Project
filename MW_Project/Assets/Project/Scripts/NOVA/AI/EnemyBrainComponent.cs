using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("NOVA/AI/Enemy Brain Component")]
[RequireComponent(typeof(Rigidbody))]
public class EnemyBrainComponent : NovaComponent
{
    private enum EnemyState
    {
        Idle,
        Chase,
        Dash,
        Attack
    }

    [Header("Definition")]
    [Tooltip("적 데이터 에셋. 지정하면 아래 기본값 대신 에셋 값을 사용한다.")]
    [SerializeField] private EnemyDefinition definition;

    [Header("Enemy Type")]
    [Tooltip("적 유형. Definition이 있으면 Definition 값을 우선한다.")]
    [SerializeField] private EnemyType enemyType = EnemyType.Melee;

    [Header("AI Settings")]
    [Tooltip("플레이어 감지 거리")]
    [SerializeField] private float detectionRange = 12f;
    [Tooltip("공격 트리거 범위")]
    [SerializeField] private float attackRange = 2f;
    [Tooltip("이동 속도")]
    [SerializeField] private float moveSpeed = 3.5f;
    [Tooltip("방향 회전 속도")]
    [SerializeField] private float rotationSpeed = 8f;
    [Tooltip("플레이어에게 피격된 후 추적을 유지하는 시간")]
    [SerializeField] private float aggroDuration = 5f;

    [Header("Attack Settings")]
    [Tooltip("공격 Ability. 비어 있으면 애니메이션과 근접 판정만 사용한다.")]
    [SerializeField] private NovaAbility attackAbility;
    [Tooltip("공격 애니메이션 Trigger")]
    [SerializeField] private string attackTrigger = "isAttack";
    [Tooltip("공격 딜레이")]
    [SerializeField] private float attackDelay = 1.5f;
    [Tooltip("공격 판정 위치")]
    [SerializeField] private Transform attackPoint;
    [Tooltip("근거리 공격 판정 반경")]
    [SerializeField] private float attackHitRange = 1.5f;
    [Tooltip("공격 데미지")]
    [SerializeField] private float attackDamage = 10f;
    [Tooltip("플레이어 레이어")]
    [SerializeField] private LayerMask playerLayer;
    [Tooltip("원거리 공격 사거리. Definition이 있으면 Definition 값을 우선한다.")]
    [SerializeField] private float rangedAttackRange = 12f;
    [Tooltip("원거리 공격 발사 이펙트")]
    [SerializeField] private GameObject rangedAttackEffectPrefab;
    [Tooltip("원거리 공격 발사 이펙트 유지 시간")]
    [SerializeField] private float rangedAttackEffectLifetime = 0.5f;
    [Tooltip("공격 애니메이션에서 퍼펙트 회피가 가능한 시간")]
    [Min(0.05f)]
    [SerializeField] private float perfectDodgeWindowDuration = 0.25f;

    [Header("Melee Dash")]
    [Tooltip("이 거리 안으로 들어오면 근거리 적이 대쉬로 달려든다. 0이면 대쉬하지 않는다.")]
    [Min(0f)]
    [SerializeField] private float dashTriggerRange = 5.5f;
    [Tooltip("대쉬 속도")]
    [Min(0f)]
    [SerializeField] private float dashSpeed = 16f;
    [Tooltip("대쉬가 유지되는 시간")]
    [Min(0.05f)]
    [SerializeField] private float dashDuration = 0.28f;
    [Tooltip("대쉬가 다시 나가기까지 기다리는 시간")]
    [Min(0f)]
    [SerializeField] private float dashCooldown = 2.2f;
    [Tooltip("대쉬 애니메이션 Trigger")]
    [SerializeField] private string dashTrigger = "isDash";
    [Tooltip("탱커가 대쉬 직후 공격할 때 쓰는 딜레이. 0이면 0.5초를 쓴다.")]
    [Min(0f)]
    [SerializeField] private float dashAttackDelay = 0.5f;
    [Tooltip("대쉬 중 메쉬를 숨기는 시간 비율")]
    [Range(0f, 1f)]
    [SerializeField] private float dashHideRatio = 0.7f;
    [Tooltip("적 대쉬 이펙트. 비어 있으면 플레이어 대쉬 이펙트를 쓴다.")]
    [SerializeField] private GameObject dashEffectPrefab;
    [Tooltip("대쉬 이펙트 유지 시간")]
    [SerializeField] private float dashEffectLifetime = 0.5f;
    [Tooltip("공격 전에 플레이어를 바라봤다고 판정하는 각도")]
    [Min(0.5f)]
    [SerializeField] private float attackFaceAngle = 8f;

    [Header("Vision")]
    [Tooltip("이 레이어의 오브젝트가 플레이어와 적 사이를 가리면 적을 인식하지 않는다.")]
    [SerializeField] private LayerMask wallMask;

    [Header("Stun")]
    [Tooltip("강공격 스턴 중에 적용할 머티리얼. 비어 있으면 Resources의 M_Stun을 쓴다.")]
    [SerializeField] private Material stunMaterial;

    [Header("Weak Point")]
    [Tooltip("약점 표시 오브젝트")]
    [SerializeField] private GameObject weakPoint;

    [Header("State")]
    [ReadOnly]
    [SerializeField] private EnemyState currentState = EnemyState.Idle;
    [ReadOnly]
    [SerializeField] private bool canMove = true;
    [ReadOnly]
    [SerializeField] private float aggroTimer;

    private Rigidbody rb;
    private Animator animator;
    private AttributeComponent attributes;
    private AbilitySystemComponent abilitySystem;
    private CombatComponent combat;
    private Transform playerTarget;
    private float attackTimer;
    private bool isInBattle;
    private int currentPhase = 1;
    private float lastHealth;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector3 dashDirection;
    private float stunTimer;
    private float perfectDodgeWindowTimer;
    private Renderer[] stunRenderers;
    private Material[][] originalMaterials;
    private bool stunMaterialApplied;
    private bool hasMoveParameter;
    private EffectComponent effectComponent;
    private Renderer[] dashRenderers;
    private Coroutine dashMeshRoutine;
    private bool isAttackLocked;
    private float attackLockGrace;
    private bool attackHitApplied;

    public EnemyType EnemyType => definition ? definition.EnemyType : enemyType;
    public bool CanMove => canMove;
    public bool IsWeakPointActive => weakPoint && weakPoint.activeSelf;
    private bool IsDead => attributes && attributes.IsDead;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        attributes = GetComponent<AttributeComponent>();
        abilitySystem = GetComponent<AbilitySystemComponent>();
        combat = GetComponent<CombatComponent>();
        effectComponent = GetComponent<EffectComponent>();

        ApplyDefinition();
        CacheDashRenderers();

        if (wallMask.value == 0)
        {
            wallMask = LayerMask.GetMask("Wall");
        }

        if (!stunMaterial)
        {
            stunMaterial = Resources.Load<Material>("Characters/Test Enemies/Materials/M_Stun");
        }

        CacheOriginalMaterials();

        if (weakPoint) weakPoint.SetActive(false);
        if (attributes)
        {
            lastHealth = attributes.CurrentHealth;
            attributes.OnDied += HandleDeath;
            attributes.OnHealthChanged += HandleHealthChanged;
        }

        GrantAttackAbilities();
        CacheMoveParameter();
    }

    private void Start()
    {
        FindPlayer();
    }

    private void CacheMoveParameter()
    {
        hasMoveParameter = false;
        if (!animator) return;

        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == "Move")
            {
                hasMoveParameter = true;
                return;
            }
        }
    }

    private void UpdateMoveAnimation()
    {
        if (!hasMoveParameter) return;

        bool moving = !isAttackLocked && (currentState == EnemyState.Chase || currentState == EnemyState.Dash);
        animator.SetFloat("Move", moving ? 1f : 0f);
    }

    private void OnDestroy()
    {
        if (attributes)
        {
            attributes.OnDied -= HandleDeath;
            attributes.OnHealthChanged -= HandleHealthChanged;
        }
    }

    private void Update()
    {
        TickStun();
        TickPerfectDodgeWindow();
        TickAttackLock();

        if (IsDead) return;
        if (!canMove) return;
        if (!playerTarget)
        {
            FindPlayer();
            return;
        }

        if (attackTimer > 0f) attackTimer -= Time.deltaTime;
        if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;

        UpdateAggroTimer();
        UpdateBattleState();
        UpdateBossPhase();

        if (currentState != EnemyState.Dash)
        {
            UpdateState();
        }

        HandleCurrentState();
        UpdateMoveAnimation();
    }

    // 공격 Ability를 AbilitySystem에 등록한다.
    private void GrantAttackAbilities()
    {
        if (!abilitySystem) return;

        if (attackAbility) abilitySystem.GrantAbility(attackAbility);

        if (definition == null || definition.Abilities == null) return;

        for (int i = 0; i < definition.Abilities.Length; i++)
        {
            abilitySystem.GrantAbility(definition.Abilities[i]);
        }
    }

    // Definition이 있으면 Inspector 기본값 대신 에셋 값을 사용한다.
    private void ApplyDefinition()
    {
        if (!definition) return;

        enemyType = definition.EnemyType;
        detectionRange = definition.DetectionRange;
        attackRange = definition.AttackRange;
        moveSpeed = definition.MoveSpeed;
        rotationSpeed = definition.RotationSpeed;
        aggroDuration = definition.AggroDuration;
        attackDelay = definition.AttackDelay;
        attackHitRange = definition.MeleeHitRadius;
        attackDamage = definition.AttackDamage;
        rangedAttackRange = definition.RangedAttackRange;
        dashTriggerRange = definition.DashTriggerRange;
        dashSpeed = definition.DashSpeed;
        dashDuration = definition.DashDuration;
        dashCooldown = definition.DashCooldown;

        if (definition.AttackAbility) attackAbility = definition.AttackAbility;

        if (attributes)
        {
            attributes.Configure(definition.MaxHealth, definition.Defense);
        }
    }

    // 거리와 어그로에 따라 Idle/Chase/Attack을 고른다.
    private void UpdateState()
    {
        if (isAttackLocked)
        {
            currentState = EnemyState.Attack;
            return;
        }

        if (!CanPerceivePlayer())
        {
            currentState = EnemyState.Idle;
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance <= GetCurrentAttackRange())
        {
            currentState = EnemyState.Attack;
            return;
        }

        if (IsAggro() || distance <= detectionRange)
        {
            currentState = EnemyState.Chase;
            return;
        }

        currentState = EnemyState.Idle;
    }

    // 현재 상태에 맞는 행동을 실행한다.
    private void HandleCurrentState()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdle();
                break;
            case EnemyState.Chase:
                HandleChase();
                break;
            case EnemyState.Dash:
                HandleDash();
                break;
            case EnemyState.Attack:
                HandleAttack();
                break;
        }
    }

    // 제자리에 선다.
    private void HandleIdle()
    {
        StopMovement();
    }

    // 플레이어를 향해 이동한다. 근거리 적은 일정 거리에서 대쉬로 전환한다.
    private void HandleChase()
    {
        if (!playerTarget) return;

        if (CanStartDash())
        {
            StartDash();
            return;
        }

        Vector3 direction = playerTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
        {
            StopMovement();
            return;
        }

        direction.Normalize();

        Vector3 velocity = direction * moveSpeed;
        velocity.y = RigidbodyVelocity.Get(rb).y;
        RigidbodyVelocity.Set(rb, velocity);
        RotateTo(direction);
    }

    // 이동을 멈추고, 플레이어를 다 바라본 뒤에만 공격한다.
    private void HandleAttack()
    {
        StopMovement();

        if (isAttackLocked) return;
        if (!playerTarget) return;
        if (attackTimer > 0f) return;

        if (!IsFacingPlayer())
        {
            FacePlayer();
            return;
        }

        SnapFacingToPlayer();
        TryAttack();
        attackTimer = attackDelay;
    }

    // 플레이어 방향으로 짧게 돌진한다.
    private void HandleDash()
    {
        dashTimer -= Time.deltaTime;

        if (dashDirection.sqrMagnitude > 0.01f)
        {
            Vector3 velocity = dashDirection * dashSpeed;
            velocity.y = RigidbodyVelocity.Get(rb).y;
            RigidbodyVelocity.Set(rb, velocity);
            RotateTo(dashDirection);
        }

        if (dashTimer > 0f) return;

        dashCooldownTimer = dashCooldown;
        StopDashPresentation();

        // 근접 대쉬는 공격 시작이므로, 대쉬가 끝나도 공격 모션이 끝날 때까지 Attack을 유지한다.
        if (isAttackLocked)
        {
            currentState = EnemyState.Attack;
            return;
        }

        if (!CanPerceivePlayer())
        {
            currentState = EnemyState.Idle;
            return;
        }

        float distance = playerTarget ? Vector3.Distance(transform.position, playerTarget.position) : float.MaxValue;

        if (distance <= GetCurrentAttackRange())
        {
            currentState = EnemyState.Attack;
            ApplyDashFollowUpAttackDelay();
            HandleAttack();
            return;
        }

        currentState = EnemyState.Chase;
    }

    // 근거리/탱커가 대쉬 거리에 들어왔고 쿨타임이 끝났는지 확인한다.
    private bool CanStartDash()
    {
        if (EnemyType != EnemyType.Melee && EnemyType != EnemyType.Tanker) return false;
        if (isAttackLocked) return false;
        if (dashTriggerRange <= 0f) return false;
        if (dashCooldownTimer > 0f) return false;
        if (!playerTarget) return false;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance > dashTriggerRange) return false;
        if (distance <= GetCurrentAttackRange()) return false;

        return true;
    }

    // 탱커 대쉬 공격은 애니메이션 이벤트에서 바로 나가므로 일반 공격을 겹치지 않게 한다.
    private void ApplyDashFollowUpAttackDelay()
    {
        if (EnemyType != EnemyType.Tanker) return;

        attackTimer = attackDelay;
    }

    // 대쉬를 시작한다. 근접 적은 이때부터 공격 애니메이션을 재생한다.
    private void StartDash()
    {
        Vector3 direction = playerTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f) return;

        dashDirection = direction.normalized;
        dashTimer = dashDuration;
        currentState = EnemyState.Dash;

        PlayDashPresentation();

        if (EnemyType == EnemyType.Melee)
        {
            StartMeleeDashAttack();
            return;
        }

        if (animator && !string.IsNullOrEmpty(dashTrigger))
        {
            animator.SetTrigger(dashTrigger);
        }
    }

    // 근접 대쉬는 돌진과 동시에 공격 애니메이션을 시작한다.
    private void StartMeleeDashAttack()
    {
        BeginAttackLock();
        attackTimer = attackDelay;

        if (!animator || string.IsNullOrEmpty(attackTrigger)) return;

        animator.ResetTrigger(dashTrigger);
        animator.SetTrigger(attackTrigger);
    }

    private bool IsFacingPlayer()
    {
        Vector3 direction = GetPlanarDirectionToPlayer();

        if (direction.sqrMagnitude <= 0.01f) return true;

        return Vector3.Angle(transform.forward, direction) <= attackFaceAngle;
    }

    private void FacePlayer()
    {
        Vector3 direction = GetPlanarDirectionToPlayer();

        if (direction.sqrMagnitude > 0.01f) RotateTo(direction.normalized);
    }

    private void SnapFacingToPlayer()
    {
        Vector3 direction = GetPlanarDirectionToPlayer();

        if (direction.sqrMagnitude <= 0.01f) return;

        transform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    private Vector3 GetPlanarDirectionToPlayer()
    {
        if (!playerTarget) return Vector3.zero;

        Vector3 direction = playerTarget.position - transform.position;
        direction.y = 0f;
        return direction;
    }

    private void PlayDashPresentation()
    {
        SpawnDashEffect();

        if (dashMeshRoutine != null)
        {
            StopCoroutine(dashMeshRoutine);
        }

        dashMeshRoutine = StartCoroutine(DashMeshRoutine());
    }

    private void SpawnDashEffect()
    {
        if (!dashEffectPrefab) return;

        Vector3 origin = transform.position + Vector3.up * 0.8f;
        Vector3 direction = dashDirection.sqrMagnitude > 0.01f ? dashDirection : transform.forward;

        if (effectComponent)
        {
            effectComponent.SpawnEffect(dashEffectPrefab, origin, direction, dashEffectLifetime);
            return;
        }

        Quaternion rotation = Quaternion.LookRotation(direction);
        GameObject instance = Instantiate(dashEffectPrefab, origin, rotation);

        if (dashEffectLifetime > 0f)
        {
            Destroy(instance, dashEffectLifetime);
        }
    }

    private IEnumerator DashMeshRoutine()
    {
        SetDashMeshVisible(false);

        float elapsed = 0f;
        float hideDuration = dashDuration * dashHideRatio;

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= hideDuration)
            {
                SetDashMeshVisible(true);
                dashMeshRoutine = null;
                yield break;
            }

            yield return null;
        }

        SetDashMeshVisible(true);
        dashMeshRoutine = null;
    }

    private void CacheDashRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        List<Renderer> cached = new List<Renderer>(renderers.Length);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (!renderer) continue;
            if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer) continue;
            if (renderer.GetComponentInParent<Canvas>()) continue;

            cached.Add(renderer);
        }

        dashRenderers = cached.ToArray();
    }

    private void SetDashMeshVisible(bool visible)
    {
        if (dashRenderers == null) return;

        for (int i = 0; i < dashRenderers.Length; i++)
        {
            if (dashRenderers[i]) dashRenderers[i].enabled = visible;
        }
    }

    private void StopDashPresentation()
    {
        if (dashMeshRoutine != null)
        {
            StopCoroutine(dashMeshRoutine);
            dashMeshRoutine = null;
        }

        SetDashMeshVisible(true);
    }

    // 공격 애니메이션을 재생한다. 실제 판정은 Animation Event에서 처리한다.
    private void TryAttack()
    {
        if (IsDead || !CanPerceivePlayer()) return;

        BeginAttackLock();

        if (animator && !string.IsNullOrEmpty(attackTrigger))
        {
            animator.SetTrigger(attackTrigger);
            return;
        }

        ActivateAttackAbility();
        OnAttackHit();
        ClearAttackLock();
    }

    private void BeginAttackLock()
    {
        isAttackLocked = true;
        attackLockGrace = 0.2f;
        attackHitApplied = false;

        if (currentState != EnemyState.Dash)
        {
            StopMovement();
        }
    }

    private void TickAttackLock()
    {
        if (!isAttackLocked) return;

        if (currentState != EnemyState.Dash)
        {
            StopMovement();
        }

        TryFireAttackHitFromAnimation();

        if (attackLockGrace > 0f)
        {
            attackLockGrace = Mathf.Max(attackLockGrace - Time.deltaTime, 0f);
            return;
        }

        if (IsPlayingAttackAnimation()) return;

        ClearAttackLock();
    }

    private void ClearAttackLock()
    {
        isAttackLocked = false;
        attackLockGrace = 0f;
    }

    private bool IsPlayingAttackAnimation()
    {
        if (!animator || !animator.enabled) return false;

        if (IsAttackAnimatorState(animator.GetCurrentAnimatorStateInfo(0))) return true;

        if (animator.IsInTransition(0) && IsAttackAnimatorState(animator.GetNextAnimatorStateInfo(0))) return true;

        return false;
    }

    private bool IsAttackAnimatorState(AnimatorStateInfo stateInfo)
    {
        return stateInfo.IsName("Attack") || stateInfo.IsName("Melee_Attack01");
    }

    // 애니메이션 이벤트가 안 와도 공격 모션 중반에 타격을 넣는다.
    private void TryFireAttackHitFromAnimation()
    {
        if (attackHitApplied) return;
        if (EnemyType != EnemyType.Melee) return;
        if (!IsPlayingAttackAnimation()) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (!IsAttackAnimatorState(stateInfo)) return;
        if (stateInfo.normalizedTime < 0.25f) return;

        OnAttackHit();
    }

    // Animation Event: 등록된 공격 Ability를 시전한다.
    public void OnAttackAbility()
    {
        if (IsDead) return;

        ActivateAttackAbility();
    }

    // 공격 Ability가 있으면 시전한다.
    private void ActivateAttackAbility()
    {
        if (!abilitySystem || !attackAbility) return;

        abilitySystem.TryActivateAbility(attackAbility.AbilityTag);
    }

    // Animation Event: 퍼펙트 회피 판정 구간을 연다.
    public void OnPerfectDodgeWindow()
    {
        if (IsDead) return;
        if (!playerTarget) FindPlayer();

        perfectDodgeWindowTimer = perfectDodgeWindowDuration;
        TryResolvePerfectDodge();
    }

    // 플레이어가 공격 범위 안에서 회피 중이면 퍼펙트 회피로 처리한다.
    public bool CanPerfectDodgeNow(Transform player)
    {
        if (perfectDodgeWindowTimer <= 0f) return false;
        if (!player) return false;

        return IsPlayerInAttackRange(player);
    }

    // 강공격에 맞으면 일정 시간 동안 행동을 멈춘다.
    public void Stun(float duration)
    {
        if (IsDead) return;
        if (duration <= 0f) return;

        stunTimer = Mathf.Max(stunTimer, duration);
        currentState = EnemyState.Idle;
        SetMovementEnabled(false);
        ClearAttackLock();
        StopDashPresentation();
        ApplyStunMaterial();

        if (animator)
        {
            animator.ResetTrigger(attackTrigger);
            animator.ResetTrigger(dashTrigger);
            animator.CrossFadeInFixedTime("Idle", 0.1f);
        }
    }

    // Animation Event: 근거리 공격 판정을 실행한다. 원거리 적은 레이로 발사한다.
    public void OnAttackHit()
    {
        if (IsDead) return;
        if (attackHitApplied) return;

        attackHitApplied = true;

        if (!playerTarget) return;
        if (!CanPerceivePlayer()) return;

        if (EnemyType == EnemyType.Ranged)
        {
            FireRangedAttack();
            return;
        }

        Vector3 hitPosition = attackPoint ? attackPoint.position : transform.position;
        Collider[] players = Physics.OverlapSphere(hitPosition, attackHitRange, playerLayer);

        foreach (Collider playerCollider in players)
        {
            NovaActor player = playerCollider.GetComponentInParent<NovaActor>();

            if (!player) continue;

            if (combat)
            {
                DamageInfo damageInfo = new DamageInfo(Owner, attackDamage)
                {
                    Target = player,
                    HitPoint = hitPosition,
                    CanBePerfectDodged = false
                };

                combat.ApplyDamage(player, damageInfo);
            }
            else if (player.Attributes)
            {
                player.Attributes.TakeDamage(attackDamage);
            }

            break;
        }
    }

    // 플레이어를 향해 원거리 레이 공격을 발사한다.
    private void FireRangedAttack()
    {
        Vector3 origin = attackPoint ? attackPoint.position : transform.position + Vector3.up * 1.2f;
        Vector3 targetPosition = playerTarget.position + Vector3.up * 1f;
        Vector3 direction = targetPosition - origin;

        if (direction.sqrMagnitude <= 0.001f) return;

        direction.Normalize();
        float range = GetCurrentAttackRange();

        SpawnRangedAttackEffect(origin, direction);

        Debug.DrawRay(origin, direction * range, Color.cyan, 1f);

        if (Physics.Raycast(origin, direction, out RaycastHit wallHit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && IsWallCollider(wallHit.collider))
        {
            return;
        }

        if (!Physics.Raycast(origin, direction, out RaycastHit hit, range, playerLayer, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        NovaActor player = hit.collider.GetComponentInParent<NovaActor>();

        if (!player) return;

        if (combat)
        {
            DamageInfo damageInfo = new DamageInfo(Owner, attackDamage)
            {
                Target = player,
                HitPoint = hit.point,
                HitNormal = hit.normal,
                CanBePerfectDodged = false
            };

            combat.ApplyDamage(player, damageInfo);
            return;
        }

        if (player.Attributes) player.Attributes.TakeDamage(attackDamage);
    }

    // 원거리 공격 발사 이펙트를 공격 지점에서 발사 방향으로 생성한다.
    private void SpawnRangedAttackEffect(Vector3 origin, Vector3 direction)
    {
        if (!rangedAttackEffectPrefab) return;

        EffectComponent effect = Owner && Owner.Effects ? Owner.Effects : GetComponent<EffectComponent>();

        if (effect)
        {
            effect.SpawnEffect(rangedAttackEffectPrefab, origin, direction, rangedAttackEffectLifetime);
            return;
        }

        Quaternion rotation = direction.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(direction)
            : Quaternion.identity;

        GameObject instance = Instantiate(rangedAttackEffectPrefab, origin, rotation);

        if (rangedAttackEffectLifetime > 0f)
        {
            Destroy(instance, rangedAttackEffectLifetime);
        }
    }

    // 플레이어에게 피격되면 어그로를 갱신한다.
    public void OnHitByPlayer()
    {
        aggroTimer = aggroDuration;

        if (weakPoint && weakPoint.activeSelf) weakPoint.SetActive(false);
    }

    // 이동을 잠그거나 해제한다.
    public void SetMovementEnabled(bool enabled)
    {
        if (IsDead)
        {
            canMove = false;
            StopMovement();
            return;
        }

        canMove = enabled;

        if (!enabled) StopMovement();
    }

    // 퍼펙트 회피 창이 열린 동안 플레이어 회피를 확인한다.
    private void TickPerfectDodgeWindow()
    {
        if (IsDead)
        {
            perfectDodgeWindowTimer = 0f;
            return;
        }

        if (perfectDodgeWindowTimer <= 0f) return;

        TryResolvePerfectDodge();
        perfectDodgeWindowTimer = Mathf.Max(perfectDodgeWindowTimer - Time.unscaledDeltaTime, 0f);
    }

    // 스턴 시간이 끝나면 다시 움직이게 한다.
    private void TickStun()
    {
        if (stunTimer <= 0f) return;

        if (IsDead)
        {
            stunTimer = 0f;
            RestoreOriginalMaterials();
            return;
        }

        stunTimer = Mathf.Max(stunTimer - Time.unscaledDeltaTime, 0f);

        if (stunTimer > 0f) return;

        RestoreOriginalMaterials();
        SetMovementEnabled(true);
    }

    // 창이 열린 상태에서 플레이어가 범위 안 회피면 퍼펙트 회피를 발동한다.
    private void TryResolvePerfectDodge()
    {
        if (perfectDodgeWindowTimer <= 0f) return;
        if (!playerTarget) return;
        if (!IsPlayerInAttackRange(playerTarget)) return;

        CharacterMovementComponent movement = playerTarget.GetComponent<CharacterMovementComponent>();

        if (!movement || !movement.IsDodging) return;

        PerfectDodgeComponent perfectDodge = playerTarget.GetComponent<PerfectDodgeComponent>();

        if (perfectDodge && perfectDodge.TryActivate(Owner))
        {
            perfectDodgeWindowTimer = 0f;
        }
    }

    // 플레이어가 이 적의 공격 범위 안에 있는지 확인한다.
    private bool IsPlayerInAttackRange(Transform player)
    {
        if (!player) return false;

        float range = Mathf.Max(GetCurrentAttackRange(), attackHitRange);

        return Vector3.Distance(transform.position, player.position) <= range;
    }

    private void UpdateAggroTimer()
    {
        if (aggroTimer <= 0f) return;

        aggroTimer = Mathf.Max(aggroTimer - Time.deltaTime, 0f);
    }

    private void UpdateBattleState()
    {
        if (!playerTarget) return;
        if (!GameManager.Instance) return;

        bool shouldBeInBattle = IsAggro() || Vector3.Distance(transform.position, playerTarget.position) <= detectionRange;

        if (shouldBeInBattle && !isInBattle)
        {
            isInBattle = true;
            GameManager.Instance.EnterBattle();
        }
        else if (!shouldBeInBattle && isInBattle)
        {
            isInBattle = false;
            GameManager.Instance.ExitBattle();
        }
    }

    // 체력이 줄면 어그로를 유지한다.
    private void HandleHealthChanged(float current, float max)
    {
        if (current < lastHealth) OnHitByPlayer();

        lastHealth = current;
    }

    // 보스 체력 비율에 따라 페이즈를 갱신한다.
    private void UpdateBossPhase()
    {
        if (EnemyType != EnemyType.Boss) return;
        if (!definition || !attributes) return;

        float ratio = attributes.MaxHealth > 0f ? attributes.CurrentHealth / attributes.MaxHealth : 0f;
        int phase = 1;

        for (int i = 1; i < definition.PhaseCount; i++)
        {
            if (ratio <= definition.PhaseHealthRatio) phase = i + 1;
        }

        if (phase == currentPhase) return;

        currentPhase = phase;
        GameplayEventBus.Raise(new GameplayEvent(new GameplayTag("Combat.BossPhase"), Owner, Owner, currentPhase));
    }

    private void HandleDeath()
    {
        stunTimer = 0f;
        perfectDodgeWindowTimer = 0f;
        dashTimer = 0f;
        currentState = EnemyState.Idle;
        canMove = false;
        ClearAttackLock();
        RestoreOriginalMaterials();
        StopDashPresentation();
        StopMovement();

        if (animator)
        {
            animator.ResetTrigger(attackTrigger);
            animator.ResetTrigger(dashTrigger);
            animator.enabled = false;
        }

        if (rb)
        {
            RigidbodyVelocity.Set(rb, Vector3.zero);
        }

        if (isInBattle && GameManager.Instance)
        {
            GameManager.Instance.ExitBattle();
        }

        GameplayEventBus.Raise(new GameplayTag("Combat.EnemyDeath"), Owner, Owner);
    }

    private bool CanPerceivePlayer()
    {
        if (IsDead) return false;
        if (!playerTarget) return false;

        return HasLineOfSightToPlayer();
    }

    private bool HasLineOfSightToPlayer()
    {
        Vector3 origin = transform.position + Vector3.up * 1.2f;
        Vector3 target = playerTarget.position + Vector3.up * 1f;
        Vector3 offset = target - origin;
        float distance = offset.magnitude;

        if (distance <= 0.01f) return true;

        Vector3 direction = offset / distance;
        origin += direction * 0.35f;
        distance = Mathf.Max(distance - 0.35f, 0.01f);

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float nearestWall = float.MaxValue;
        float nearestPlayer = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;

            if (!hitCollider) continue;
            if (hitCollider.transform.root == transform.root) continue;

            if (IsPlayerCollider(hitCollider))
            {
                nearestPlayer = Mathf.Min(nearestPlayer, hits[i].distance);
                continue;
            }

            if (IsWallCollider(hitCollider))
            {
                nearestWall = Mathf.Min(nearestWall, hits[i].distance);
            }
        }

        if (nearestWall < nearestPlayer) return false;

        return true;
    }

    private bool IsWallCollider(Collider hitCollider)
    {
        if (!hitCollider) return false;
        if (wallMask.value != 0 && ((1 << hitCollider.gameObject.layer) & wallMask) != 0) return true;

        return hitCollider.CompareTag("Wall");
    }

    private bool IsPlayerCollider(Collider hitCollider)
    {
        if (!hitCollider) return false;
        if (playerTarget && hitCollider.transform.root == playerTarget.root) return true;

        return hitCollider.CompareTag("Player");
    }

    private void CacheOriginalMaterials()
    {
        if (originalMaterials != null) return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        List<Renderer> cachedRenderers = new List<Renderer>(renderers.Length);
        List<Material[]> cachedMaterials = new List<Material[]>(renderers.Length);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (!renderer) continue;
            if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer) continue;

            cachedRenderers.Add(renderer);
            cachedMaterials.Add(renderer.sharedMaterials);
        }

        stunRenderers = cachedRenderers.ToArray();
        originalMaterials = cachedMaterials.ToArray();
    }

    private void ApplyStunMaterial()
    {
        if (stunMaterialApplied) return;

        CacheOriginalMaterials();

        if (!stunMaterial || stunRenderers == null) return;

        for (int i = 0; i < stunRenderers.Length; i++)
        {
            Renderer renderer = stunRenderers[i];

            if (!renderer) continue;

            Material[] swapped = new Material[renderer.sharedMaterials.Length];

            for (int slot = 0; slot < swapped.Length; slot++)
            {
                swapped[slot] = stunMaterial;
            }

            renderer.sharedMaterials = swapped;
        }

        stunMaterialApplied = true;
    }

    private void RestoreOriginalMaterials()
    {
        if (!stunMaterialApplied || stunRenderers == null || originalMaterials == null) return;

        for (int i = 0; i < stunRenderers.Length; i++)
        {
            Renderer renderer = stunRenderers[i];

            if (!renderer) continue;

            renderer.sharedMaterials = originalMaterials[i];
        }

        stunMaterialApplied = false;
    }

    private bool IsAggro()
    {
        return aggroTimer > 0f;
    }

    // 원거리 적은 더 먼 거리에서 공격을 시작한다.
    private float GetCurrentAttackRange()
    {
        if (EnemyType == EnemyType.Ranged)
        {
            return rangedAttackRange > 0f ? rangedAttackRange : attackRange;
        }

        return attackRange;
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject) playerTarget = playerObject.transform;
    }

    // 루트 모션의 위아래 이동만 버리고, 중력은 리지드바디가 처리하게 한다.
    private void OnAnimatorMove()
    {
        if (!animator || !animator.applyRootMotion) return;
        if (IsDead) return;

        Vector3 deltaPosition = animator.deltaPosition;
        deltaPosition.y = 0f;

        if (rb)
        {
            rb.MovePosition(rb.position + deltaPosition);
            rb.MoveRotation(rb.rotation * animator.deltaRotation);
            return;
        }

        transform.position += deltaPosition;
        transform.rotation = animator.deltaRotation * transform.rotation;
    }

    private void StopMovement()
    {
        if (!rb) return;

        Vector3 velocity = RigidbodyVelocity.Get(rb);
        velocity.x = 0f;
        velocity.z = 0f;
        RigidbodyVelocity.Set(rb, velocity);
    }

    private void RotateTo(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.magenta;

        Vector3 hitPosition = attackPoint ? attackPoint.position : transform.position;

        Gizmos.DrawWireSphere(hitPosition, attackHitRange);

        if (dashTriggerRange > 0f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, dashTriggerRange);
        }
    }
}
