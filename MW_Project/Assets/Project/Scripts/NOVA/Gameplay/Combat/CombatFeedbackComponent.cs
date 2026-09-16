using UnityEngine;

[AddComponentMenu("NOVA/Combat/Combat Feedback Component")]
public class CombatFeedbackComponent : NovaComponent
{
    [Header("Hit Effect")]
    [Tooltip("피격 위치에 생성할 기본 히트 이펙트")]
    [SerializeField] private GameObject hitEffectPrefab;
    [Tooltip("히트 이펙트 유지 시간")]
    [SerializeField] private float hitEffectLifetime = 0.5f;

    [Header("Hit Stop")]
    [Tooltip("타격 시 히트스톱을 사용할지")]
    [SerializeField] private bool useHitStop = false;
    [Tooltip("히트스톱 시간")]
    [Min(0f)]
    [SerializeField] private float hitStopDuration = 0.05f;
    [Tooltip("히트스톱 중 TimeScale")]
    [Range(0f, 1f)]
    [SerializeField] private float hitStopTimeScale = 0.08f;

    private EffectComponent effectComponent;

    protected override void Awake()
    {
        base.Awake();

        effectComponent = GetComponent<EffectComponent>();
    }

    // 타격 순간의 이펙트와 히트스톱을 재생한다.
    public void PlayHit(DamageInfo damageInfo)
    {
        SpawnHitEffect(damageInfo.HitPoint, damageInfo.HitNormal);
    }

    // 피격 위치에 히트 이펙트를 생성한다.
    private void SpawnHitEffect(Vector3 position, Vector3 direction)
    {
        if (!hitEffectPrefab) return;

        if (effectComponent)
        {
            effectComponent.SpawnEffect(hitEffectPrefab, position, direction, hitEffectLifetime);
            return;
        }

        Quaternion rotation = direction.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(direction.normalized)
            : Quaternion.identity;

        GameObject effect = Instantiate(hitEffectPrefab, position, rotation);

        if (hitEffectLifetime > 0f)
        {
            Destroy(effect, hitEffectLifetime);
        }
    }

    public bool UseHitStop => useHitStop;
    public float HitStopDuration => hitStopDuration;
    public float HitStopTimeScale => hitStopTimeScale;
}
