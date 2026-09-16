using UnityEngine;

[AddComponentMenu("NOVA/Combat/Weak Point Component")]
public class WeakPointComponent : NovaComponent
{
    [Header("Weak Point")]
    [Tooltip("약점 표시 오브젝트")]
    [SerializeField] private GameObject weakPointObject;
    [Tooltip("약점이 유지되는 시간")]
    [Min(0f)]
    [SerializeField] private float weakPointDuration = 1.5f;
    [Tooltip("약점 발동 시 시간 배율")]
    [Range(0f, 1f)]
    [SerializeField] private float weakPointTimeScale = 0.2f;
    [Tooltip("약점 공격 데미지 배율")]
    [Min(1f)]
    [SerializeField] private float strongAttackDamageMultiplier = 1.5f;
    [Tooltip("약점 공격 후 적을 경직시키는 시간")]
    [Min(0f)]
    [SerializeField] private float strongAttackStunDuration = 0.4f;

    [Header("State")]
    [ReadOnly]
    [Tooltip("현재 약점이 활성화되어 있는지")]
    [SerializeField] private bool isWeakPointActive;
    [ReadOnly]
    [Tooltip("약점 남은 시간")]
    [SerializeField] private float weakPointTimer;

    public bool IsWeakPointActive => isWeakPointActive;
    public float StrongAttackDamageMultiplier => strongAttackDamageMultiplier;
    public float StrongAttackStunDuration => strongAttackStunDuration;

    protected override void Awake()
    {
        base.Awake();

        SetWeakPointVisible(false);
    }

    private void Update()
    {
        if (!isWeakPointActive) return;

        weakPointTimer -= Time.unscaledDeltaTime;

        if (weakPointTimer <= 0f)
        {
            EndWeakPoint();
        }
    }

    // 퍼펙트 회피 등으로 약점을 노출한다.
    public void ActivateWeakPoint()
    {
        if (isWeakPointActive) return;

        isWeakPointActive = true;
        weakPointTimer = weakPointDuration;
        Time.timeScale = weakPointTimeScale;

        SetWeakPointVisible(true);
        GameplayEventBus.Raise(new GameplayTag("Combat.WeakPoint"), Owner, Owner);
    }

    // 약점 강공격을 시도한다. 활성화 중이 아니면 false.
    public bool TryStrongAttack()
    {
        if (!isWeakPointActive) return false;

        EndWeakPoint();

        return true;
    }

    // 약점 상태를 강제로 종료한다.
    public void EndWeakPoint()
    {
        isWeakPointActive = false;
        weakPointTimer = 0f;
        Time.timeScale = 1f;

        SetWeakPointVisible(false);
    }

    // 약점 표시 오브젝트를 켜거나 끈다.
    private void SetWeakPointVisible(bool visible)
    {
        if (weakPointObject) weakPointObject.SetActive(visible);
    }
}
