using UnityEngine;

public abstract class NovaAbility : ScriptableObject
{
    [Header("Ability")]
    [Tooltip("Ability를 식별하는 태그")]
    [SerializeField] private GameplayTag abilityTag;
    [Tooltip("Ability 분류. 기본 공격, 스킬, 궁극기, 회피 등을 구분한다.")]
    [SerializeField] private AbilityCategory category = AbilityCategory.None;
    [Tooltip("Ability 쿨타임(초)")]
    [Min(0f)]
    [SerializeField] private float cooldown = 0f;

    [Header("Activation")]
    [Tooltip("공중에서도 사용할 수 있는지")]
    [SerializeField] private bool canActivateInAir = true;
    [Tooltip("회피 중에도 사용할 수 있는지")]
    [SerializeField] private bool canActivateWhileDodging = false;
    [Tooltip("이미 다른 Ability가 활성화된 상태에서도 사용할 수 있는지")]
    [SerializeField] private bool canActivateWhileBusy = true;

    [Header("Animation")]
    [Tooltip("시전 시 재생할 Animator Trigger 이름. 비어 있으면 Ability가 애니메이션을 직접 재생하지 않는다.")]
    [SerializeField] private string animationTrigger;
    [Tooltip("애니메이션 재생 시 Root Motion을 사용할지")]
    [SerializeField] private bool useRootMotion = false;

    private float cooldownRemaining;
    private bool isActive;

    public GameplayTag AbilityTag => abilityTag;
    public AbilityCategory Category => category;
    public float Cooldown => cooldown;
    public bool IsOnCooldown => cooldownRemaining > 0f;
    public bool IsActive => isActive;
    public string AnimationTrigger => animationTrigger;
    public bool UseRootMotion => useRootMotion;

    // 에셋 원본과 런타임 쿨타임이 공유되지 않도록 인스턴스를 복제한다.
    public virtual NovaAbility CreateRuntimeInstance()
    {
        NovaAbility instance = Instantiate(this);
        instance.cooldownRemaining = 0f;
        instance.isActive = false;

        return instance;
    }

    // 사용 가능하면 Ability를 시전하고 쿨타임을 시작한다.
    public bool TryActivate(NovaActor owner, bool ignoreCooldown = false)
    {
        if (!owner) return false;
        if (!ignoreCooldown && IsOnCooldown) return false;
        if (!CanActivate(owner)) return false;

        isActive = true;
        Activate(owner);
        cooldownRemaining = cooldown;

        GameplayEventBus.Raise(abilityTag, owner, owner);

        return true;
    }

    // Ability 종료 처리를 한다. 애니메이션 이벤트에서 호출할 수 있다.
    public void EndAbility(NovaActor owner)
    {
        if (!isActive) return;

        isActive = false;
        OnEnded(owner);
    }

    // 남은 쿨타임을 감소시킨다.
    public void TickCooldown(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        if (cooldownRemaining <= 0f) return;

        cooldownRemaining = Mathf.Max(cooldownRemaining - deltaTime, 0f);
    }

    // 기본 시전 조건을 검사한다. 자식 Ability는 추가 조건을 override한다.
    protected virtual bool CanActivate(NovaActor owner)
    {
        if (!owner) return false;

        AttributeComponent attribute = owner.Attributes ? owner.Attributes : owner.GetComponent<AttributeComponent>();

        if (attribute && attribute.IsDead) return false;

        CharacterMovementComponent movement = owner.Movement ? owner.Movement : owner.GetComponent<CharacterMovementComponent>();

        if (movement)
        {
            if (!canActivateInAir && !movement.IsGrounded) return false;
            if (!canActivateWhileDodging && movement.IsDodging) return false;
        }

        AbilitySystemComponent abilitySystem = owner.AbilitySystem ? owner.AbilitySystem : owner.GetComponent<AbilitySystemComponent>();

        if (abilitySystem && !canActivateWhileBusy && abilitySystem.HasActiveAbility && abilitySystem.ActiveAbility != this)
        {
            return false;
        }

        return true;
    }

    protected abstract void Activate(NovaActor owner);

    protected virtual void OnEnded(NovaActor owner)
    {
    }

    // Ability에 지정된 애니메이션 Trigger를 재생한다.
    protected void PlayAbilityAnimation(NovaActor owner)
    {
        if (!owner) return;
        if (string.IsNullOrEmpty(animationTrigger)) return;

        CharacterAnimationComponent animationComponent = owner.GetComponent<CharacterAnimationComponent>();

        if (animationComponent)
        {
            animationComponent.PlayTrigger(animationTrigger, useRootMotion);
            return;
        }

        CharacterMovementComponent movement = owner.Movement ? owner.Movement : owner.GetComponent<CharacterMovementComponent>();

        if (movement)
        {
            movement.HandleAttackAnimation(animationTrigger);
        }
    }
}
