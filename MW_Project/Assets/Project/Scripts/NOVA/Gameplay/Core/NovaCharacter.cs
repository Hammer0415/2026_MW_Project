using UnityEngine;

public class NovaCharacter : NovaActor
{
    [Header("Team")]
    [Tooltip("이 캐릭터가 속한 팀. Neutral이면 Player/Enemy 태그로 자동 지정한다.")]
    [SerializeField] private TeamType teamType = TeamType.Neutral;

    public TeamType TeamType => teamType;
    public bool IsPlayer => teamType == TeamType.Player;
    public bool IsEnemy => teamType == TeamType.Enemy;
    public bool IsDead => Attributes && Attributes.IsDead;

    protected override void Awake()
    {
        base.Awake();

        ResolveTeamFromTag();

        if (Attributes) Attributes.OnDied += HandleDeath;
    }

    private void OnDestroy()
    {
        if (Attributes) Attributes.OnDied -= HandleDeath;
    }

    // 팀이 Neutral이면 오브젝트 태그로 플레이어/적을 구분한다.
    private void ResolveTeamFromTag()
    {
        if (teamType != TeamType.Neutral) return;

        if (gameObject.CompareTag("Player")) teamType = TeamType.Player;
        else if (gameObject.CompareTag("Enemy")) teamType = TeamType.Enemy;
    }

    // 플레이어가 죽으면 게임 상태에 반영한다.
    private void HandleDeath()
    {
        if (!IsPlayer) return;
        if (!GameManager.Instance) return;

        GameManager.Instance.SetPlayerDead();
    }

    // 다른 액터가 적대 대상인지 확인한다.
    public bool IsHostileTo(NovaActor other)
    {
        if (!other) return false;
        if (other == this) return false;

        NovaCharacter otherCharacter = other as NovaCharacter;

        if (!otherCharacter) return true;
        if (teamType == TeamType.Neutral || otherCharacter.teamType == TeamType.Neutral) return true;

        return teamType != otherCharacter.teamType;
    }
}
