using UnityEngine;

[AddComponentMenu("NOVA/Combat/Perfect Dodge Component")]
public class PerfectDodgeComponent : NovaComponent
{
    [Header("Slow Motion")]
    [Tooltip("퍼펙트 회피 후 슬로모션이 유지되는 실제 시간")]
    [Min(0.1f)]
    [SerializeField] private float slowMotionDuration = 5f;
    [Tooltip("퍼펙트 회피 중 TimeScale")]
    [Range(0.01f, 1f)]
    [SerializeField] private float slowMotionTimeScale = 0.1f;

    [Header("Strong Attack")]
    [Tooltip("슬로모션 중 공격 데미지 배율")]
    [Min(1f)]
    [SerializeField] private float strongAttackDamageMultiplier = 2f;
    [Tooltip("강공격을 맞은 적이 경직되는 시간")]
    [Min(0f)]
    [SerializeField] private float strongAttackStunDuration = 2f;
    [Tooltip("퍼펙트 회피 후 강공격이 나가기까지 기다리는 실제 시간")]
    [Min(0f)]
    [SerializeField] private float strongAttackDelay = 0.8f;

    [Header("Camera")]
    [Tooltip("퍼펙트 회피 대상에게 카메라를 맞출 높이")]
    [SerializeField] private float cameraFocusHeight = 1.2f;
    [Tooltip("플레이어와 적 사이에서 어디를 볼지. 0이면 플레이어, 1이면 적")]
    [Range(0f, 1f)]
    [SerializeField] private float cameraFocusRatio = 0.4f;

    [Header("State")]
    [ReadOnly]
    [SerializeField] private bool isActive;
    [ReadOnly]
    [SerializeField] private float windowTimer;

    private CameraArmController cameraArm;
    private CameraController cameraController;
    private AbilitySystemComponent abilitySystem;
    private NovaCharacter focusTarget;
    private bool ownsTimeScale;
    private float defaultFixedDeltaTime;
    private bool wasDodging;
    private float delayTimer;
    private bool queuedStrongAttack;

    public bool IsActive => isActive;
    public bool IsStrongAttackReady => isActive;
    public NovaCharacter FocusTarget => focusTarget;
    public float StrongAttackDamageMultiplier => strongAttackDamageMultiplier;
    public float StrongAttackStunDuration => strongAttackStunDuration;

    protected override void Awake()
    {
        base.Awake();

        abilitySystem = GetComponent<AbilitySystemComponent>();
        defaultFixedDeltaTime = Time.fixedDeltaTime;

        if (Camera.main)
        {
            cameraArm = Camera.main.GetComponentInParent<CameraArmController>();
            cameraController = Camera.main.GetComponent<CameraController>();
        }
    }

    private void OnDestroy()
    {
        if (ownsTimeScale)
        {
            RestoreTimeScale();
        }
    }

    private void Update()
    {
        DetectDodgeStart();
        TickWindow();
        TickStrongAttackDelay();
    }

    // 회피가 시작되면 열려 있는 적 공격 윈도우와 맞춰본다.
    private void DetectDodgeStart()
    {
        CharacterMovementComponent movement = Owner && Owner.Movement ? Owner.Movement : GetComponent<CharacterMovementComponent>();
        bool dodging = movement && movement.IsDodging;

        if (dodging && !wasDodging)
        {
            TryActivateFromOpenWindows();
        }

        wasDodging = dodging;
    }

    // 슬로모션 창이 끝나면 카메라와 시간을 되돌린다.
    private void TickWindow()
    {
        if (!isActive) return;

        windowTimer -= Time.unscaledDeltaTime;

        if (windowTimer <= 0f || (focusTarget && focusTarget.IsDead))
        {
            RestoreCamera();
            EndWindow();
        }
    }

