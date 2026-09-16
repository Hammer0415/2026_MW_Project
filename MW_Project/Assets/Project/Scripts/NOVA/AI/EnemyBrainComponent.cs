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

    public EnemyType EnemyType => definition ? definition.EnemyType : enemyType;
    public bool CanMove => canMove;
    public bool IsWeakPointActive => weakPoint && weakPoint.activeSelf;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        attributes = GetComponent<AttributeComponent>();
        abilitySystem = GetComponent<AbilitySystemComponent>();
        combat = GetComponent<CombatComponent>();

        ApplyDefinition();

        if (weakPoint) weakPoint.SetActive(false);
        if (attributes)
        {
            lastHealth = attributes.CurrentHealth;
            attributes.OnDied += HandleDeath;
            attributes.OnHealthChanged += HandleHealthChanged;
        }

        GrantAttackAbilities();
    }

    private void Start()
    {
        FindPlayer();
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

    // 제자리에서 플레이어를 보고 공격한다.
    private void HandleAttack()
    {
        StopMovement();

        if (!playerTarget) return;

        Vector3 direction = playerTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            RotateTo(direction.normalized);
        }

        if (attackTimer > 0f) return;

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

        float distance = playerTarget ? Vector3.Distance(transform.position, playerTarget.position) : float.MaxValue;

        if (distance <= GetCurrentAttackRange())
        {
            currentState = EnemyState.Attack;
            HandleAttack();
            return;
        }

        currentState = EnemyState.Chase;
    }

    // 근거리 적이 대쉬 거리에 들어왔고 쿨타임이 끝났는지 확인한다.
    private bool CanStartDash()
    {
        if (EnemyType != EnemyType.Melee) return false;
        if (dashTriggerRange <= 0f) return false;
        if (dashCooldownTimer > 0f) return false;
        if (!playerTarget) return false;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance > dashTriggerRange) return false;
        if (distance <= GetCurrentAttackRange()) return false;

        return true;
    }

    // 대쉬를 시작하고 대쉬 애니메이션을 재생한다.
    private void StartDash()
    {
        Vector3 direction = playerTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f) return;

        dashDirection = direction.normalized;
        dashTimer = dashDuration;
        currentState = EnemyState.Dash;

        if (animator && !string.IsNullOrEmpty(dashTrigger))
        {
            animator.SetTrigger(dashTrigger);
        }
    }

    // 공격 애니메이션을 재생한다. 실제 판정은 Animation Event에서 처리한다.
    private void TryAttack()
    {
        if (animator && !string.IsNullOrEmpty(attackTrigger))
        {
            animator.SetTrigger(attackTrigger);
            return;
        }

        ActivateAttackAbility();
        OnAttackHit();
    }

    // Animation Event: 등록된 공격 Ability를 시전한다.
    public void OnAttackAbility()
    {
        ActivateAttackAbility();
    }

    // 공격 Ability가 있으면 시전한다.
    private void ActivateAttackAbility()
    {
        if (!abilitySystem || !attackAbility) return;

        abilitySystem.TryActivateAbility(attackAbility.AbilityTag);
    }

    // Animation Event: 근거리 공격 판정을 실행한다. 원거리 적은 레이로 발사한다.
    public void OnAttackHit()
    {
        if (!playerTarget) return;

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

            CharacterMovementComponent movement = player.GetComponent<CharacterMovementComponent>();

            if (movement && movement.TryConsumePerfectDodge())
            {
                WeakPointComponent playerWeakPoint = player.GetComponent<WeakPointComponent>();

                if (playerWeakPoint) playerWeakPoint.ActivateWeakPoint();
                if (weakPoint) weakPoint.SetActive(true);

                return;
            }

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

        Debug.DrawRay(origin, direction * range, Color.cyan, 1f);

        if (!Physics.Raycast(origin, direction, out RaycastHit hit, range, playerLayer, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        NovaActor player = hit.collider.GetComponentInParent<NovaActor>();

        if (!player) return;

        CharacterMovementComponent movement = player.GetComponent<CharacterMovementComponent>();

        if (movement && movement.TryConsumePerfectDodge())
        {
            WeakPointComponent playerWeakPoint = player.GetComponent<WeakPointComponent>();

            if (playerWeakPoint) playerWeakPoint.ActivateWeakPoint();
            if (weakPoint) weakPoint.SetActive(true);

            return;
        }

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

    // 플레이어에게 피격되면 어그로를 갱신한다.
    public void OnHitByPlayer()
    {
        aggroTimer = aggroDuration;

        if (weakPoint && weakPoint.activeSelf) weakPoint.SetActive(false);
    }

    // 이동을 잠그거나 해제한다.
    public void SetMovementEnabled(bool enabled)
    {
        canMove = enabled;

        if (!enabled) StopMovement();
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
        canMove = false;
        StopMovement();

        if (isInBattle && GameManager.Instance)
        {
            GameManager.Instance.ExitBattle();
        }

        GameplayEventBus.Raise(new GameplayTag("Combat.EnemyDeath"), Owner, Owner);
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
