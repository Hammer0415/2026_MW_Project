using UnityEngine;
using UnityEngine.UI;

public class AttributeBarUI : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("일반 체력 바")]
    [SerializeField] private Slider currentBar;
    [Tooltip("딜레이 체력 바")]
    [SerializeField] private Slider delayedBar;

    [Header("Attribute")]
    [SerializeField] private AttributeComponent attribute;

    [Header("Type")]
    [Tooltip("체력바인지(false == 스테미나)")]
    [SerializeField] private bool isHealth = true;

    [Header("Delayed Bar")]
    [Tooltip("딜레이 체력바 값이 주는 속도")]
    [SerializeField] private float delayedSpeed = 50f;
    [Tooltip("딜레이 체력바가 줄기 시작하기까지의 시간")]
    [SerializeField] private float delayedStartDelay = 0.2f;

    private float delayedTimer;

    private void OnEnable()
    {
        if (!attribute) return;

        if (isHealth) attribute.OnHealthChanged += HandleHealthChanged;
        else attribute.OnStaminaChanged += HandleStaminaChanged;

        UpdateInitialValue();
    }

    private void OnDisable()
    {
        if (!attribute) return;

        if (isHealth) attribute.OnHealthChanged -= HandleHealthChanged;
        else attribute.OnStaminaChanged -= HandleStaminaChanged;
    }

    private void Update()
    {
        if (!isHealth) return;
        if (!delayedBar) return;

        if (delayedTimer > 0f)
        {
            delayedTimer -= Time.deltaTime;
            return;
        }

        if (delayedBar.value > currentBar.value)
        {
            delayedBar.value = Mathf.MoveTowards(delayedBar.value, currentBar.value, delayedSpeed * Time.deltaTime);
        }
    }

    private void UpdateInitialValue()
    {
        if (!currentBar) return;

        if (isHealth)
        {
            currentBar.maxValue = attribute.MaxHealth;
            currentBar.value = attribute.CurrentHealth;

            if (delayedBar)
            {
                delayedBar.maxValue = attribute.MaxHealth;
                delayedBar.value = attribute.CurrentHealth;
            }
        }
        else
        {
            currentBar.maxValue = attribute.MaxStamina;
            currentBar.value = attribute.CurrentStamina;
        }
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (!currentBar) return;

        currentBar.maxValue = max;
        currentBar.value = current;

        if (!delayedBar) return;

        delayedBar.maxValue = max;

        if (current < delayedBar.value)
        {
            delayedTimer = delayedStartDelay;
        }
        else
        {
            delayedBar.value = current;
        }
    }

    private void HandleStaminaChanged(float current, float max)
    {
        if (!currentBar) return;

        currentBar.maxValue = max;
        currentBar.value = current;
    }
}
