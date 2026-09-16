using UnityEngine;

[CreateAssetMenu(fileName = "SkillAbility", menuName = "NOVA/Ability/Skill")]
public class SkillAbility : NovaAbility
{
    [Header("Skill Settings")]
    [Tooltip("스킬 데미지")]
    [SerializeField] private float damage = 30f;
    [Tooltip("스킬 판정 방식")]
    [SerializeField] private AttackTraceType traceType = AttackTraceType.Sphere;
    [Tooltip("스킬 사거리 또는 판정 반경")]
    [SerializeField] private float range = 8f;
    [Tooltip("연속 타격 횟수")]
    [Min(1)]
    [SerializeField] private int hitCount = 1;
    [Tooltip("연속 타격 간격")]
    [Min(0f)]
    [SerializeField] private float hitInterval = 0.1f;
    [Tooltip("퍼펙트 회피로 흘릴 수 있는 공격인지")]
    [SerializeField] private bool canBePerfectDodged = false;

    [Header("Skill Effect")]
    [Tooltip("스킬 시전 이펙트")]
    [SerializeField] private GameObject castEffectPrefab;
    [Tooltip("이펙트를 붙일 Effect Point Key")]
    [SerializeField] private string effectPointKey = "Skill";
    [Tooltip("시전 이펙트 유지 시간")]
    [SerializeField] private float castEffectLifetime = 1f;
    [Tooltip("적 피격 이펙트")]
    [SerializeField] private GameObject hitEffectPrefab;
    [Tooltip("피격 이펙트 유지 시간")]
    [SerializeField] private float hitEffectLifetime = 0.5f;

    protected override bool CanActivate(NovaActor owner)
    {
        if (!base.CanActivate(owner)) return false;

        return owner.GetComponent<CombatComponent>();
    }

    protected override void Activate(NovaActor owner)
    {
        CombatComponent combat = owner.Combat ? owner.Combat : owner.GetComponent<CombatComponent>();
        EffectComponent effect = owner.Effects ? owner.Effects : owner.GetComponent<EffectComponent>();

        if (!combat) return;

        PlayAbilityAnimation(owner);

        if (effect && castEffectPrefab)
        {
            effect.SpawnEffect(castEffectPrefab, effectPointKey, castEffectLifetime);
        }

        combat.PerformSkillAttack(damage, traceType, range, hitCount, hitInterval, canBePerfectDodged, hitEffectPrefab, hitEffectLifetime);
    }
}
