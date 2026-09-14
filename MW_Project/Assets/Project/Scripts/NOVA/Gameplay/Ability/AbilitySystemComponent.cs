using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AbilitySystemComponent : NovaComponent
{
    [SerializeField] private List<NovaAbility> abilities = new();

    [Header("Tag")]
    [Tooltip("기본 공격 Ability Tag")]
    [SerializeField] private GameplayTag basicAttackTag;

    private Dictionary<GameplayTag, NovaAbility> abilityMap = new();

    protected override void Awake()
    {
        base.Awake();

        InitializeAbilities();
    }

    // 기본 공격 입력
    public void OnBasicAttack(InputAction.CallbackContext context)
    {
        TryActivateAbility(basicAttackTag);
    }

    private void Update()
    {
        TickAbilities(Time.deltaTime);
    }

    // Ability 초기화
    private void InitializeAbilities()
    {
        abilityMap.Clear();

        foreach (NovaAbility ability in abilities)
        {
            if (ability == null) continue;

            GameplayTag abilityTag = ability.AbilityTag;

            if (!abilityTag.IsValid()) continue;

            if (abilityMap.ContainsKey(abilityTag))
            {
                Debug.LogWarning($"Ability가 중복 되었습니다. {abilityTag}", this);

                continue;
            }

            abilityMap.Add(abilityTag, ability);
        }
    }

    // Ability 쿨타임 갱신
    private void TickAbilities(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        foreach (NovaAbility ability in abilities)
        {
            if (!ability) return;

            ability.TickCooldown(deltaTime);
        }
    }

    // Ability 보유 여부 확인
    public bool HasAbility(GameplayTag abilityTag)
    {
        return abilityMap.ContainsKey(abilityTag);
    }

    // Ability 받아오기
    public NovaAbility GetAbility(GameplayTag abilityTag)
    {
        abilityMap.TryGetValue(abilityTag, out NovaAbility ability);

        return ability;    
    }

    // Ability 활성화
    public bool TryActivateAbility(GameplayTag abilityTag)
    {
        NovaAbility ability = GetAbility(abilityTag);

        if (!ability) return false;

        return ability.TryActivate(Owner);
    }
}
