using UnityEngine;

[CreateAssetMenu(fileName = "DodgeAbility", menuName = "NOVA/Ability/Dodge")]
public class DodgeAbility : NovaAbility
{
    [Header("Dodge Settings")]
    [Tooltip("회피에 필요한 스테미나")]
    [SerializeField] private float staminaCost = 20f;
    [Tooltip("회피 거리")]
    [SerializeField] private float dodgeDistance = 5f;
    [Tooltip("회피 지속 시간")]
    [SerializeField] private float dodgeDuration = 0.25f;
    [Tooltip("회피 입력 보정 시간")]
    [SerializeField] private float dodgeInputBufferTime = 0.1f;

    [Header("I-Frame")]
    [Tooltip("회피 중 무적이 유지되는 시간 비율")]
    [Range(0f, 1f)]
    [SerializeField] private float invincibleRatio = 0.7f;
    [Tooltip("퍼펙트 회피로 판정되는 시간")]
    [Min(0f)]
    [SerializeField] private float perfectDodgeWindow = 0.12f;

    [Header("Dodge Effect")]
    [Tooltip("대쉬 이펙트")]
    [SerializeField] private GameObject dodgeEffectPrefab;
    [Tooltip("Effect Point Key")]
    [SerializeField] private string dodgeEffectPointKey = "Dodge";
    [Tooltip("대쉬 이펙트 유지 시간")]
    [SerializeField] private float dodgeEffectLifetime = 0.5f;

    protected override bool CanActivate(NovaActor owner)
    {
        if (!base.CanActivate(owner)) return false;

        AttributeComponent attribute = owner.Attributes ? owner.Attributes : owner.GetComponent<AttributeComponent>();
        CharacterMovementComponent movement = owner.Movement ? owner.Movement : owner.GetComponent<CharacterMovementComponent>();

        if (!attribute || !movement) return false;
        if (movement.IsDodging) return false;
        if (attribute.CurrentStamina < staminaCost) return false;

        return true;
    }

    protected override void Activate(NovaActor owner)
    {
        AttributeComponent attribute = owner.Attributes ? owner.Attributes : owner.GetComponent<AttributeComponent>();
        CharacterMovementComponent movement = owner.Movement ? owner.Movement : owner.GetComponent<CharacterMovementComponent>();

        if (!attribute || !movement) return;
        if (!attribute.TryConsumeStamina(staminaCost)) return;

        AbilitySystemComponent abilitySystem = owner.AbilitySystem ? owner.AbilitySystem : owner.GetComponent<AbilitySystemComponent>();

        if (abilitySystem) abilitySystem.InterruptForDodge();

        SpawnDodgeEffect(owner);

        float invincibleDuration = dodgeDuration * invincibleRatio;

        movement.StartDodge(dodgeDistance, dodgeDuration, dodgeInputBufferTime, invincibleDuration, perfectDodgeWindow);
        PlayAbilityAnimation(owner);
        EndAbility(owner);
    }

    // 회피 이펙트를 지정된 Effect Point에 생성한다.
    private void SpawnDodgeEffect(NovaActor owner)
    {
        if (!dodgeEffectPrefab) return;

        EffectComponent effect = owner.Effects ? owner.Effects : owner.GetComponent<EffectComponent>();

        if (!effect)
        {
            Debug.LogWarning("EffectComponent를 찾을 수 없습니다.", owner);
            return;
        }

        effect.SpawnEffect(dodgeEffectPrefab, dodgeEffectPointKey, dodgeEffectLifetime);
    }
}
