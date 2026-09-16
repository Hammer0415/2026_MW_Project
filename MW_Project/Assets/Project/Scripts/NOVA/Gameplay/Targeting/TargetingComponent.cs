using UnityEngine;

[AddComponentMenu("NOVA/Targeting/Targeting Component")]
public class TargetingComponent : NovaComponent
{
    [Header("Targeting Range")]
    [Tooltip("타겟을 탐색할 최대 거리")]
    [SerializeField] private float targetingRange = 15f;
    [Tooltip("타겟 유지 범위")]
    [SerializeField] private float targetingHoldRange = 18f;
    [Tooltip("타겟으로 인식할 Layer")]
    [SerializeField] private LayerMask targetLayer;

    [Header("Current Target")]
    [Tooltip("현재 타겟")]
    [SerializeField] private NovaCharacter currentTarget;
    [Tooltip("타겟을 바라볼 때의 높이")]
    [SerializeField] private float targetHeight = 1.2f;

    [Header("Lock On")]
    [Tooltip("플레이어와 타겟 사이에서 바라볼 위치")]
    [Range(0f, 1f)]
    [SerializeField] private float lockOnFocusRatio = 0.2f;
    [Tooltip("화면 가장자리에서 제외할 영역")]
    [Range(0f, 0.5f)]
    [SerializeField] private float lockOnScreenMargin = 0.15f;
    [Tooltip("현재 타겟이 화면 밖으로 나갔을 때 새로운 타겟을 탐색하는 시간")]
    [SerializeField] private float targetSwitchDelay = 0.2f;
    [Tooltip("타겟과의 시야를 막는 장애물 Layer")]
    [SerializeField] private LayerMask obstructionLayer;

    public NovaCharacter CurrentTarget => currentTarget;
    public bool IsTargeting => currentTarget != null;
    public float LockOnFocusRatio => lockOnFocusRatio;
    public float TargetHeight => targetHeight;

    private TargetIndicator currentIndicator;
    private Camera mainCamera;
    private float targetLostTimer;

    protected override void Awake()
    {
        base.Awake();

        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (currentTarget && currentTarget.IsDead)
        {
            SwitchTarget();
            return;
        }

        CheckTargetDistance();
        CheckTargetVisibility();
        CheckTargetLineOfSight();
    }

    // 타겟팅 입력. 이미 락온 중이면 해제하고, 아니면 가장 가까운 적을 찾는다.
    public void OnTargeting()
    {
        if (IsTargeting)
        {
            ClearTarget();
            return;
        }

        FindTarget();
    }

    // 주변에서 가장 가까운 적을 찾아 락온한다.
    private void FindTarget()
    {
        NovaCharacter closestTarget = FindClosestTarget(false);

        if (closestTarget != null)
        {
            SetTarget(closestTarget);
        }
    }

    // 현재 타겟이 유지 범위를 벗어나면 락온을 해제한다.
    private void CheckTargetDistance()
    {
        if (!IsTargeting) return;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distance > targetingHoldRange)
        {
            ClearTarget();
        }
    }

    // 타겟이 화면 밖으로 나가면 잠시 기다렸다가 다른 타겟으로 바꾼다.
    private void CheckTargetVisibility()
    {
        if (!IsTargeting || !currentTarget)
        {
            targetLostTimer = 0f;
            return;
        }

        if (IsTargetOnScreen(currentTarget))
        {
            targetLostTimer = 0f;
            return;
        }

        targetLostTimer += Time.deltaTime;

        if (targetLostTimer < targetSwitchDelay) return;

        targetLostTimer = 0f;
        SwitchTarget();
    }

    // 타겟을 지정하고 인디케이터를 켠다.
    public void SetTarget(NovaCharacter target)
    {
        if (!target) return;
        if (target == Owner) return;
        if (target.IsDead) return;

        if (currentIndicator) currentIndicator.SetVisible(false);

        currentTarget = target;
        currentIndicator = currentTarget.GetComponentInChildren<TargetIndicator>();

        if (currentIndicator) currentIndicator.SetVisible(true);
    }

    // 현재 락온을 해제한다.
    public void ClearTarget()
    {
        if (currentIndicator)
        {
            currentIndicator.SetVisible(false);
            currentIndicator = null;
        }

        currentTarget = null;
    }

    public NovaCharacter GetTarget()
    {
        return currentTarget;
    }

    // 화면 안의 다른 타겟으로 바꾸거나, 없으면 락온을 해제한다.
    private void SwitchTarget()
    {
        NovaCharacter closestTarget = FindClosestTarget(true);

        if (closestTarget) SetTarget(closestTarget);
        else ClearTarget();
    }

    // 범위 안에서 가장 가까운 적을 찾는다.
    private NovaCharacter FindClosestTarget(bool requireOnScreen)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, targetingRange, targetLayer);

        NovaCharacter closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (Collider collider in colliders)
        {
            NovaCharacter target = collider.GetComponentInParent<NovaCharacter>();

            if (!target) continue;
            if (target == Owner) continue;
            if (target == currentTarget) continue;
            if (target.IsDead) continue;
            if (requireOnScreen && !IsTargetOnScreen(target)) continue;

            float distance = Vector3.Distance(transform.position, target.transform.position);

            if (distance >= closestDistance) continue;

            closestDistance = distance;
            closestTarget = target;
        }

        return closestTarget;
    }

    // 타겟이 화면 안쪽 마진 영역에 있는지 확인한다.
    private bool IsTargetOnScreen(NovaCharacter target)
    {
        if (!mainCamera) return false;

        Vector3 targetPosition = target.transform.position + Vector3.up * targetHeight;
        Vector3 viewportPosition = mainCamera.WorldToViewportPoint(targetPosition);

        if (viewportPosition.z <= 0f) return false;

        float min = lockOnScreenMargin;
        float max = 1f - lockOnScreenMargin;

        return viewportPosition.x >= min && viewportPosition.x <= max && viewportPosition.y >= min && viewportPosition.y <= max;
    }

    // 카메라와 타겟 사이에 장애물이 있으면 락온을 해제한다.
    private void CheckTargetLineOfSight()
    {
        if (!IsTargeting) return;
        if (!currentTarget) return;
        if (!mainCamera) return;

        Vector3 origin = mainCamera.transform.position;
        Vector3 targetPosition = currentTarget.transform.position + Vector3.up * targetHeight;
        Vector3 direction = targetPosition - origin;
        float distance = direction.magnitude;

        if (Physics.Raycast(origin, direction.normalized, distance, obstructionLayer, QueryTriggerInteraction.Ignore))
        {
            ClearTarget();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, targetingRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, targetingHoldRange);
    }
}
