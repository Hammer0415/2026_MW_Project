using UnityEngine;

public class EffectComponent : NovaComponent
{
    [System.Serializable]
    private class EffectPoint
    {
        public string key;
        public Transform point;
    }

    [Header("Effect Points")]
    [SerializeField] private EffectPoint[] effectPoints;

    protected override void Awake()
    {
        base.Awake();

        if (effectPoints == null || effectPoints.Length == 0)
        {
            Debug.LogWarning("Effect Point가 설정되지 않았습니다.", this);
        }
    }

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

    // Key + 방향 지정
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

    // Transform + 기본 회전
    public GameObject SpawnEffect(GameObject effectPrefab, Transform point, float lifetime = 0f)
    {
        if (!effectPrefab) return null;
        if (!point) return null;

        Debug.Log("Test3");

        return SpawnEffect(effectPrefab, point, point.rotation, lifetime);
    }

    // Transform + 원하는 회전
    public GameObject SpawnEffect(GameObject effectPrefab, Transform point, Quaternion rotation, float lifetime = 0f)
    {
        if (!effectPrefab) return null;
        if (!point) return null;

        GameObject effect = Instantiate(effectPrefab, point.position, rotation);

        effect.transform.SetParent(point, true);

        if (lifetime > 0f)
        {
            Destroy(effect, lifetime);
        }

        return effect;
    }

    private Transform GetEffectPoint(string pointKey)
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
