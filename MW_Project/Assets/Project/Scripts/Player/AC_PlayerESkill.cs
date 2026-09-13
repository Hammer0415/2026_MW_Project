using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class AC_PlayerESkill : MonoBehaviour
{
    //=====Components=====//
    private AC_PlayerWeakPointController weakPoint = null;
    private AC_TargetingController targeting = null;
    private AC_PlayerAttackController attackController = null;
    //====================//

    //=====SkillSettings=====//
    [Header("Skill Settings")]
    [Tooltip("스킬 최대 게이지")]
    [SerializeField] private float maxSkillGauge = 0.0f;
    [Tooltip("일반 스킬 공격력")]
    [SerializeField] private float skillDamage = 0.0f;
    [Tooltip("약점 스킬 공격력 배율")]
    [SerializeField] private float weakPointDamageMultiplier = 0.0f;

    [Header("Skill Attack Settings")]
    [Tooltip("스킬 발사 횟수")]
    [SerializeField] private int skillShotCount = 3;
    [Tooltip("스킬 발사 간격")]
    [SerializeField] private float skillShotInterval = 0.0f;
    [Tooltip("스킬 공격 사거리")]
    [SerializeField] private float skillAttackRange = 0.0f;
    [Tooltip("스킬 타격 대상 레이어")]
    [SerializeField] private LayerMask enemyLayer;
    [Tooltip("왼손 총구")]
    [SerializeField] private Transform leftMuzzlePoint = null;
    [Tooltip("오른손 총구")]
    [SerializeField] private Transform rightMuzzlePoint = null;
    //========================//

    //=====ReferenceVars=====//
    [ReadOnly]
    [Tooltip("현재 스킬 게이지")]
    [SerializeField] private float currentSkillGauge = 0.0f;
    [ReadOnly]
    [Tooltip("스킬 사용 가능 여부")]
    [SerializeField] private bool isSkillReady = false;

    public float SkillGauge => maxSkillGauge > 0.0f ? currentSkillGauge / maxSkillGauge : 0.0f;

    public bool IsSkillReady => isSkillReady;
    //=======================//

    //=====CheckingVars=====//
    private bool isUsingSkill = false;
    private bool isLeftHandNext = true;
    //======================//

    private void Start()
    {
        InitStartSetup();
    }

    private void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            AddSkillGauge(20.0f);
            if (currentSkillGauge < maxSkillGauge)
            {
                Debug.Log(currentSkillGauge);   
            }
        }
    }

    public void AddSkillGauge(float amount)
    {
        if (amount <= 0.0f) return;
        if (isSkillReady) return;

        currentSkillGauge += amount;

        currentSkillGauge = Mathf.Clamp(currentSkillGauge, 0.0f, maxSkillGauge);

        if (currentSkillGauge >= maxSkillGauge)
        {
            currentSkillGauge = maxSkillGauge;
            isSkillReady = true;

            Debug.Log("Skill Ready!");
        }
    }

    private void TryUseSkill()
    {
        if (!isSkillReady)
        {
            Debug.Log("Skill Not Ready");
            return;
        }

        ExecuteSkill();
    }

    private void ExecuteSkill()
    {
        bool isWeakPointAttack = weakPoint != null && weakPoint.isWeakPointActive;

        if (isWeakPointAttack)
        {
            ExecuteWeakPointSkill();
        }
        else
        {
            ExecuteNormalSkill();
        }

        currentSkillGauge = 0.0f;
        isSkillReady = false;
    }

    private void ExecuteNormalSkill()
    {
        if (isUsingSkill) return;

        Debug.Log("=====Normal SKILL=====");

        float damage = skillDamage;

        StartCoroutine(VoidBurstCoroutine(damage));
    }

    private void ExecuteWeakPointSkill()
    {
        Debug.Log("=====Weak Point Skill=====");
        float damage = skillDamage * weakPointDamageMultiplier;

        Debug.Log($"Weak Point Skill Damage: {damage}");

        // 이펙트, 타격판정, 사운드 등
    }

    private void FireSkillShot(float damage, int shotIndex)
    {
        Transform currentMuzzle = isLeftHandNext ? leftMuzzlePoint : rightMuzzlePoint;
        isLeftHandNext = !isLeftHandNext;

        Debug.Log($"VOID BURST Shot {shotIndex + 1} / " + $"Damage : {damage} / " + $"Muzzle : {(currentMuzzle != null ? currentMuzzle.name : "NULL")}");

        Transform target = GetSkillTarget();

        if (target != null)
        {
            PerformSkillHit(target, damage);
        }
        else
        {
            PerformSkillRaycast(currentMuzzle, damage);
        }

        Vector3 weaponEffectRotation = currentMuzzle.eulerAngles;
        
        attackController.SpawnHitEffect(currentMuzzle.position, weaponEffectRotation);
    }

    private Transform GetSkillTarget()
    {
        if (targeting != null && targeting.IsTargeting && targeting.CurrentTarget != null)
        {
            return targeting.CurrentTarget;
        }

        return FindNearestSkillTarget();
    }

    private Transform FindNearestSkillTarget()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, skillAttackRange, enemyLayer);

        Transform closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider enemyCollider in enemies)
        {
            AC_Enemy enemy = enemyCollider.GetComponentInParent<AC_Enemy>();

            if (enemy == null) continue;

            if (enemy.isDead) continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }

        return closestEnemy;
    }

    private void PerformSkillHit(Transform target, float damage)
    {
        AC_Enemy enemy = target.GetComponentInParent<AC_Enemy>();

        if (enemy == null) return;

        // 여기서 AC_Enemy의 실제 데미지 함수 호출

        Debug.Log($"VOID BURST HIT : {enemy.name} / Damage : {damage}");
    }

    private void PerformSkillRaycast(Transform muzzle, float damage)
    {
        if (muzzle == null) return;

        Ray ray = new Ray(muzzle.position, muzzle.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, skillAttackRange, enemyLayer, QueryTriggerInteraction.Ignore))
        {
            AC_Enemy enemy = hit.collider.GetComponentInParent<AC_Enemy>();

            if (enemy != null && !enemy.isDead)
            {
                // 실제 데미지 함수 호출

                Debug.Log($"VOID BURST RAY HIT : " + $"{enemy.name} / Damage : {damage}");
            }
        }
    }

    private IEnumerator VoidBurstCoroutine(float damage)
    {
        isUsingSkill = true;

        for (int i = 0; i < skillShotCount; i++)
        {
            FireSkillShot(damage, i);

            yield return new WaitForSeconds(skillShotInterval);
        }

        isUsingSkill = false;
        Debug.Log("=====Skill End=====");
    }

    //======================================//
    private void InitStartSetup()
    {
        if (weakPoint == null) weakPoint = GetComponentInParent<AC_PlayerWeakPointController>();
        if (attackController == null) attackController = GetComponentInParent<AC_PlayerAttackController>();

        if (weakPoint == null) Debug.Log("AC_PlayerWeakPointController를 찾을 수 없습니다.");
        if (attackController == null) Debug.Log("AC_PlayerAttackController를 찾을 수 없습니다.");

        currentSkillGauge = 0.0f;
        isSkillReady = false;
        isUsingSkill = false;
        isLeftHandNext = true;
    }
    //======================================//

    public void OnESkill(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        TryUseSkill();
    }
}
