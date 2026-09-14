using UnityEngine;

public class TargetingComponent : NovaComponent
{
    [Header("Targeting")]
    [Tooltip("타겟을 탐색할 최대 거리")]
    [SerializeField] private float targetingRange = 15f;
    [Tooltip("타겟 유지 범위")]
    [SerializeField] private float targetingHoldRange = 18f;
    [Tooltip("타겟으로 인식할 Layer")]
    [SerializeField] private LayerMask targetLayer;

    [Header("Targeting")]
    [Tooltip("현재 타겟")]
    [SerializeField] private NovaCharacter currentTarget;
    [Tooltip("타겟을 바라볼 때의 높이")]
    [SerializeField] private float targetHeight = 1.2f;

    public NovaCharacter CurrentTarget => currentTarget;
    public bool IsTargeting => currentTarget != null;

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

    public float LockOnFocusRatio => lockOnFocusRatio;
    public float TargetHeight => targetHeight;

    private TargetIndicator currentIndicator = null;

    private Camera mainCamera = null;

    protected override void Awake()
    {
        base.Awake();

        mainCamera = Camera.main;
    }

    private void Update()
    {
        CheckTargetDistance();
        CheckTargetVisibility();
        CheckTargetLineOfSight();
    }

    // 타겟팅 입력
    public void OnTargeting()
    {
        if (IsTargeting)
        {
            ClearTarget();
            return;
        }

        FindTarget();
    }

    // 주변 타겟 탐색
    private void FindTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, targetingRange, targetLayer);

        NovaCharacter closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (Collider collider in colliders)
        {
            NovaCharacter target = collider.GetComponentInParent<NovaCharacter>();

            if (!target) continue;

            if (target == Owner) continue;

            float distance = Vector3.Distance(transform.position, target.transform.position);

            if (distance >= closestDistance) continue;

            closestDistance = distance;
            closestTarget = target;
        }

        if (closestTarget != null)
        {
            SetTarget(closestTarget);
        }
    }

    // 현재 타겟의 거리 확인
    private void CheckTargetDistance()
    {
        if (!IsTargeting) return;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distance > targetingHoldRange)
        {
            ClearTarget();
        }
    }

    // 현재 타겟이 플레이어 시아 안에 들어와 있는지 확인
    private void CheckTargetVisibility()
    {
        if (!IsTargeting) return;
        if (!currentTarget) return;

        if (!IsTargetOnScreen(currentTarget))
        {
            SwitchTarget();
        }
    }

    public void SetTarget(NovaCharacter target)
    {
        if (!target) return;
        if (target == Owner) return;

        if (currentIndicator) currentIndicator.SetVisible(false);

        currentTarget = target;

        currentIndicator = currentTarget.GetComponentInChildren<TargetIndicator>();

        if (currentIndicator) currentIndicator.SetVisible(true);
    }
    
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

    private void SwitchTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, targetingRange, targetLayer);

        NovaCharacter closestTarget = null;
        float closestDistance = float.MaxValue;

        if (!mainCamera) return;

        foreach (Collider collider in colliders)
        {
            NovaCharacter target = collider.GetComponentInParent<NovaCharacter>();

            if (!target) continue;
            if (target == Owner) continue;
            if (target == currentTarget) continue;

            if (!IsTargetOnScreen(target)) continue;

            float distance = Vector3.Distance(transform.position, target.transform.position);

            if (distance >= closestDistance) continue;

            closestDistance = distance;
            closestTarget = target;
        }

        if (closestTarget) SetTarget(closestTarget);
        else ClearTarget();
    }

    private bool IsTargetOnScreen(NovaCharacter target)
    {
        if (!mainCamera) return false;

        Vector3 viewportPosition = mainCamera.WorldToViewportPoint(target.transform.position);

        if (viewportPosition.z <= 0f) return false;

        float min = lockOnScreenMargin;
        float max = 1f - lockOnScreenMargin;

        return viewportPosition.x >= min &&
            viewportPosition.x <= max &&
            viewportPosition.y >= min &&
            viewportPosition.y <= max;
    }

    private void CheckTargetLineOfSight()
    {
        if (!IsTargeting) return;
        if (!currentTarget) return;

        Vector3 origin = mainCamera.transform.position;
        Vector3 targetPosition = currentTarget.transform.position;

        Vector3 direction = targetPosition - origin;
        float distance = direction.magnitude;

        direction.y += targetHeight;

        if (Physics.Raycast(origin, direction.normalized, distance, obstructionLayer))
        {
            ClearTarget();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, targetingRange);
    }
}