    // 적의 퍼펙트 회피 타이밍에 범위 안에서 회피하면 슬로모션을 시작한다.
    public bool TryActivate(NovaActor attacker)
    {
        if (isActive) return false;
        if (!attacker) return false;

        NovaCharacter attackerCharacter = attacker as NovaCharacter;

        if (!attackerCharacter) attackerCharacter = attacker.GetComponent<NovaCharacter>();
        if (!attackerCharacter) return false;
        if (attackerCharacter.IsDead) return false;

        isActive = true;
        focusTarget = attackerCharacter;
        windowTimer = slowMotionDuration;
        delayTimer = strongAttackDelay;
        queuedStrongAttack = false;

        ApplySlowMotion();
        FocusCamera(focusTarget.transform);
        GameplayEventBus.Raise(new GameplayTag("Combat.PerfectDodge"), Owner, attackerCharacter);

        return true;
    }

    // 딜레이가 끝나기 전에 들어온 공격 입력을 받아 두고, 끝나면 강공격을 나간다.
    public bool TryQueueStrongAttack()
    {
        if (!isActive) return false;

        queuedStrongAttack = true;

        if (delayTimer <= 0f)
        {
            FireQueuedStrongAttack();
        }

        return true;
    }

    // 퍼펙트 회피 직후 대기 시간이 끝나면 받아 둔 강공격을 실행한다.
    private void TickStrongAttackDelay()
    {
        if (!isActive) return;
        if (delayTimer <= 0f) return;

        delayTimer = Mathf.Max(delayTimer - Time.unscaledDeltaTime, 0f);

        if (delayTimer > 0f) return;

        FireQueuedStrongAttack();
    }

    // 받아 둔 강공격을 바로 시작한다. 회피 중이면 회피를 끊는다.
    private void FireQueuedStrongAttack()
    {
        if (!isActive) return;
        if (!queuedStrongAttack) return;

        queuedStrongAttack = false;

        RestoreTimeScale();

        CharacterMovementComponent movement = Owner && Owner.Movement ? Owner.Movement : GetComponent<CharacterMovementComponent>();

        if (movement) movement.CancelDodge();

        if (abilitySystem) abilitySystem.StartBufferedBasicAttack();
    }

    // 슬로모션 중 공격이 맞으면 강공격으로 처리하고 창을 닫는다.
    public bool TryApplyStrongAttack(NovaActor hitTarget)
    {
        if (!isActive) return false;
        if (!hitTarget) return false;

        EnemyBrainComponent brain = hitTarget.GetComponent<EnemyBrainComponent>();

        if (brain) brain.Stun(strongAttackStunDuration);

        RestoreCamera();
        EndWindow();

        return true;
    }

    // 주변에 퍼펙트 회피 창이 열린 적이 있으면 발동한다.
    private void TryActivateFromOpenWindows()
    {
        EnemyBrainComponent[] enemies = FindObjectsByType<EnemyBrainComponent>(FindObjectsSortMode.None);

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyBrainComponent enemy = enemies[i];

            if (!enemy) continue;
            if (!enemy.CanPerfectDodgeNow(transform)) continue;
            if (TryActivate(enemy.Owner)) return;
        }
    }

    // 플레이어와 적 사이를 바라보게 해서 둘 다 화면에 남긴다.
    private void FocusCamera(Transform target)
    {
        if (!target) return;

        if (cameraArm) cameraArm.ResetLookTarget();
        if (cameraController) cameraController.SetFocusOverride(target, cameraFocusHeight, cameraFocusRatio);
    }

    // 카메라를 플레이어 추적으로 되돌린다.
    private void RestoreCamera()
    {
        if (cameraArm) cameraArm.ResetLookTarget();
        if (cameraController) cameraController.ClearFocusOverride();
    }

    private void ApplySlowMotion()
    {
        ownsTimeScale = true;
        Time.timeScale = slowMotionTimeScale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * slowMotionTimeScale;
    }

    private void RestoreTimeScale()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
        ownsTimeScale = false;
    }

    private void EndWindow()
    {
        isActive = false;
        windowTimer = 0f;
        delayTimer = 0f;
        queuedStrongAttack = false;
        focusTarget = null;
        RestoreTimeScale();
    }
}
