using UnityEngine;

[DefaultExecutionOrder(-50)]
public class NovaActor : MonoBehaviour
{
    [Header("Actor")]
    [Tooltip("액터 표시 이름. 비어 있으면 GameObject 이름을 사용한다.")]
    [SerializeField] private string displayName;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

    public AttributeComponent Attributes { get; private set; }
    public AbilitySystemComponent AbilitySystem { get; private set; }
    public CombatComponent Combat { get; private set; }
    public EffectComponent Effects { get; private set; }
    public CharacterMovementComponent Movement { get; private set; }

    protected virtual void Awake()
    {
        CacheComponents();
    }

    // 자주 사용하는 컴포넌트를 한 번에 찾아 둔다.
    protected void CacheComponents()
    {
        Attributes = GetComponent<AttributeComponent>();
        AbilitySystem = GetComponent<AbilitySystemComponent>();
        Combat = GetComponent<CombatComponent>();
        Effects = GetComponent<EffectComponent>();
        Movement = GetComponent<CharacterMovementComponent>();
    }
}
