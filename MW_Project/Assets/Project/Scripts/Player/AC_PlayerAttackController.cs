using UnityEngine;
using UnityEngine.InputSystem;

public class AC_PlayerAttackController : MonoBehaviour
{
    //=====Components=====//
    private AC_PlayerController controller;
    private AC_TargetingController targeting;
    private MG_Audio audioManager;
    private AC_PlayerWeakPointController weakPoint;
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
    [Tooltip("왼손 권총 총구")]
    [SerializeField] private Transform leftMuzzlePoint;
    [Tooltip("오른손 권총 총구")]
    [SerializeField] private Transform rightMuzzlePoint;
    //=======================//

    //=====VFXSettings=====//
    [Header("VFX Settings")]
    [Tooltip("적 피격 시 생성할 히트 이펙트 프리팹")]
    [SerializeField] private GameObject hitEffectPrefab;
    [Tooltip("적 공격 시 생성할 총기 이펙트 프리팹")]
    [SerializeField] private GameObject weaponEffectPrefab;
    [Tooltip("히트 이펙트 자동 파괴 시간 (초)")]
    [SerializeField] private float hitEffectDestroyTime = 1.0f;
    //======================//

    //=====CheckingVars=====//
    private bool isRotatingToTarget = false;
    private bool isLeftHandNext = true;
    //======================//

    //=====OtherSettings=====//
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
        if (!weakPoint.isWeakPointActive)
        {
            if (targeting != null && targeting.IsTargeting) currentTarget = targeting.CurrentTarget;
            else currentTarget = FindNearestEnemyInFOV();
        }
        else
        {
            if (!targeting.IsTargeting) currentTarget = FindWeakPointEnemy();
        }

        RotateTowardsTarget();
    }

    private void TryShoot(float attackDamage)
    {
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;

        Transform currentMuzzle = isLeftHandNext ? leftMuzzlePoint : rightMuzzlePoint;
        isLeftHandNext = !isLeftHandNext;

        if (audioManager != null) audioManager.PlayPlayerDefaultAttack();

        if (currentTarget != null)
        {
            Vector3 targetDir = (currentTarget.position - transform.position).normalized;
            targetDir.y = 0.0f;

            if (targetDir.sqrMagnitude > 0.01f)
            {
                targetRotation = Quaternion.LookRotation(targetDir);
                isRotatingToTarget = true;
            }

            PerformHit(currentTarget, attackDamage);
        }
        else
        {
            PerformRaycastShoot(currentMuzzle, attackDamage);
        }

        if (weaponEffectPrefab != null && currentMuzzle != null)
        {
            Quaternion weaponEffectRotation = currentTarget == null ? currentMuzzle.rotation : targetRotation;
            GameObject weaponEffectInstance = Instantiate(weaponEffectPrefab, currentMuzzle.position, weaponEffectRotation);
            Destroy(weaponEffectInstance, hitEffectDestroyTime);
        }
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

    private Transform FindWeakPointEnemy()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);

        Transform closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider enemyCollider in enemies)
        {
            AC_Enemy enemy = enemyCollider.GetComponent<AC_Enemy>();

            if (enemy == null) continue;
            if (enemy.isDead) continue;

            AC_EnemyController enemyController = enemy.GetComponent<AC_EnemyController>();

            if (enemyController == null) continue;
            if (!enemyController.IsWeakPointActive) continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }

        return closestEnemy;
    }

    private void PerformHit(Transform target, float attackDamage)
    {
        AC_Enemy enemy = target.GetComponent<AC_Enemy>();

        if (enemy != null)
        {
            enemy.TakeDamage(attackDamage);

            Vector3 hitPosition = enemy.targetPoint != null ? enemy.targetPoint.position : target.position + Vector3.up * 1.0f;
            Vector3 direction = (hitPosition - transform.position).normalized;

            SpawnHitEffect(hitPosition, direction);

            Debug.Log($"[{target.name}]에게 {attackDamage} 데미지 타격!");
        }
    }

    private void PerformRaycastShoot(Transform muzzle, float attackDamage)
    {
        Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.2f;
        Vector3 direction = transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, attackRange, enemyLayer))
        {
            AC_Enemy enemy = hit.collider.GetComponent<AC_Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(attackDamage);
                Debug.Log($"[{enemy.name}]에게 {attackDamage} 데미지 타격!");
            }
        }
    }

    private void SpawnHitEffect(Vector3 spawnPosition, Vector3 surfaceNormal)
    {
        if (hitEffectPrefab == null) return;

        Quaternion hitRotation = Quaternion.LookRotation(surfaceNormal);

        GameObject effectInstance = Instantiate(hitEffectPrefab, spawnPosition, hitRotation);
        Destroy(effectInstance, hitEffectDestroyTime);
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
        if (targeting == null) targeting = GetComponent<AC_TargetingController>();
        if (audioManager == null) audioManager = MG_Audio.Instance;
        if (weakPoint == null) weakPoint = GetComponent<AC_PlayerWeakPointController>();

        if (controller == null) Debug.LogError("AC_PlayerController 찾을 수 없습니다.");
        if (targeting == null) Debug.LogError("AC_TargetingController를 찾을 수 없습니다.");
        if (audioManager == null) Debug.LogError("MG_Audio를 찾을 수 없습니다.");
        if (weakPoint == null) Debug.LogError("AC_PlayerWeakPointController를 찾을 수 없습니다.");
    }
    //======================================//

    //======================================//
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (weakPoint != null && weakPoint.TryStrongAttack())
        {
            TryShoot(attackDamage * weakPoint.strongAttackDamageMultiplier);
            return;
        }

        TryShoot(attackDamage);

        if (controller) controller.animator.SetTrigger("isAttack");
    }
    //======================================//

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Vector3 leftBoundary = DirFromAngle(-attackFOV * 0.5f, false);
        Vector3 rightBoundary = DirFromAngle(attackFOV * 0.5f, false);

        Gizmos.DrawLine(transform.position, transform.position + leftBoundary * attackRange);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary * attackRange);

        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}
