using System.Collections;
using UnityEngine;

[AddComponentMenu("NOVA/Combat/Combat Component")]
public class CombatComponent : NovaComponent
{
    [Header("Pistol Muzzles")]
    [Tooltip("왼쪽 권총 총구")]
    [SerializeField] private Transform leftMuzzle;
    [Tooltip("오른쪽 권총 총구")]
    [SerializeField] private Transform rightMuzzle;

    [Header("Melee")]
    [Tooltip("근거리 공격 판정 위치. 비어 있으면 캐릭터 위치를 사용한다.")]
    [SerializeField] private Transform meleeAttackPoint;

    [Header("Targeting")]
    [Tooltip("플레이어의 TargetingComponent")]
    [SerializeField] private TargetingComponent targetingComponent;

    [Header("Combat Settings")]
    [Tooltip("공격 판정 레이어")]
    [SerializeField] private LayerMask targetMask;
    [Tooltip("기본 공격 사거리")]
    [SerializeField] private float attackRange = 50f;
    [Tooltip("공격 시 적을 바라볼 수 있는 범위")]
    [SerializeField] private float attackFaceRange = 10f;
    [Tooltip("공격 시 타겟을 바라보는 회전 속도")]
    [SerializeField] private float attackRotationSpeed = 20f;
    [Tooltip("같은 팀에게 데미지를 줄 수 있는지")]
    [SerializeField] private bool allowFriendlyFire = false;

    [Header("Hit Feel")]
    [Tooltip("피격 시 기본 히트 이펙트. Ability에서 따로 지정하면 Ability 값을 우선한다.")]
    [SerializeField] private GameObject defaultHitEffectPrefab;
    [Tooltip("기본 히트 이펙트 유지 시간")]
    [SerializeField] private float defaultHitEffectLifetime = 0.5f;
    [Tooltip("타격 시 히트스톱 시간. 0이면 사용하지 않는다.")]
    [Min(0f)]
    [SerializeField] private float hitStopDuration = 0f;
    [Tooltip("히트스톱 중 적용할 TimeScale")]
    [Range(0f, 1f)]
    [SerializeField] private float hitStopTimeScale = 0.05f;

    [Header("Debug")]
    [Tooltip("공격 레이를 Scene 뷰에 그릴지")]
    [SerializeField] private bool drawDebugRay = true;
    [Tooltip("공격 시 자동 조준 범위를 Scene 뷰에 그릴지")]
    [SerializeField] private bool drawAttackFaceRange = true;

    private Transform currentMuzzle;
    private NovaCharacter attackFaceTarget;
    private CombatFeedbackComponent feedback;
    private EffectComponent effectComponent;
    private AbilitySystemComponent abilitySystem;
    private PerfectDodgeComponent perfectDodge;

    private float pendingAttackDamage;
    private bool pendingAttack;
    private bool isAttackFacing;
    private Transform pendingMuzzle;
    private GameObject pendingHitEffectPrefab;
    private float pendingHitEffectLifetime;
    private int muzzleIndex;
    private int comboIndex;
    private Coroutine skillRoutine;
    private Coroutine hitStopRoutine;

    public Transform CurrentMuzzle => currentMuzzle;
    public int ComboIndex => comboIndex;
    public bool IsAttackFacing => isAttackFacing;

    public bool CanAttack
    {
        get
        {
            if (!leftMuzzle && !rightMuzzle && !meleeAttackPoint) return false;

            return true;
        }
    }

    protected override void Awake()
    {
        base.Awake();

        if (!targetingComponent) targetingComponent = GetComponent<TargetingComponent>();

        feedback = GetComponent<CombatFeedbackComponent>();
        effectComponent = GetComponent<EffectComponent>();
        abilitySystem = GetComponent<AbilitySystemComponent>();
        perfectDodge = GetComponent<PerfectDodgeComponent>();
    }

    private void Update()
    {
        HandleAttackFacing();
    }

    // 기본 원거리 공격을 수행한다. 콤보 중에는 이벤트 타이밍에 바로 발사한다.
    public bool PerformBasicAttack(float damage, GameObject hitEffectPrefab = null, float hitEffectLifetime = 0f)
    {
        if (!CanAttack) return false;
        if (damage <= 0f) return false;

        bool alreadyFacing = isAttackFacing;
        attackFaceTarget = GetAttackTarget();

        if (attackFaceTarget) isAttackFacing = true;

        Transform muzzle = GetNextMuzzle();

        if (!muzzle) return false;

        currentMuzzle = muzzle;

        if (pendingAttack) ExecutePendingAttack();

        pendingHitEffectPrefab = hitEffectPrefab;
        pendingHitEffectLifetime = hitEffectLifetime;

        if (attackFaceTarget && !alreadyFacing)
        {
            pendingAttackDamage = damage;
            pendingMuzzle = muzzle;
            pendingAttack = true;
            return true;
        }

        Fire(muzzle, damage, hitEffectPrefab, hitEffectLifetime);

        return true;
    }

