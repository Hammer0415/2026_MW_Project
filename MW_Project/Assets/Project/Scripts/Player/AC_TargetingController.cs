using UnityEngine;
using UnityEngine.InputSystem;

public class AC_TargetingController : MonoBehaviour
{
    //===== Components =====//
    private Camera mainCamera = null;
    //======================//

    //=====ReferenceVars=====//
    [ReadOnly]
    [SerializeField] private Transform currentTarget = null;

    public Transform CurrentTarget => currentTarget;

    public bool IsTargeting => targetingActive;
    //=======================//

    //=====TargetSettings=====//
    [Header("Target Settings")]
    [SerializeField] private float targetRange = 0.0f;
    [SerializeField] private float targetAngle = 0.0f;
    [Header("Target Layer")]
    [SerializeField] private LayerMask targetLayer;
    //========================//

    //=====UISettings=====//
    [Space(20)]
    [Header("Target UI")]
    [Tooltip("적 바디에 띄워질 타겟팅 UI")]
    [SerializeField] private RectTransform targetMarker;
    //====================//

    //=====CheckingVars=====//
    private bool targetingActive = false;
    //======================//

    private void Awake()
    {
        InitSetup();
    }

    private void Update()
    {
        CheckCurrentTarget();
        UpdateTargetMarker();
    }

    private void ToggleTargeting()
    {
        if (targetingActive)
        {
            targetingActive = false;
            ClearTarget();
            return;
        }

        Transform target = FindBestTarget();

        if (target != null)
        {
            targetingActive = true;
            SetTarget(target);
        }
    }

    private Transform FindBestTarget()
    {
        Collider[] targets = Physics.OverlapSphere(transform.position, targetRange, targetLayer);

        Transform bestTarget = null;
        float bestScore = float.MinValue;

        foreach (Collider targetCollider in targets)
        {
            AC_Enemy enemy = targetCollider.GetComponent<AC_Enemy>();

            if (enemy == null || enemy.isDead) continue;

            Transform target = enemy.transform;
            Vector3 direction = target.position - mainCamera.transform.position;
            float distance = Vector3.Distance(transform.position, target.position);

            if (distance > targetRange) continue;

            float angle = Vector3.Angle(mainCamera.transform.forward, direction);

            if (angle > targetAngle) continue;

            float angleScore = 1.0f - (angle / targetAngle);
            float distanceScore = 1.0f - (distance / targetRange);
            float score = angleScore * 0.7f + distanceScore * 0.3f;

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = target;
            }
        }

        return bestTarget;
    }

    private void SetTarget(Transform target)
    {
        currentTarget = target;
        Debug.Log("Target Lock : " + currentTarget.name);
    }

    private void ClearTarget()
    {
        if (currentTarget != null)
        {
            Debug.Log("Target Unlock : " + currentTarget.name);
        }

        currentTarget = null;
    }

    private void UpdateTargetMarker()
    {
        if (targetMarker == null) return;

        if (currentTarget == null)
        {
            targetMarker.gameObject.SetActive(false);
            return;
        }

        targetMarker.gameObject.SetActive(true);

        AC_Enemy targetEnemy = currentTarget.GetComponent<AC_Enemy>();

        if (targetEnemy == null || targetEnemy.targetPoint == null)
        {
            targetMarker.gameObject.SetActive(false);
            return;
        }

        Vector3 targetPosition = targetEnemy.targetPoint.position;
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(targetPosition);
        targetMarker.position = screenPosition;
    }

    private void CheckCurrentTarget()
    {
        if (!targetingActive) return;

        if (currentTarget == null)
        {
            Transform target = FindBestTarget();

            if (target != null)
            {
                SetTarget(target);
            }
            else
            {
                targetingActive = false;
                ClearTarget();
                Debug.Log("No Target Found");
            }

            return;
        }

        AC_Enemy enemy = currentTarget.GetComponent<AC_Enemy>();

        if (enemy == null || enemy.isDead)
        {
            Debug.Log("Current Target Dead!");

            currentTarget = null;
            return;
        }

        float distance = Vector3.Distance(transform.position, currentTarget.position);

        if (distance > targetRange)
        {
            targetingActive = false;
            ClearTarget();
        }
    }

    private void TryAutoTarget()
    {
        if (!targetingActive) return;

        MG_Game gameManager = MG_Game.Instance;

        if (gameManager == null) return;
        if (!gameManager.isBattle) return;

        Transform target = FindBestTarget();

        if (target != null)
        {
            SetTarget(target);
        }
        else
        {
            targetingActive = false;
            ClearTarget();
        }
    }

    //======================================//
    private void InitSetup()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("Main Camera를 찾을 수 없습니다.", this);
        }
    }
    //======================================//

    //======================================//
    public void OnTargeting(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        ToggleTargeting();
    }
    //======================================//

    private void OnDrawGizmos()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        if (mainCamera == null) return;

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(transform.position, targetRange);
        Vector3 forward = mainCamera.transform.forward;
        forward.y = 0.0f;

        if (forward.sqrMagnitude <= 0.01f) return;

        forward.Normalize();

        Quaternion leftRotation = Quaternion.Euler(0.0f, -targetAngle, 0.0f);
        Quaternion rightRotation = Quaternion.Euler(0.0f, targetAngle, 0.0f);
        Vector3 leftDirection = leftRotation * forward;
        Vector3 rightDirection = rightRotation * forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, leftDirection * targetRange);
        Gizmos.DrawRay(transform.position, rightDirection * targetRange);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, forward * targetRange);
    }
}
