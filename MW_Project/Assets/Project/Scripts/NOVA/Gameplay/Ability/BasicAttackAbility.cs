using UnityEngine;

[CreateAssetMenu(fileName = "BasicAttackAbility", menuName = "NOVA/Ability/Basic Attack")]
public class BasicAttackAbility : NovaAbility
{
    [Header("Attack Settings")]
    [Tooltip("기본 공격 데미지")]
    [SerializeField] private float damage = 10f;

    protected override bool CanActivate(NovaActor owner)
    {
        if (!owner) return false;

        CombatComponent combat = owner.GetComponent<CombatComponent>();

        if (!combat) return false;

        return combat.CanAttack;
    }

    protected override void Activate(NovaActor owner)
    {
        CombatComponent combat = owner.GetComponent<CombatComponent>();

        if (!combat) return;

        combat.PerformBasicAttack(damage);
    }
}
