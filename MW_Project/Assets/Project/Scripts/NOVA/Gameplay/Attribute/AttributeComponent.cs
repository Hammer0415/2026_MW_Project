using System;
using UnityEngine;

public class AttributeComponent : NovaComponent
{
    [Header("Health")]
    [Tooltip("최대 체력")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Stamina")]
    [Tooltip("최대 스테미나")]
    [SerializeField] private float maxStamina = 100f;
    [Tooltip("스테미나 회복량(초)")]
    [SerializeField] private float staminaRecoveryRate = 20f;

    private float currentHealth = 0f;
    private float currentStamina = 0f;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;

    public bool IsDead => currentHealth <= 0f;

    // 값 변경 이벤트
    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnStaminaChanged;

    protected override void Awake()
    {
        base.Awake();

        ResetAttributes();
    }

    // 체력 감소
    public void TakeDamage(float damage)
    {
        if (damage <= 0f) return;

        float previousHealth = currentHealth;

        currentHealth = Mathf.Max(currentHealth - damage, 0f);

        if (!Mathf.Approximately(previousHealth, currentHealth))
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    // 체력 회복
    public void Heal(float amount)
    {
        if (amount <= 0f) return;

        float previousHealth = currentHealth;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

        if (!Mathf.Approximately(previousHealth, currentHealth))
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    // 스테미나 감소
    public bool TryConsumeStamina(float amount)
    {
        if (amount <= 0f) return true;
        if (currentStamina < amount) return false;

        float previousStamina = currentStamina;

        currentStamina -= amount;

        if (!Mathf.Approximately(previousStamina, currentStamina))
        {
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        return true;
    }

    // 스테미나 회복
    public void RecoverStamina(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        float previousStamina = currentStamina;

        currentStamina = Mathf.Min(currentStamina + staminaRecoveryRate * deltaTime, maxStamina);

        if (!Mathf.Approximately(previousStamina, currentStamina))
        {
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
    }

    // 모든 상태 리셋
    public void ResetAttributes()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }
}
