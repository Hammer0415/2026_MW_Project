using UnityEngine;

public class NovaComponent : MonoBehaviour
{
    protected NovaActor Owner { get; private set; }

    protected virtual void Awake()
    {
        Owner = gameObject.GetComponent<NovaActor>();

        if (!Owner)
        {
            Debug.LogError($"{GetType().Name} - NovaActor가 필요합니다.", this);
        }
    }
}