    // 근거리 구형 판정 공격을 수행한다.
    public bool PerformMeleeAttack(float damage, float hitRadius, string attackPointKey, bool canBePerfectDodged, GameObject hitEffectPrefab, float hitEffectLifetime)
    {
        if (damage <= 0f) return false;

        Transform attackPoint = meleeAttackPoint;

        if (!string.IsNullOrEmpty(attackPointKey) && effectComponent)
        {
            Transform keyedPoint = effectComponent.GetEffectPoint(attackPointKey);

            if (keyedPoint) attackPoint = keyedPoint;
        }

        Vector3 origin = attackPoint ? attackPoint.position : Owner.transform.position;
        float radius = hitRadius > 0f ? hitRadius : 1f;

        OverlapAttack(origin, radius, damage, canBePerfectDodged, hitEffectPrefab, hitEffectLifetime);

        return true;
    }

    // 스킬/궁극기처럼 여러 번 들어가는 공격을 수행한다.
    public void PerformSkillAttack(float damage, AttackTraceType traceType, float range, int hitCount, float hitInterval, bool canBePerfectDodged, GameObject hitEffectPrefab, float hitEffectLifetime)
    {
        if (skillRoutine != null)
        {
            StopCoroutine(skillRoutine);
        }

        skillRoutine = StartCoroutine(SkillAttackRoutine(damage, traceType, range, hitCount, hitInterval, canBePerfectDodged, hitEffectPrefab, hitEffectLifetime));
    }

    // 공격 동안 적을 바라보기 시작한다.
    public void StartAttackFacing()
    {
        isAttackFacing = true;
        attackFaceTarget = GetAttackTarget();
    }

    // 공격이 끝나면 적 바라보기를 해제한다.
    public void StopAttackFacing()
    {
        isAttackFacing = false;
        attackFaceTarget = null;
        pendingAttack = false;
        pendingMuzzle = null;
    }

    // 콤보 인덱스를 다음 타격으로 넘긴다.
    public void AdvanceCombo()
    {
        comboIndex++;
    }

    // 콤보 인덱스를 초기화한다.
    public void ResetCombo()
    {
        comboIndex = 0;
    }

    // Animation Event에서 전달된 총구 인덱스를 저장한다. 0 = 오른손, 1 = 왼손.
    public void SetMuzzleIndex(int index)
    {
        muzzleIndex = index;
        GetNextMuzzle();
    }

    // 대상에게 데미지를 적용한다.
    public void ApplyDamage(NovaActor target, float damage)
    {
        ApplyDamage(target, new DamageInfo(Owner, damage));
    }

    // 대상에게 상세 데미지 정보를 적용한다.
    public void ApplyDamage(NovaActor target, DamageInfo damageInfo)
    {
        if (!target) return;
        if (damageInfo.Amount <= 0f) return;
        if (target == Owner) return;

        if (!allowFriendlyFire)
        {
            NovaCharacter ownerCharacter = Owner as NovaCharacter;
            NovaCharacter targetCharacter = target as NovaCharacter;

            if (ownerCharacter && targetCharacter && !ownerCharacter.IsHostileTo(targetCharacter))
            {
                return;
            }
        }

        damageInfo.Instigator = Owner;
        damageInfo.Target = target;

        CharacterMovementComponent targetMovement = target.Movement ? target.Movement : target.GetComponent<CharacterMovementComponent>();

        if (damageInfo.CanBePerfectDodged && targetMovement && targetMovement.TryConsumePerfectDodge())
        {
            WeakPointComponent weakPoint = target.GetComponent<WeakPointComponent>();

            if (weakPoint) weakPoint.ActivateWeakPoint();

            GameplayEventBus.Raise(new GameplayTag("Combat.PerfectDodge"), Owner, target);
            return;
        }

        bool strongAttack = perfectDodge && perfectDodge.HasResolvedResult;

        if (strongAttack)
        {
            damageInfo.Amount *= perfectDodge.ResolvedDamageMultiplier;
        }

        IDamageable damageable = target.GetComponent<IDamageable>();

        if (damageable == null) return;

        damageable.ApplyDamage(damageInfo);

        GameplayEventBus.Raise(new GameplayEvent(new GameplayTag("Combat.Hit"), Owner, target, damageInfo.Amount));
        PlayHitFeedback(damageInfo);

        if (strongAttack)
        {
            perfectDodge.TryApplyStrongAttack(target);
        }
    }

