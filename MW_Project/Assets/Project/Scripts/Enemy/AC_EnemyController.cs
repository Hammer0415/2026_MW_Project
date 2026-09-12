using UnityEngine;
using System.Collections;

public class AC_EnemyController : MonoBehaviour
{
    private enum EnemyState
    {
        Idle,
        Chase,
        Attack
    }

    //=====Components=====//
    private Rigidbody rb = null;
    private CapsuleCollider collider = null;
    private AC_Enemy enemy = null;
    //====================//

    //=====ReferenceVars=====//
    [ReadOnly]
    [SerializeField] private bool canMove = true;

    public bool CanMove => canMove;

    [Header("Combat Aggro")]
    [Tooltip("플레이어에게 피격된 후 추적을 유지하는 시간")]
    [SerializeField] private float aggroDuration = 0.0f;

    [ReadOnly]
    [SerializeField] private float aggroTimer = 0.0f;
    //=======================//

    //=====EnemySettings=====//
    [Header("Enemy State")]
    [Tooltip("적 상태")]
    [SerializeField] private EnemyState currentState = EnemyState.Idle;
    //=====================//

    //=====AISetting=====//
    [Header("AI Settings")]
    [Tooltip("플레이어 감지 거리")]
    [SerializeField] private float detectionRange = 0.0f;
    [Tooltip("공격 범위")]
    [SerializeField] private float attackRange = 0.0f;
    [Tooltip("이동 속도")]
    [SerializeField] private float moveSpeed = 0.0f;
    [Tooltip("공격 쿨타임")]
    [SerializeField] private float attackCooldown = 0.0f;
    [Tooltip("방향 회전 속도")]
    [SerializeField] private float rotationSpeed = 0.0f;
    //===================//

    //=====AttackSettings=====//
    [Header("Attack Settings")]
    [Tooltip("공격 범위")]
    [SerializeField] private float attackDamage = 0.0f;
    [Tooltip("공격 딜레이")]
    [SerializeField] private float attackDelay = 0.0f;
    //========================//

    //=====CheckingVars=====//
    private bool isAttacking = false;
    //======================//

    //=====OtherSettings=====//
    private Transform playerTarget;
    private float attackTimer = 0.0f;
    //=======================//

    private void Awake()
    {
        InitSetup();
    }

    private void Start()
    {
        InitStartSetup();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }
    }

    private void Update()
    {
        if (canMove)
        {
            if (playerTarget == null)
                return;

            UpdateAttackTimer();
            UpdateAggroTimer();
            UpdateState();

            switch (currentState)
            {
                case EnemyState.Idle:
                    HandleIdle();
                    break;

                case EnemyState.Chase:
                    HandleChase();
                    break;

                case EnemyState.Attack:
                    HandleAttack();
                    break;
            }
        }
    }

    private void UpdateState()
    {
        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance <= attackRange)
        {
            currentState = EnemyState.Attack;
            return;
        }
        if (IsAggro())
        {
            currentState = EnemyState.Chase;
            return;
        }
        if (distance <= detectionRange)
        {
            currentState = EnemyState.Chase;
            return;
        }

        currentState = EnemyState.Idle;
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }
        else
        {
            Debug.LogWarning("Player 태그를 가진 오브젝트를 찾을 수 없습니다.");
        }
    }

    private void HandleIdle()
    {
        StopMovement();
    }

    private void HandleChase()
    {
        if (playerTarget == null)
            return;

        Vector3 direction = playerTarget.position - transform.position;
        direction.y = 0.0f;

        if (direction.sqrMagnitude <= 0.01f)
        {
            StopMovement();
            return;
        }

        direction.Normalize();
        Vector3 velocity = direction * moveSpeed;
        velocity.y = GetVerticalVelocity();

        SetVelocity(velocity);
        RotateToPlayer(direction);
    }

    private void HandleAttack()
    {
        StopMovement();
        if (playerTarget == null)
            return;

        Vector3 direction = playerTarget.position - transform.position;
        direction.y = 0.0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            RotateToPlayer(direction.normalized);
        }

        if (attackTimer <= 0f && !isAttacking)
        {
            StartCoroutine(AttackCoroutine());
        }
    }

    private IEnumerator AttackCoroutine()
    {
        isAttacking = true;
        Debug.Log("Enemy Attack Start");

        yield return new WaitForSeconds(attackDelay);

        if (playerTarget != null)
        {
            float distance = Vector3.Distance(
                transform.position,
                playerTarget.position
            );

            if (distance <= attackRange)
            {
                AC_Player player = playerTarget.GetComponent<AC_Player>();

                if (player != null)
                {
                    player.TakeDamage(attackDamage);
                    Debug.Log("Enemy hit Player : " + attackDamage);
                }
            }
        }

        attackTimer = attackCooldown;
        isAttacking = false;
        Debug.Log("Enemy Attack End");
    }

    private void UpdateAggroTimer()
    {
        if (aggroTimer > 0.0f)
        {
            aggroTimer -= Time.deltaTime;

            if (aggroTimer < 0.0f)
                aggroTimer = 0.0f;
        }
    }

    public void OnHitByPlayer()
    {
        aggroTimer = aggroDuration;

        Debug.Log(
            $"[{gameObject.name}] 플레이어 피격 → 어그로 시작 ({aggroDuration}초)"
        );
    }

    //======================================//
    private void UpdateAttackTimer()
    {
        if (attackTimer > 0.0f)
        {
            attackTimer -= Time.deltaTime;

            if (attackTimer < 0.0f)
                attackTimer = 0.0f;
        }
    }

    private bool IsAggro()
    {
        return aggroTimer > 0.0f;
    }

    private void StopMovement()
    {
        if (rb == null)
            return;

        Vector3 velocity = GetCurrentVelocity();

        velocity.x = 0.0f;
        velocity.z = 0.0f;

        SetVelocity(velocity);
    }

    private void RotateToPlayer(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        );
    }

    private float GetVerticalVelocity()
    {
        Vector3 velocity = GetCurrentVelocity();
        return velocity.y;
    }

    private Vector3 GetCurrentVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    private void SetVelocity(Vector3 velocity)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = velocity;
#else
        rb.velocity = velocity;
#endif
    }

    private void InitSetup()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (collider == null) collider = GetComponent<CapsuleCollider>();
        if (enemy == null) enemy = GetComponent<AC_Enemy>();

        if (rb == null) Debug.LogError("Rigidbody를 찾을 수 없습니다.", this);
        if (collider == null) Debug.LogError("CapsuleCollider를 찾을 수 없습니다.", this);
        if (enemy == null) Debug.LogError("AC_Enemy를 찾을 수 없습니다.", this);
    }

    private void InitStartSetup()
    {
        canMove = true;
    }
    //======================================//

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );
    }
}
