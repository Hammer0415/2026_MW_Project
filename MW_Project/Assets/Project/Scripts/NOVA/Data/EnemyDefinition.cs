using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDefinition", menuName = "NOVA/Data/Enemy Definition")]
public class EnemyDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("적 표시 이름")]
    [SerializeField] private string displayName;
    [Tooltip("적 유형. 근거리, 원거리, 탱커, 보스")]
    [SerializeField] private EnemyType enemyType = EnemyType.Melee;

    [Header("Attributes")]
    [Tooltip("최대 체력")]
    [SerializeField] private float maxHealth = 100f;
    [Tooltip("공격력")]
    [SerializeField] private float attackDamage = 10f;
    [Tooltip("방어력. 탱커/보스에서 높게 잡을 수 있다.")]
    [SerializeField] private float defense = 0f;
    [Tooltip("경직 저항. 높을수록 히트스톱/경직을 덜 받는다.")]
    [Min(0f)]
    [SerializeField] private float poise = 0f;

    [Header("AI")]
    [Tooltip("플레이어 감지 거리")]
    [SerializeField] private float detectionRange = 12f;
    [Tooltip("공격 시작 거리")]
    [SerializeField] private float attackRange = 2f;
    [Tooltip("이동 속도")]
    [SerializeField] private float moveSpeed = 3.5f;
    [Tooltip("회전 속도")]
    [SerializeField] private float rotationSpeed = 8f;
    [Tooltip("피격 후 추적을 유지하는 시간")]
    [SerializeField] private float aggroDuration = 5f;
    [Tooltip("공격 딜레이")]
    [SerializeField] private float attackDelay = 1.5f;

    [Header("Combat")]
    [Tooltip("근거리 공격 판정 반경")]
    [SerializeField] private float meleeHitRadius = 1.5f;
    [Tooltip("원거리 공격 사거리")]
    [SerializeField] private float rangedAttackRange = 18f;
    [Tooltip("이 적이 사용할 Ability 목록")]
    [SerializeField] private NovaAbility[] abilities;
    [Tooltip("기본 공격 Ability")]
    [SerializeField] private NovaAbility attackAbility;

    [Header("Boss")]
    [Tooltip("보스 페이즈 수. 일반 적은 1로 둔다.")]
    [Min(1)]
    [SerializeField] private int phaseCount = 1;
    [Tooltip("페이즈가 넘어가는 체력 비율")]
    [Range(0f, 1f)]
    [SerializeField] private float phaseHealthRatio = 0.5f;

    public string DisplayName => displayName;
    public EnemyType EnemyType => enemyType;
    public float MaxHealth => maxHealth;
    public float AttackDamage => attackDamage;
    public float Defense => defense;
    public float Poise => poise;
    public float DetectionRange => detectionRange;
    public float AttackRange => attackRange;
    public float MoveSpeed => moveSpeed;
    public float RotationSpeed => rotationSpeed;
    public float AggroDuration => aggroDuration;
    public float AttackDelay => attackDelay;
    public float MeleeHitRadius => meleeHitRadius;
    public float RangedAttackRange => rangedAttackRange;
    public NovaAbility[] Abilities => abilities;
    public NovaAbility AttackAbility => attackAbility;
    public int PhaseCount => phaseCount;
    public float PhaseHealthRatio => phaseHealthRatio;
}
