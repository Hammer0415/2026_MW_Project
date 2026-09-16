using System;
using UnityEngine;

[AddComponentMenu("NOVA/Attribute/Attribute Component")]
public class AttributeComponent : NovaComponent, IDamageable
{
    [Header("Health")]
    [Tooltip("최대 체력")]
    [SerializeField] private float maxHealth = 100f;
    [Tooltip("받는 데미지를 깎는 방어력. 0이면 기존과 동일하게 그대로 적용된다.")]
    [Min(0f)]
    [SerializeField] private float defense = 0f;

    [Header("Stamina")]
    [Tooltip("최대 스테미나")]
    [SerializeField] private float maxStamina = 100f;
    [Tooltip("스테미나 회복량(초)")]
    [SerializeField] private float staminaRecoveryRate = 20f;
    [Tooltip("스테미나를 소모한 뒤 회복이 시작되기까지 대기 시간. 0이면 바로 회복한다.")]
    [Min(0f)]
    [SerializeField] private float staminaRecoveryDelay = 0f;

    [Header("Combat")]
    [Tooltip("기본 공격력. Ability에 별도 데미지가 있으면 Ability 값을 우선한다.")]
    [Min(0f)]
    [SerializeField] private float attackPower = 0f;

    private float currentHealth;
    private float currentStamina;
    private float staminaRecoveryTimer;
    private float invincibleTimer;
    private bool deathNotified;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public float AttackPower => attackPower;
    public float Defense => defense;
    public bool IsDead => currentHealth <= 0f;
    public bool IsInvincible => invincibleTimer > 0f;

    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnStaminaChanged;
    public event Action OnDied;

    protected override void Awake()
    {
        base.Awake();

        ResetAttributes();
    }

    private void Update()
    {
        TickInvincible(Time.deltaTime);
    }

    // 무적 시간을 감소시킨다.
    private void TickInvincible(float deltaTime)
    {
        if (invincibleTimer <= 0f) return;

        invincibleTimer = Mathf.Max(invincibleTimer - deltaTime, 0f);
    }

    // 단순 수치 데미지를 적용한다.
    public void TakeDamage(float damage)
    {
        ApplyDamage(new DamageInfo(null, damage));
    }

    // 데미지 정보를 받아 체력을 감소시킨다.
    public void ApplyDamage(DamageInfo damageInfo)
    {
        if (IsDead) return;
        if (IsInvincible) return;
        if (damageInfo.Amount <= 0f) return;

        float previousHealth = currentHealth;
        float finalDamage = Mathf.Max(damageInfo.Amount - defense, 0f);

        currentHealth = Mathf.Max(currentHealth - finalDamage, 0f);

        if (!Mathf.Approximately(previousHealth, currentHealth))
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        NotifyDeathIfNeeded();
    }

    // 체력을 회복한다.
    public void Heal(float amount)
    {
        if (IsDead) return;
        if (amount <= 0f) return;

        float previousHealth = currentHealth;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

        if (!Mathf.Approximately(previousHealth, currentHealth))
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    // 스테미나를 소모한다. 부족하면 false를 반환한다.
    public bool TryConsumeStamina(float amount)
    {
        if (amount <= 0f) return true;
        if (currentStamina < amount) return false;

        float previousStamina = currentStamina;

        currentStamina -= amount;
        staminaRecoveryTimer = staminaRecoveryDelay;

        if (!Mathf.Approximately(previousStamina, currentStamina))
        {
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        return true;
    }

    // 스테미나를 회복한다. 회복 대기 중이면 대기 시간만 줄인다.
    public void RecoverStamina(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        if (currentStamina >= maxStamina) return;

        if (staminaRecoveryTimer > 0f)
        {
            staminaRecoveryTimer = Mathf.Max(staminaRecoveryTimer - deltaTime, 0f);
            return;
        }

        float previousStamina = currentStamina;

        currentStamina = Mathf.Min(currentStamina + staminaRecoveryRate * deltaTime, maxStamina);

        if (!Mathf.Approximately(previousStamina, currentStamina))
        {
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
    }

    // 지정 시간 동안 무적 상태로 만든다.
    public void SetInvincible(float duration)
    {
        if (duration <= 0f) return;

        invincibleTimer = Mathf.Max(invincibleTimer, duration);
    }

    // 무적 상태를 즉시 해제한다.
    public void ClearInvincible()
    {
        invincibleTimer = 0f;
    }

    // NovaEffect 데이터로 체력/스테미나를 변경한다.
    public void ApplyEffect(NovaEffect effect)
    {
        if (effect == null) return;
        if (!effect.EffectTag.IsValid()) return;

        string tag = effect.EffectTag.Tag;

        if (tag.Contains("Health.Damage")) TakeDamage(effect.Value);
        else if (tag.Contains("Health.Heal")) Heal(effect.Value);
        else if (tag.Contains("Stamina.Cost")) TryConsumeStamina(effect.Value);
        else if (tag.Contains("Stamina.Recover")) RecoverStamina(0f);
    }

    // 적 Definition 등에서 체력/방어력을 적용한 뒤 현재 값을 최대치로 맞춘다.
    public void Configure(float newMaxHealth, float newDefense)
    {
        maxHealth = Mathf.Max(newMaxHealth, 1f);
        defense = Mathf.Max(newDefense, 0f);
        ResetAttributes();
    }

    // 체력과 스테미나를 최대치로 되돌린다.
    public void ResetAttributes()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        staminaRecoveryTimer = 0f;
        invincibleTimer = 0f;
        deathNotified = false;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    // 체력이 0이 된 순간 한 번만 사망 이벤트를 보낸다.
    private void NotifyDeathIfNeeded()
    {
        if (!IsDead) return;
        if (deathNotified) return;

        deathNotified = true;
        OnDied?.Invoke();

        GameplayEventBus.Raise(new GameplayTag("Combat.Death"), Owner, Owner);
    }
}
