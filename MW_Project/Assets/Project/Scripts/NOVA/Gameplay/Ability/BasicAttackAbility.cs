using UnityEngine;

[CreateAssetMenu(fileName = "BasicAttackAbility", menuName = "NOVA/Ability/Basic Attack")]
public class BasicAttackAbility : NovaAbility
{
    [Header("Attack Settings")]
    [Tooltip("기본 공격 데미지")]
    [SerializeField] private float damage = 10f;
    [Header("Attack Effect")]
    [Tooltip("총구에서 생성할 발사 이펙트")]
    [SerializeField] private GameObject muzzleEffectPrefab;
    [Tooltip("발사 이펙트 유지 시간")]
    [SerializeField] private float muzzleEffectLifetime = 0.1f;

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

        SpawnMuzzleEffect(combat);
    }

    private void SpawnMuzzleEffect(CombatComponent combat)
    {
        if (!muzzleEffectPrefab) return;
        if (!combat.CurrentMuzzle) return;

        EffectComponent effect = combat.GetComponent<EffectComponent>();

        if (!effect)
        {
            Debug.LogWarning("EffectComponent를 찾을 수 없습니다.", combat);
            return;
        }

        Debug.Log("Test2");

        effect.SpawnEffect(muzzleEffectPrefab, combat.CurrentMuzzle, muzzleEffectLifetime);
    }
}
