using UnityEngine;

public class NovaComponent : MonoBehaviour
{
    public NovaActor Owner { get; private set; }

    protected virtual void Awake()
    {
        Owner = GetComponent<NovaActor>();

        if (!Owner)
        {
            Debug.LogError($"{GetType().Name} - NovaActor가 필요합니다.", this);
        }
    }

    // Owner에서 컴포넌트를 찾는다. Owner가 없으면 자신의 GameObject에서 찾는다.
    protected T GetOwnerComponent<T>() where T : Component
    {
        if (Owner) return Owner.GetComponent<T>();

        return GetComponent<T>();
    }
}
