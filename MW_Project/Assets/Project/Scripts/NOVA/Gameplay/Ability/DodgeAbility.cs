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
    
    [Header("Dodge Effect")]
    [Tooltip("대쉬 이펙트")]
    [SerializeField] private GameObject dodgeEffectPrefab;
    [Tooltip("Effect Point Key")]
    [SerializeField] private string dodgeEffectPointKey = "Dodge";
    [Tooltip("대쉬 이펙트 유지 시간")]
    [SerializeField] private float dodgeEffectLifetime = 0.5f;

    protected override bool CanActivate(NovaActor owner)
    {
        if (!owner) return false;

        AttributeComponent attribute = owner.GetComponent<AttributeComponent>();
        CharacterMovementComponent movement = owner.GetComponent<CharacterMovementComponent>();

        if (!attribute || !movement) return false;

        if (!movement.CanMove) return false;
        if (movement.IsDodging) return false;
        if (attribute.CurrentStamina < staminaCost) return false;

        return true;
    }

    protected override void Activate(NovaActor owner)
    {
        AttributeComponent attribute = owner.GetComponent<AttributeComponent>();
        CharacterMovementComponent movement = owner.GetComponent<CharacterMovementComponent>();

        if (!attribute || !movement) return;
        if (!attribute.TryConsumeStamina(staminaCost)) return;

        SpawnDodgeEffect(owner);

        movement.StartDodge(dodgeDistance, dodgeDuration, dodgeInputBufferTime);
    }

    private void SpawnDodgeEffect(NovaActor owner)
    {
        if (!dodgeEffectPrefab) return;

        EffectComponent effect = owner.GetComponent<EffectComponent>();

        if (!effect)
        {
            Debug.LogWarning("EffectComponent를 찾을 수 없습니다.", owner);

            return;
        }

        effect.SpawnEffect(dodgeEffectPrefab, dodgeEffectPointKey, dodgeEffectLifetime);
    }
}
