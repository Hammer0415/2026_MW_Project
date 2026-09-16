using UnityEngine;

[AddComponentMenu("NOVA/Ability/Effect Component")]
public class EffectComponent : NovaComponent
{
    [System.Serializable]
    private class EffectPoint
    {
        [Tooltip("이펙트를 찾을 때 사용하는 Key")]
        public string key;
        [Tooltip("이펙트가 생성될 위치")]
        public Transform point;
    }

    [Header("Effect Points")]
    [Tooltip("캐릭터에 미리 배치한 이펙트 소켓 목록")]
    [SerializeField] private EffectPoint[] effectPoints;

    protected override void Awake()
    {
        base.Awake();

        if (effectPoints == null || effectPoints.Length == 0)
        {
            Debug.LogWarning("Effect Point가 설정되지 않았습니다.", this);
        }
    }

    // Key로 지정된 소켓에 이펙트를 생성한다.
    public GameObject SpawnEffect(GameObject effectPrefab, string pointKey, float lifetime = 0f)
    {
        if (!effectPrefab) return null;

        Transform point = GetEffectPoint(pointKey);

        if (!point)
        {
            Debug.LogWarning($"Effect Point를 찾을 수 없습니다. Key: {pointKey}", this);
            return null;
        }

        return SpawnEffect(effectPrefab, point, point.rotation, lifetime);
    }

    // Key 위치에서 지정한 방향을 바라보도록 이펙트를 생성한다.
    public GameObject SpawnEffect(GameObject effectPrefab, string pointKey, Vector3 direction, float lifetime = 0f)
    {
        if (!effectPrefab) return null;

        Transform point = GetEffectPoint(pointKey);

        if (!point)
        {
            Debug.LogWarning($"Effect Point를 찾을 수 없습니다. Key: {pointKey}", this);
            return null;
        }

        Quaternion rotation = point.rotation;

        if (direction.sqrMagnitude > 0.001f)
        {
            rotation = Quaternion.LookRotation(direction.normalized);
        }

        return SpawnEffect(effectPrefab, point, rotation, lifetime);
    }

    // Transform 위치에 이펙트를 생성한다.
    public GameObject SpawnEffect(GameObject effectPrefab, Transform point, float lifetime = 0f)
    {
        if (!effectPrefab) return null;
        if (!point) return null;

        return SpawnEffect(effectPrefab, point, point.rotation, lifetime);
    }

    // 월드 좌표에 이펙트를 생성한다.
    public GameObject SpawnEffect(GameObject effectPrefab, Vector3 position, Vector3 direction, float lifetime = 0f)
    {
        if (!effectPrefab) return null;

        Quaternion rotation = direction.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(direction.normalized)
            : Quaternion.identity;

        GameObject effect = Instantiate(effectPrefab, position, rotation);

        if (lifetime > 0f)
        {
            Destroy(effect, lifetime);
        }

        return effect;
    }

    // Transform + 원하는 회전으로 이펙트를 생성한다.
    public GameObject SpawnEffect(GameObject effectPrefab, Transform point, Quaternion rotation, float lifetime = 0f)
    {
        if (!effectPrefab) return null;
        if (!point) return null;

        GameObject effect = Instantiate(effectPrefab, point.position, rotation);

        if (lifetime > 0f)
        {
            Destroy(effect, lifetime);
        }

        return effect;
    }

    // Key에 해당하는 이펙트 소켓을 반환한다.
    public Transform GetEffectPoint(string pointKey)
    {
        if (effectPoints == null) return null;

        foreach (EffectPoint effectPoint in effectPoints)
        {
            if (effectPoint == null) continue;
            if (effectPoint.key != pointKey) continue;
            if (!effectPoint.point) continue;

            return effectPoint.point;
        }

        return null;
    }
}
