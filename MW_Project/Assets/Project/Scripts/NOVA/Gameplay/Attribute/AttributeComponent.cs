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

    protected override void Awake()
    {
        base.Awake();

        ResetAttributes();
    }

    // 체력 감소
    public void TakeDamage(float damage)
    {
        if (damage <= 0f) return;

        currentHealth = Mathf.Max(currentHealth -damage, 0f);
    }

    // 체력 회복
    public void Heal(float amount)
    {
        if (amount <= 0f) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    // 스테미나 감소
    public bool TryConsumeStamina(float amount)
    {
        if (amount <= 0f) return true;

        if (currentStamina < amount) return false;

        currentStamina -= amount;
        return true;
    }

    // 스테미나 회복
    public void RecoverStamina(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        currentStamina = Mathf.Min(currentStamina + staminaRecoveryRate * deltaTime, maxStamina);
    }

    // 모든 상태 리셋
    public void ResetAttributes()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;
    }
}
