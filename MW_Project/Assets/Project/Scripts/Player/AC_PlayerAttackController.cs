using UnityEngine;
using UnityEngine.InputSystem;

public class AC_PlayerAttackController : MonoBehaviour
{
    //=====Components=====//
    private AC_PlayerController controller;
    //====================//

    //=====AttackValues=====//
    [Header("Attack Settings")]
    [Tooltip("공격 사거리 (탐색 범위)")]
    [SerializeField] private float attackRange = 0.0f;
    [Tooltip("공격 시야각")]
    [SerializeField] private float attackFOV = 120.0f;
    [Tooltip("적 탐색 레이어 (Enemy 레이어 지정)")]
    [SerializeField] private LayerMask enemyLayer;
    [Tooltip("공격 데미지")]
    [SerializeField] private float attackDamage = 0.0f;
    [Tooltip("공격 쿨타임 (초)")]
    [SerializeField] private float attackCooldown = 0.0f;
    [Tooltip("적 자동 타겟팅 시 회전 속도")]
    [SerializeField] private float autoTargetRotateSpeed = 0.0f;
    //======================//

    //=====WeaponSetting=====//
    [Header("Weapon Settings")]
    [Tooltip("총알 발사 위치 (총구 Transform)")]
    [SerializeField] private Transform muzzlePoint;
    //=======================//

    //=====CheckingVars=====//
    private bool isRotatingToTarget = false;
    //======================//

    //=====Others=====//
    private float lastAttackTime = 0.0f;
    private Transform currentTarget = null;
    private Quaternion targetRotation;
    //================//

    private void Start()
    {
        StartInitSetup();
    }

    private void Update()
    {
        currentTarget = FindNearestEnemyInFOV();

        RotateTowardsTarget();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            TryShoot();
        }
    }

    private void TryShoot()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;

        if (currentTarget != null)
        {
            Vector3 targetDir = (currentTarget.position - transform.position).normalized;
            targetDir.y = 0f;

            if (targetDir != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(targetDir);
                isRotatingToTarget = true;
            }

            PerformHit(currentTarget);
        }
        else
        {
            PerformRaycastShoot();
        }

        // 발사 이펙트/사운드 재생 위치
        Debug.Log("총기 발사!");
    }

    private void RotateTowardsTarget()
    {
        if (!isRotatingToTarget) return;

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, autoTargetRotateSpeed * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, targetRotation) < 1.0f)
        {
            transform.rotation = targetRotation;
            isRotatingToTarget = false;
        }
    }

    private Transform FindNearestEnemyInFOV()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);

        Transform nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider enemyCollider in hitEnemies)
        {
            AC_Enemy enemy = enemyCollider.GetComponent<AC_Enemy>();
            if (enemy != null && enemy.isDead) continue;

            Vector3 dirToEnemy = (enemyCollider.transform.position - transform.position);
            dirToEnemy.y = 0f;

            float angleToEnemy = Vector3.Angle(transform.forward, dirToEnemy.normalized);

            if (angleToEnemy <= attackFOV * 0.5f)
            {
                float distance = Vector3.Distance(transform.position, enemyCollider.transform.position);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = enemyCollider.transform;
                }
            }
        }

        return nearest;
    }

    private void PerformHit(Transform target)
    {
        AC_Enemy enemy = target.GetComponent<AC_Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(attackDamage);
            Debug.Log($"[{target.name}]에게 {attackDamage} 데미지 타격!");      // Debug
        }
    }

    private void PerformRaycastShoot()
    {
        Vector3 origin = muzzlePoint != null ? muzzlePoint.position : transform.position + Vector3.up * 1.2f;
        Vector3 direction = transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, attackRange, enemyLayer))
        {
            AC_Enemy enemy = hit.collider.GetComponent<AC_Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(attackDamage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 1. 전체 공격 거리 원 표시
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 2. 시야각(FOV) 부채꼴 영역 표시
        Gizmos.color = Color.yellow;
        Vector3 leftBoundary = DirFromAngle(-attackFOV * 0.5f, false);
        Vector3 rightBoundary = DirFromAngle(attackFOV * 0.5f, false);

        Gizmos.DrawLine(transform.position, transform.position + leftBoundary * attackRange);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary * attackRange);

        // 3. 감지된 타겟 표시
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }

    private Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    //======================================//
    private void StartInitSetup()
    {
        if (controller == null) controller = gameObject.GetComponent<AC_PlayerController>();

        if (controller == null) Debug.LogError("AC_PlayerController 찾을 수 없습니다.");
    }
    //======================================//
}
