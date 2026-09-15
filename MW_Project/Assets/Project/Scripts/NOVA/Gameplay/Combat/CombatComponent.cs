using UnityEngine;

public class CombatComponent : NovaComponent
{
    [Header("Pistol Muzzles")]
    [Tooltip("왼쪽 권총 총구")]
    [SerializeField] private Transform leftMuzzle;
    [Tooltip("오른쪽 권총 총구")]
    [SerializeField] private Transform rightMuzzle;

    private Transform currentMuzzle;
    public Transform CurrentMuzzle => currentMuzzle;

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

    [Header("Debug")]
    [SerializeField] private bool drawDebugRay = true;
    [SerializeField] private bool drawAttackFaceRange = true;
    
    private bool useLeftMuzzle = true;

    private NovaCharacter attackFaceTarget;

    private float pendingAttackDamage;
    private bool pendingAttack;

    public bool CanAttack
    {
        get
        {
            if (!leftMuzzle && !rightMuzzle) return false;

            return true;
        }
    }

    protected override void Awake()
    {
        base.Awake();

        if (!targetingComponent) targetingComponent = GetComponent<TargetingComponent>();
    }

    private void Update()
    {
        HandleAttackFacing();
    }

    public bool PerformBasicAttack(float damage)
    {
        if (!CanAttack) return false;
        if (damage <= 0f) return false;

        attackFaceTarget = GetAttackTarget();

        if (attackFaceTarget)
        {
            pendingAttackDamage = damage;
            pendingAttack = true;
            return true;
        }

        Transform muzzle = GetNextMuzzle();

        if (!muzzle) return false;

        Fire(muzzle, damage);

        return true;
    }

    private Transform GetNextMuzzle()
    {
        Transform muzzle = null;

        if (useLeftMuzzle)
        {
            muzzle = leftMuzzle;

            if (!muzzle) muzzle = rightMuzzle;
        }
        else
        {
            muzzle = rightMuzzle;

            if (!muzzle) muzzle = leftMuzzle;
        }

        useLeftMuzzle = !useLeftMuzzle;
        currentMuzzle = muzzle;

        return muzzle;
    }

    private void Fire(Transform muzzle, float damage)
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
            HandleHit(hit, damage);
        }
    }

    private Vector3 GetFireDirection(Transform muzzle)
    {
        if (targetingComponent && targetingComponent.IsTargeting)
        {
            NovaCharacter target = targetingComponent.CurrentTarget;

            if (target)
            {
                Vector3 targetPosition = target.transform.position + Vector3.up * targetingComponent.TargetHeight;
                Vector3 direction = targetPosition - muzzle.position;

                if (direction.sqrMagnitude > 0.001f)
                {
                    return direction.normalized;
                }
            }
        }

        return muzzle.forward;
    }

    private void HandleHit(RaycastHit hit, float damage)
    {
        NovaActor target = hit.collider.GetComponentInParent<NovaActor>();

        if (!target) return;
        if (target == Owner) return;

        ApplyDamage(target, damage);
    }

    public void ApplyDamage(NovaActor target, float damage)
    {
        if (!target) return;
        if (damage <= 0f) return;

        AttributeComponent attribute = target.GetComponent<AttributeComponent>();

        if (!attribute) return;

        attribute.TakeDamage(damage);
    }

    private void FaceAttackTarget()
    {
        NovaCharacter target = GetAttackTarget();

        if (!target) return;

        Vector3 direction = target.transform.position - Owner.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        attackFaceTarget = GetAttackTarget();
    }

    private void HandleAttackFacing()
    {
        if (!attackFaceTarget) return;

        Vector3 direction = attackFaceTarget.transform.position - Owner.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            CancelPendingAttack();
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        Owner.transform.rotation = Quaternion.Slerp(Owner.transform.rotation, targetRotation, attackRotationSpeed * Time.deltaTime);

        float angle = Quaternion.Angle(Owner.transform.rotation, targetRotation);

        if (angle <= 1f)
        {
            Owner.transform.rotation = targetRotation;
            ExecutePendingAttack();
        }
    }

    private NovaCharacter GetAttackTarget()
    {
        if (targetingComponent && targetingComponent.IsTargeting)
        {
            NovaCharacter currentTarget = targetingComponent.CurrentTarget;

            if (currentTarget && IsTargetInAttackFaceRange(currentTarget))
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

    private bool IsTargetInAttackFaceRange(NovaCharacter target)
    {
        if (!target) return false;

        float distanceSqr = (target.transform.position - Owner.transform.position).sqrMagnitude;

        return distanceSqr <= attackFaceRange * attackFaceRange;
    }

    private void ExecutePendingAttack()
    {
        if (!pendingAttack) return;

        pendingAttack = false;
        attackFaceTarget = null;

        Transform muzzle = GetNextMuzzle();

        if (!muzzle) return;

        Fire(muzzle, pendingAttackDamage);
    }

    private void CancelPendingAttack()
    {
        attackFaceTarget = null;
        pendingAttack = false;
    }

    private void OnDrawGizmos()
    {
        if (!drawAttackFaceRange) return;

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(transform.position, attackFaceRange);
    }
}
