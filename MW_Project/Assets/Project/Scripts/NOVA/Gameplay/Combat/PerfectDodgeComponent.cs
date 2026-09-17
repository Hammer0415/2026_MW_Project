using UnityEngine;
using UnityEngine.InputSystem;

[AddComponentMenu("NOVA/Combat/Perfect Dodge Component")]
public class PerfectDodgeComponent : NovaComponent
{
    enum ChargeResult
    {
        None,
        Success,
        Partial
    }

    [Header("Slow Motion")]
    [Tooltip("퍼펙트 회피 후 슬로모션이 유지되는 실제 시간")]
    [Min(0.1f)]
    [SerializeField] private float slowMotionDuration = 5f;
    [Tooltip("퍼펙트 회피 중 TimeScale")]
    [Range(0.01f, 1f)]
    [SerializeField] private float slowMotionTimeScale = 0.1f;

    [Header("Strong Attack")]
    [Tooltip("강공격 성공 시 데미지 배율")]
    [Min(1f)]
    [SerializeField] private float strongAttackDamageMultiplier = 2f;
    [Tooltip("판정 실패 시 데미지 배율. 일반 공격보다 세고 성공보다 약하다.")]
    [Min(1f)]
    [SerializeField] private float partialDamageMultiplier = 1.35f;
    [Tooltip("강공격 성공 시 적이 경직되는 시간")]
    [Min(0f)]
    [SerializeField] private float strongAttackStunDuration = 2f;
    [Tooltip("강공격에 쓸 애니 Trigger. 비어 있으면 지금 기본 공격 애니를 쓴다.")]
    [SerializeField] private string strongAttackAnimationTrigger;

    [Header("Charge Timing")]
    [Tooltip("스테미나가 가득일 때 원이 모이는 시간")]
    [Min(0.1f)]
    [SerializeField] private float minChargeDuration = 0.45f;
    [Tooltip("스테미나가 없을 때 원이 모이는 시간")]
    [Min(0.1f)]
    [SerializeField] private float maxChargeDuration = 1.4f;

    [Header("Charge Ring")]
    [Tooltip("줄어들기 시작하는 원의 반지름")]
    [Min(0.5f)]
    [SerializeField] private float chargeStartRadius = 4.2f;
    [Tooltip("판정선 반지름")]
    [Min(0.2f)]
    [SerializeField] private float judgmentRadius = 1.35f;
    [Tooltip("판정선과 겹쳤다고 볼 거리")]
    [Min(0.05f)]
    [SerializeField] private float judgmentWindow = 0.22f;
    [Tooltip("링을 바닥에서 띄울 높이")]
    [SerializeField] private float ringHeight = 0.06f;
    [SerializeField] private Color shrinkingRingColor = new Color(1f, 0.72f, 0.18f, 0.95f);
    [SerializeField] private Color judgmentRingColor = new Color(0.95f, 0.95f, 0.95f, 0.9f);

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
    [SerializeField] private bool isCharging;
    [ReadOnly]
    [SerializeField] private float windowTimer;

    private CameraArmController cameraArm;
    private CameraController cameraController;
    private AbilitySystemComponent abilitySystem;
    private CharacterMovementComponent movement;
    private AttributeComponent attributes;
    private NovaCharacter focusTarget;
    private bool ownsTimeScale;
    private float defaultFixedDeltaTime;
    private bool wasDodging;
    private float chargeTimer;
    private float chargeDuration;
    private ChargeResult resolvedResult;
    private float resolvedTimer;
    private LineRenderer shrinkingRing;
    private LineRenderer judgmentRing;
    const int RingSegments = 64;
    const float ResolvedAttackLifetime = 1.6f;

    public bool IsActive => isActive;
    public bool IsChoosing => isActive && !isCharging && resolvedResult == ChargeResult.None;
    public bool IsCharging => isCharging;
    public bool BlocksOtherActions => IsChoosing || isCharging;
    public bool ShouldPreferFocusTarget => (isActive || isCharging || resolvedResult != ChargeResult.None) && FocusTarget;
    public bool HasResolvedResult => resolvedResult != ChargeResult.None;
    public NovaCharacter FocusTarget => focusTarget && !focusTarget.IsDead ? focusTarget : null;
    public float StrongAttackDamageMultiplier => strongAttackDamageMultiplier;
    public float StrongAttackStunDuration => strongAttackStunDuration;
    public float ResolvedDamageMultiplier
    {
        get
        {
            if (resolvedResult == ChargeResult.Success) return strongAttackDamageMultiplier;
            if (resolvedResult == ChargeResult.Partial) return partialDamageMultiplier;
            return 1f;
        }
    }
    public bool ResolvedAppliesStun => resolvedResult == ChargeResult.Success;

