using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDefinition", menuName = "NOVA/Data/Character Definition")]
public class CharacterDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("캐릭터 표시 이름")]
    [SerializeField] private string displayName;
    [Tooltip("캐릭터 팀")]
    [SerializeField] private TeamType teamType = TeamType.Player;

    [Header("Attributes")]
    [Tooltip("최대 체력")]
    [SerializeField] private float maxHealth = 100f;
    [Tooltip("최대 스테미나")]
    [SerializeField] private float maxStamina = 100f;
    [Tooltip("스테미나 회복량(초)")]
    [SerializeField] private float staminaRecoveryRate = 20f;
    [Tooltip("기본 공격력")]
    [SerializeField] private float attackPower = 10f;
    [Tooltip("방어력")]
    [SerializeField] private float defense = 0f;

    [Header("Movement")]
    [Tooltip("이동 속도")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("달리기 속도")]
    [SerializeField] private float sprintSpeed = 8f;

    [Header("Abilities")]
    [Tooltip("기본으로 지급할 Ability 목록")]
    [SerializeField] private NovaAbility[] defaultAbilities;

    public string DisplayName => displayName;
    public TeamType TeamType => teamType;
    public float MaxHealth => maxHealth;
    public float MaxStamina => maxStamina;
    public float StaminaRecoveryRate => staminaRecoveryRate;
    public float AttackPower => attackPower;
    public float Defense => defense;
    public float MoveSpeed => moveSpeed;
    public float SprintSpeed => sprintSpeed;
    public NovaAbility[] DefaultAbilities => defaultAbilities;
}
