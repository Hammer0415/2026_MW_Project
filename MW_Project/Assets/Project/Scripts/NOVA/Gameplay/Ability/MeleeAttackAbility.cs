using UnityEngine;

[CreateAssetMenu(fileName = "MeleeAttackAbility", menuName = "NOVA/Ability/Melee Attack")]
public class MeleeAttackAbility : NovaAbility
{
    [Header("Attack Settings")]
    [Tooltip("근거리 공격 데미지")]
    [SerializeField] private float damage = 15f;
    [Tooltip("공격 판정 반경")]
    [SerializeField] private float hitRadius = 1.5f;
    [Tooltip("공격 판정 위치 Key. 비어 있으면 캐릭터 위치를 사용한다.")]
    [SerializeField] private string attackPointKey = "Attack";
    [Tooltip("퍼펙트 회피로 흘릴 수 있는 공격인지")]
    [SerializeField] private bool canBePerfectDodged = true;

    [Header("Attack Effect")]
    [Tooltip("타격 이펙트")]
    [SerializeField] private GameObject hitEffectPrefab;
    [Tooltip("타격 이펙트 유지 시간")]
    [SerializeField] private float hitEffectLifetime = 0.5f;

    protected override bool CanActivate(NovaActor owner)
    {
        if (!base.CanActivate(owner)) return false;

        return owner.GetComponent<CombatComponent>();
    }

    protected override void Activate(NovaActor owner)
    {
        CombatComponent combat = owner.Combat ? owner.Combat : owner.GetComponent<CombatComponent>();

        if (!combat) return;

        PlayAbilityAnimation(owner);
        combat.PerformMeleeAttack(damage, hitRadius, attackPointKey, canBePerfectDodged, hitEffectPrefab, hitEffectLifetime);
    }
}