    // 현재 사용할 총구를 선택한다.
    private Transform GetNextMuzzle()
    {
        Transform muzzle = null;

        if (muzzleIndex == 1)
        {
            muzzle = leftMuzzle;

            if (!muzzle) muzzle = rightMuzzle;
        }
        else
        {
            muzzle = rightMuzzle;

            if (!muzzle) muzzle = leftMuzzle;
        }

        currentMuzzle = muzzle;

        return muzzle;
    }

    // 총구에서 레이캐스트 공격을 발사한다.
    private void Fire(Transform muzzle, float damage, GameObject hitEffectPrefab, float hitEffectLifetime)
    {
        Vector3 origin = muzzle.position;
        Vector3 direction = GetFireDirection(muzzle);

        if (direction == Vector3.zero) return;

        if (drawDebugRay)
        {
            Debug.DrawRay(origin, direction * attackRange, Color.red, 1f);
        }

        if (Physics.Raycast(origin, direction, out RaycastHit hit, attackRange, targetMask, QueryTriggerInteraction.Ignore))
        {
            HandleHit(hit, damage, false, hitEffectPrefab, hitEffectLifetime);
        }
    }

    // 구형 범위 안의 적에게 데미지를 적용한다.
    private void OverlapAttack(Vector3 origin, float radius, float damage, bool canBePerfectDodged, GameObject hitEffectPrefab, float hitEffectLifetime)
    {
        Collider[] colliders = Physics.OverlapSphere(origin, radius, targetMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < colliders.Length; i++)
        {
            NovaActor target = colliders[i].GetComponentInParent<NovaActor>();

            if (!target) continue;
            if (target == Owner) continue;

            DamageInfo damageInfo = new DamageInfo(Owner, damage)
            {
                Target = target,
                HitPoint = colliders[i].ClosestPoint(origin),
                HitNormal = (colliders[i].transform.position - origin).normalized,
                CanBePerfectDodged = canBePerfectDodged
            };

            ApplyDamage(target, damageInfo);
            SpawnHitEffect(damageInfo.HitPoint, damageInfo.HitNormal, hitEffectPrefab, hitEffectLifetime);
        }
    }

    // 조준 중이면 타겟을, 아니면 총구 전방을 발사 방향으로 사용한다.
    private Vector3 GetFireDirection(Transform muzzle)
    {
        NovaCharacter priorityTarget = null;

        if (perfectDodge && perfectDodge.ShouldPreferFocusTarget)
        {
            priorityTarget = perfectDodge.FocusTarget;
        }
        else if (targetingComponent && targetingComponent.IsTargeting)
        {
            priorityTarget = targetingComponent.CurrentTarget;
        }

        if (priorityTarget)
        {
            NovaCharacter target = priorityTarget;

            if (target)
            {
                float targetHeight = targetingComponent ? targetingComponent.TargetHeight : 1.2f;
                Vector3 targetPosition = target.transform.position + Vector3.up * targetHeight;
                Vector3 direction = targetPosition - muzzle.position;

                if (direction.sqrMagnitude > 0.001f)
                {
                    return direction.normalized;
                }
            }
        }

        return muzzle.forward;
    }

    // 레이캐스트에 맞은 대상에게 데미지를 적용한다.
    private void HandleHit(RaycastHit hit, float damage, bool canBePerfectDodged, GameObject hitEffectPrefab, float hitEffectLifetime)
    {
        NovaActor target = hit.collider.GetComponentInParent<NovaActor>();

        if (!target) return;
        if (target == Owner) return;

        DamageInfo damageInfo = new DamageInfo(Owner, damage)
        {
            Target = target,
            HitPoint = hit.point,
            HitNormal = hit.normal,
            CanBePerfectDodged = canBePerfectDodged
        };

        ApplyDamage(target, damageInfo);
        SpawnHitEffect(hit.point, hit.normal, hitEffectPrefab, hitEffectLifetime);
    }

    // 공격 중 가까운 적을 향해 회전한다.
    private void HandleAttackFacing()
    {
        if (!isAttackFacing && !pendingAttack) return;

        NovaCharacter target = attackFaceTarget ? attackFaceTarget : GetAttackTarget();

        if (!target)
        {
            if (pendingAttack) ExecutePendingAttack();
            return;
        }

        attackFaceTarget = target;

        Vector3 direction = target.transform.position - Owner.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            if (pendingAttack) CancelPendingAttack();
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        Owner.transform.rotation = Quaternion.Slerp(Owner.transform.rotation, targetRotation, attackRotationSpeed * Time.deltaTime);

        float angle = Quaternion.Angle(Owner.transform.rotation, targetRotation);

        if (angle <= 1f)
        {
            Owner.transform.rotation = targetRotation;

            if (pendingAttack) ExecutePendingAttack();
        }
    }

