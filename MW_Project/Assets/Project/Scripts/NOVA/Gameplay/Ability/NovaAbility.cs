using UnityEngine;

public abstract class NovaAbility : ScriptableObject
{
    [Header("Ability")]
    [Tooltip("Ability 태그")]
    [SerializeField] private GameplayTag abilityTag;
    [Tooltip("Ability 쿨타임")]
    [SerializeField] private float cooldown = 0f;

    private float cooldownRemaining;

    public GameplayTag AbilityTag => abilityTag;
    public float Cooldown => cooldown;
    public bool IsOnCooldown => cooldownRemaining > 0f;

    // Ability 시전
    public bool TryActivate(NovaActor owner)
    {
        if (!owner) return false;
        if (IsOnCooldown) return false;
        if (!CanActivate(owner)) return false;

        Activate(owner);

        cooldownRemaining = cooldown;

        return true;
    }

    // 쿨타임 시작
    public void TickCooldown(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        if (cooldownRemaining <= 0f) return;

        cooldownRemaining = Mathf.Max(cooldownRemaining - deltaTime, 0f);
    }

    protected virtual bool CanActivate(NovaActor owner)
    {
        return true;
    }

    protected abstract void Activate(NovaActor owner);
}