    protected override void Awake()
    {
        base.Awake();

        abilitySystem = GetComponent<AbilitySystemComponent>();
        movement = GetComponent<CharacterMovementComponent>();
        attributes = GetComponent<AttributeComponent>();
        defaultFixedDeltaTime = Time.fixedDeltaTime;

        if (Camera.main)
        {
            cameraArm = Camera.main.GetComponentInParent<CameraArmController>();
            cameraController = Camera.main.GetComponent<CameraController>();
        }

        CreateRings();
    }

    private void OnDestroy()
    {
        if (ownsTimeScale)
        {
            RestoreTimeScale();
        }

        DestroyRing(shrinkingRing);
        DestroyRing(judgmentRing);
    }

    private void Update()
    {
        DetectDodgeStart();
        TickWindow();
        TickCharge();
        TickResolvedResult();
        UpdateRings();
    }

    // 회피가 시작되면 열려 있는 적 공격 윈도우와 맞춰본다.
    private void DetectDodgeStart()
    {
        CharacterMovementComponent ownerMovement = movement ? movement : GetComponent<CharacterMovementComponent>();
        bool dodging = ownerMovement && ownerMovement.IsDodging;

        if (dodging && !wasDodging)
        {
            TryActivateFromOpenWindows();
        }

        wasDodging = dodging;
    }

    // 슬로모션 창이 끝나면 카메라와 시간을 되돌린다. 차징 중이면 기다린다.
    private void TickWindow()
    {
        if (!isActive) return;
        if (isCharging) return;
        if (resolvedResult != ChargeResult.None) return;

        windowTimer -= Time.unscaledDeltaTime;

        if (windowTimer <= 0f || (focusTarget && focusTarget.IsDead))
        {
            RestoreCamera();
            EndWindow();
        }
    }

    private void TickCharge()
    {
        if (!isCharging) return;

        if (focusTarget && focusTarget.IsDead)
        {
            CancelCharge();
            return;
        }

        chargeTimer += Time.unscaledDeltaTime;

        if (chargeTimer >= chargeDuration)
        {
            ResolveCharge(ChargeResult.Partial);
        }
    }

    private void TickResolvedResult()
    {
        if (resolvedResult == ChargeResult.None) return;
        if (isCharging) return;

        resolvedTimer -= Time.unscaledDeltaTime;

        if (resolvedTimer <= 0f)
        {
            RestoreCamera();
            EndWindow();
        }
    }

    // 적의 퍼펙트 회피 타이밍에 범위 안에서 회피하면 슬로모션을 시작한다.
    public bool TryActivate(NovaActor attacker)
    {
        if (isActive || isCharging) return false;
        if (!attacker) return false;

        NovaCharacter attackerCharacter = attacker as NovaCharacter;

        if (!attackerCharacter) attackerCharacter = attacker.GetComponent<NovaCharacter>();
        if (!attackerCharacter) return false;
        if (attackerCharacter.IsDead) return false;

        isActive = true;
        isCharging = false;
        resolvedResult = ChargeResult.None;
        focusTarget = attackerCharacter;
        windowTimer = slowMotionDuration;
        chargeTimer = 0f;
        resolvedTimer = 0f;

        ApplySlowMotion();
        FocusCamera(focusTarget.transform);
        GameplayEventBus.Raise(new GameplayTag("Combat.PerfectDodge"), Owner, attackerCharacter);

        return true;
    }

    // 슬로모 중에 우클릭하면 타임스케일을 되돌리고 차징 QTE를 시작한다.
    public void OnStrongAttackCharge(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        TryStartCharge();
    }

    public bool TryStartCharge()
    {
        if (!IsChoosing) return false;

        isCharging = true;
        chargeTimer = 0f;
        chargeDuration = GetChargeDuration();

        RestoreTimeScale();

        if (movement)
        {
            movement.CancelDodge();
            movement.SetMovementEnabled(false);
        }

        SetRingsVisible(true);
        return true;
    }

    // 차징 중 좌클릭으로 판정선을 맞춘다.
    public bool TrySubmitCharge()
    {
        if (!isCharging) return false;

        float currentRadius = GetCurrentChargeRadius();
        bool success = Mathf.Abs(currentRadius - judgmentRadius) <= judgmentWindow;
        ResolveCharge(success ? ChargeResult.Success : ChargeResult.Partial);
        return true;
    }