    // 락온 대상 또는 가장 가까운 적을 공격 대상으로 고른다.
    private NovaCharacter GetAttackTarget()
    {
        if (perfectDodge && perfectDodge.ShouldPreferFocusTarget)
        {
            return perfectDodge.FocusTarget;
        }

        if (targetingComponent && targetingComponent.IsTargeting)
        {
            NovaCharacter currentTarget = targetingComponent.CurrentTarget;

            if (currentTarget && !currentTarget.IsDead)
            {
                return currentTarget;
            }
        }

        Collider[] colliders = Physics.OverlapSphere(Owner.transform.position, attackFaceRange, targetMask, QueryTriggerInteraction.Ignore);

        NovaCharacter closestTarget = null;
        float closestDistanceSqr = float.MaxValue;

        foreach (Collider collider in colliders)
        {
            NovaCharacter character = collider.GetComponentInParent<NovaCharacter>();

            if (!character) continue;
            if (character == Owner) continue;

            float distanceSqr = (character.transform.position - Owner.transform.position).sqrMagnitude;

            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestTarget = character;
            }
        }

        return closestTarget;
    }

    // 몸을 다 돌린 뒤 보류 중인 공격을 발사한다.
    private void ExecutePendingAttack()
    {
        if (!pendingAttack) return;

        pendingAttack = false;

        Transform muzzle = pendingMuzzle ? pendingMuzzle : GetNextMuzzle();
        pendingMuzzle = null;

        if (!muzzle) return;

        Fire(muzzle, pendingAttackDamage, pendingHitEffectPrefab, pendingHitEffectLifetime);
    }

    // 보류 중인 공격을 취소한다.
    private void CancelPendingAttack()
    {
        pendingAttack = false;
        pendingMuzzle = null;
    }

    // 스킬 타격을 횟수만큼 반복한다.
    private IEnumerator SkillAttackRoutine(float damage, AttackTraceType traceType, float range, int hitCount, float hitInterval, bool canBePerfectDodged, GameObject hitEffectPrefab, float hitEffectLifetime)
    {
        int count = Mathf.Max(hitCount, 1);

        for (int i = 0; i < count; i++)
        {
            if (traceType == AttackTraceType.Ray)
            {
                Transform muzzle = GetNextMuzzle();

                if (muzzle)
                {
                    Fire(muzzle, damage, hitEffectPrefab, hitEffectLifetime);
                }
            }
            else
            {
                Vector3 origin = meleeAttackPoint ? meleeAttackPoint.position : Owner.transform.position;

                OverlapAttack(origin, range, damage, canBePerfectDodged, hitEffectPrefab, hitEffectLifetime);
            }

            if (i < count - 1 && hitInterval > 0f)
            {
                yield return new WaitForSeconds(hitInterval);
            }
        }

        skillRoutine = null;

        if (abilitySystem) abilitySystem.EndActiveAbility();
    }

    // 히트 이펙트와 히트스톱을 재생한다.
    private void PlayHitFeedback(DamageInfo damageInfo)
    {
        if (feedback)
        {
            feedback.PlayHit(damageInfo);
        }

        if (hitStopDuration > 0f)
        {
            if (hitStopRoutine != null) StopCoroutine(hitStopRoutine);

            hitStopRoutine = StartCoroutine(HitStopRoutine());
        }
    }

    // 피격 위치에 히트 이펙트를 생성한다.
    private void SpawnHitEffect(Vector3 position, Vector3 direction, GameObject hitEffectPrefab, float hitEffectLifetime)
    {
        GameObject prefab = hitEffectPrefab ? hitEffectPrefab : defaultHitEffectPrefab;
        float lifetime = hitEffectLifetime > 0f ? hitEffectLifetime : defaultHitEffectLifetime;

        if (!prefab) return;

        if (effectComponent)
        {
            effectComponent.SpawnEffect(prefab, position, direction, lifetime);
            return;
        }

        Quaternion rotation = direction.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(direction.normalized)
            : Quaternion.identity;

        GameObject effect = Instantiate(prefab, position, rotation);

        if (lifetime > 0f)
        {
            Destroy(effect, lifetime);
        }
    }

    // 짧은 시간 동안 게임을 멈추듯 연출한다.
    private IEnumerator HitStopRoutine()
    {
        float previousTimeScale = Time.timeScale;

        Time.timeScale = hitStopTimeScale;

        yield return new WaitForSecondsRealtime(hitStopDuration);

        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        hitStopRoutine = null;
    }

    private void OnDrawGizmos()
    {
        if (!drawAttackFaceRange) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackFaceRange);
    }
}
