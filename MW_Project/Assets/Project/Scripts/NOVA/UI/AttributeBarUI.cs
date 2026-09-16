using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("NOVA/UI/Attribute Bar UI")]
public class AttributeBarUI : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("일반 체력 바")]
    [SerializeField] private Slider currentBar;
    [Tooltip("딜레이 체력 바")]
    [SerializeField] private Slider delayedBar;

    [Header("Attribute")]
    [Tooltip("값을 읽어올 AttributeComponent")]
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
        BindAttribute(attribute);
        UpdateInitialValue();
    }

    private void OnDisable()
    {
        UnbindAttribute(attribute);
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

        if (currentBar && delayedBar.value > currentBar.value)
        {
            delayedBar.value = Mathf.MoveTowards(delayedBar.value, currentBar.value, delayedSpeed * Time.deltaTime);
        }
    }

    // 현재 Attribute 값으로 슬라이더를 맞춘다.
    private void UpdateInitialValue()
    {
        if (!currentBar || !attribute) return;

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

    // 체력 변경 이벤트를 슬라이더에 반영한다.
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

    // 스테미나 변경 이벤트를 슬라이더에 반영한다.
    private void HandleStaminaChanged(float current, float max)
    {
        if (!currentBar) return;

        currentBar.maxValue = max;
        currentBar.value = current;
    }

    // 다른 캐릭터의 Attribute로 바인딩을 교체한다.
    public void SetAttribute(AttributeComponent newAttribute)
    {
        UnbindAttribute(attribute);

        attribute = newAttribute;

        if (attribute == null)
        {
            if (currentBar) currentBar.value = 0f;
            if (delayedBar) delayedBar.value = 0f;
            return;
        }

        BindAttribute(attribute);
        UpdateInitialValue();
    }

    public void ClearAttribute()
    {
        SetAttribute(null);
    }

    // Attribute 이벤트를 구독한다.
    private void BindAttribute(AttributeComponent target)
    {
        if (!target) return;

        if (isHealth) target.OnHealthChanged += HandleHealthChanged;
        else target.OnStaminaChanged += HandleStaminaChanged;
    }

    // Attribute 이벤트 구독을 해제한다.
    private void UnbindAttribute(AttributeComponent target)
    {
        if (!target) return;

        if (isHealth) target.OnHealthChanged -= HandleHealthChanged;
        else target.OnStaminaChanged -= HandleStaminaChanged;
    }
}