    // 슬로모션 중 공격이 맞으면 성공 시에만 스턴하고 창을 닫는다.
    public bool TryApplyStrongAttack(NovaActor hitTarget)
    {
        if (resolvedResult == ChargeResult.None) return false;
        if (!hitTarget) return false;

        if (resolvedResult == ChargeResult.Success)
        {
            EnemyBrainComponent brain = hitTarget.GetComponent<EnemyBrainComponent>();

            if (brain) brain.Stun(strongAttackStunDuration);
        }

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

    private void ResolveCharge(ChargeResult result)
    {
        if (!isCharging) return;

        isCharging = false;
        resolvedResult = result;
        resolvedTimer = ResolvedAttackLifetime;
        SetRingsVisible(false);

        if (abilitySystem) abilitySystem.StartBufferedBasicAttack(strongAttackAnimationTrigger);
    }

    private void CancelCharge()
    {
        isCharging = false;
        SetRingsVisible(false);

        if (movement) movement.SetMovementEnabled(true);

        RestoreCamera();
        EndWindow();
    }

    private float GetChargeDuration()
    {
        float stamina01 = 0f;

        if (attributes && attributes.MaxStamina > 0f)
        {
            stamina01 = Mathf.Clamp01(attributes.CurrentStamina / attributes.MaxStamina);
        }

        float min = Mathf.Min(minChargeDuration, maxChargeDuration);
        float max = Mathf.Max(minChargeDuration, maxChargeDuration);
        return Mathf.Lerp(max, min, stamina01);
    }

    private float GetCurrentChargeRadius()
    {
        if (chargeDuration <= 0.0001f) return 0f;

        float t = Mathf.Clamp01(chargeTimer / chargeDuration);
        return Mathf.Lerp(chargeStartRadius, 0f, t);
    }

    private void FocusCamera(Transform target)
    {
        if (!target) return;

        if (cameraArm) cameraArm.ResetLookTarget();
        if (cameraController) cameraController.SetFocusOverride(target, cameraFocusHeight, cameraFocusRatio);
    }

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
        isCharging = false;
        windowTimer = 0f;
        chargeTimer = 0f;
        resolvedTimer = 0f;
        resolvedResult = ChargeResult.None;
        focusTarget = null;
        SetRingsVisible(false);
        RestoreTimeScale();
    }

    private void CreateRings()
    {
        shrinkingRing = CreateRing("StrongAttackChargeRing", shrinkingRingColor, 0.08f);
        judgmentRing = CreateRing("StrongAttackJudgmentRing", judgmentRingColor, 0.12f);
        if (shrinkingRing) shrinkingRing.transform.SetParent(transform, false);
        if (judgmentRing) judgmentRing.transform.SetParent(transform, false);
        SetRingsVisible(false);
    }

    static void DestroyRing(LineRenderer line)
    {
        if (!line) return;

        if (line.sharedMaterial) Destroy(line.sharedMaterial);
        Destroy(line.gameObject);
    }

    static LineRenderer CreateRing(string name, Color color, float width)
    {
        var go = new GameObject(name);
        go.hideFlags = HideFlags.HideAndDontSave;

        var line = go.AddComponent<LineRenderer>();
        line.loop = true;
        line.useWorldSpace = true;
        line.positionCount = RingSegments;
        line.startWidth = width;
        line.endWidth = width;
        line.widthMultiplier = 1f;
        line.alignment = LineAlignment.View;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.numCapVertices = 2;
        line.textureMode = LineTextureMode.Stretch;

        var shader = Shader.Find("Sprites/Default");
        if (!shader) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader)
        {
            var material = new Material(shader);
            material.color = color;
            line.sharedMaterial = material;
        }

        line.startColor = color;
        line.endColor = color;
        return line;
    }

    private void SetRingsVisible(bool visible)
    {
        if (shrinkingRing) shrinkingRing.enabled = visible;
        if (judgmentRing) judgmentRing.enabled = visible;
    }

    private void UpdateRings()
    {
        bool show = isCharging && Owner;
        SetRingsVisible(show);

        if (!show) return;

        Vector3 center = Owner.transform.position;
        center.y += ringHeight;

        WriteRing(shrinkingRing, center, GetCurrentChargeRadius());
        WriteRing(judgmentRing, center, judgmentRadius);
    }

    static void WriteRing(LineRenderer line, Vector3 center, float radius)
    {
        if (!line) return;

        for (int i = 0; i < RingSegments; i++)
        {
            float angle = (i / (float)RingSegments) * Mathf.PI * 2f;
            line.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }
}
