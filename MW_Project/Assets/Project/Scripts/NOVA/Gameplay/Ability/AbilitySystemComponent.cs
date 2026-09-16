using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[AddComponentMenu("NOVA/Ability/Ability System Component")]
public class AbilitySystemComponent : NovaComponent
{
    [Header("Abilities")]
    [Tooltip("이 캐릭터가 보유한 Ability 목록. 런타임에는 복제본을 사용한다.")]
    [SerializeField] private List<NovaAbility> abilities = new();

    [Header("Ability Slots")]
    [Tooltip("기본 공격 Ability Tag")]
    [SerializeField] private GameplayTag basicAttackTag;
    [Tooltip("회피 Ability Tag. 비어 있으면 Ability.Dodge를 사용한다.")]
    [SerializeField] private GameplayTag dodgeTag;
    [Tooltip("스킬 Ability Tag. 비어 있으면 Ability.Skill을 사용한다.")]
    [SerializeField] private GameplayTag skillTag;
    [Tooltip("궁극기 Ability Tag. 비어 있으면 Ability.Ultimate를 사용한다.")]
    [SerializeField] private GameplayTag ultimateTag;

    [Header("Animation")]
    [Tooltip("기본 공격 입력 시 재생할 Animator Trigger")]
    [SerializeField] private string basicAttackTrigger = "isComboAttack";
    [Tooltip("콤보 진행 값을 넣을 Animator Int 파라미터")]
    [SerializeField] private string comboIndexParameter = "Combo Index";
    [Tooltip("콤보 최대 타수. 3이면 01-02-03까지 이어진다.")]
    [Min(1)]
    [SerializeField] private int maxComboCount = 3;
    [Tooltip("콤보 시작 직후 애니메이터가 Combo 상태로 들어가길 기다리는 시간")]
    [Min(0f)]
    [SerializeField] private float comboStartGrace = 0.15f;
    [Tooltip("콤보가 끝나지 않고 멈췄을 때 강제로 해제하는 시간")]
    [Min(0.1f)]
    [SerializeField] private float comboTimeout = 3f;
    [Tooltip("콤보에서 Move로 넘어갈 때 블렌드 시간")]
    [Min(0f)]
    [SerializeField] private float comboToMoveBlendTime = 0.2f;

    private readonly Dictionary<GameplayTag, NovaAbility> abilityMap = new();
    private readonly List<NovaAbility> runtimeAbilities = new();

    private CharacterMovementComponent movement;
    private CharacterAnimationComponent animationComponent;
    private CombatComponent combat;
    private PerfectDodgeComponent perfectDodge;
    private Animator animator;
    private NovaAbility activeAbility;
    private int queuedComboCount;
    private bool isComboActive;
    private float comboTimer;
    private float comboGraceTimer;
    private string trackedComboClipName;
    private bool comboMoveUnlocked;

    public NovaAbility ActiveAbility => activeAbility;
    public bool HasActiveAbility => activeAbility && activeAbility.IsActive;
    public GameplayTag BasicAttackTag => basicAttackTag;
    public GameplayTag DodgeTag => dodgeTag.IsValid() ? dodgeTag : new GameplayTag("Ability.Dodge");
    public GameplayTag SkillTag => skillTag.IsValid() ? skillTag : new GameplayTag("Ability.Skill");
    public GameplayTag UltimateTag => ultimateTag.IsValid() ? ultimateTag : new GameplayTag("Ability.Ultimate");

    protected override void Awake()
    {
        base.Awake();

        movement = GetComponent<CharacterMovementComponent>();
        animationComponent = GetComponent<CharacterAnimationComponent>();
        combat = GetComponent<CombatComponent>();
        perfectDodge = GetComponent<PerfectDodgeComponent>();
        animator = GetComponent<Animator>();

        if (!animator && animationComponent) animator = animationComponent.Animator;

        InitializeAbilities();
    }

    private void Update()
    {
        TickAbilities(Time.deltaTime);
        TickCombo(Time.deltaTime);
    }

    // 기본 공격 입력. 첫 클릭은 Combo01을 시작하고, 추가 클릭은 다음 콤보를 예약한다.
    public void OnBasicAttack(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        if (perfectDodge && perfectDodge.TryQueueStrongAttack()) return;

        if (isComboActive)
        {
            TryQueueNextCombo();
            return;
        }

        StartCombo();
    }

    // 퍼펙트 회피로 받아 둔 기본 공격을 시작한다.
    public void StartBufferedBasicAttack()
    {
        if (isComboActive) return;

        StartCombo();
    }

    // 스킬 입력. 지정된 스킬 Ability를 시전한다.
    public void OnSkill(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        TryActivateAbility(SkillTag);
    }

    // 궁극기 입력. 지정된 궁극기 Ability를 시전한다.
    public void OnUltimate(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        TryActivateAbility(UltimateTag);
    }

    // Inspector에 등록된 Ability를 런타임 인스턴스로 복제한다.
    private void InitializeAbilities()
    {
        abilityMap.Clear();
        runtimeAbilities.Clear();

        foreach (NovaAbility ability in abilities)
        {
            if (!ability) continue;

            NovaAbility instance = ability.CreateRuntimeInstance();

            if (!instance) continue;

            GameplayTag abilityTag = instance.AbilityTag;

            if (!abilityTag.IsValid()) continue;

            if (abilityMap.ContainsKey(abilityTag))
            {
                Debug.LogWarning($"Ability가 중복 되었습니다. {abilityTag}", this);
                continue;
            }

            runtimeAbilities.Add(instance);
            abilityMap.Add(abilityTag, instance);
        }
    }

    // 모든 Ability의 쿨타임을 갱신한다.
    private void TickAbilities(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        for (int i = 0; i < runtimeAbilities.Count; i++)
        {
            NovaAbility ability = runtimeAbilities[i];

            if (!ability) continue;

            ability.TickCooldown(deltaTime);
        }
    }

    // 해당 태그의 Ability를 보유 중인지 확인한다.
    public bool HasAbility(GameplayTag abilityTag)
    {
        return abilityMap.ContainsKey(abilityTag);
    }

    // 해당 태그의 Ability를 반환한다.
    public NovaAbility GetAbility(GameplayTag abilityTag)
    {
        abilityMap.TryGetValue(abilityTag, out NovaAbility ability);

        return ability;
    }

    // 태그로 Ability를 시전한다.
    public bool TryActivateAbility(GameplayTag abilityTag, bool ignoreCooldown = false)
    {
        NovaAbility ability = GetAbility(abilityTag);

        if (!ability) return false;

        bool activated = ability.TryActivate(Owner, ignoreCooldown);

        if (activated)
        {
            activeAbility = ability;
        }

        return activated;
    }

    // 런타임에 Ability를 추가한다. 같은 태그가 있으면 교체한다.
    public bool GrantAbility(NovaAbility abilityAsset)
    {
        if (!abilityAsset) return false;
        if (!abilityAsset.AbilityTag.IsValid()) return false;

        NovaAbility instance = abilityAsset.CreateRuntimeInstance();

        if (abilityMap.ContainsKey(instance.AbilityTag))
        {
            RemoveAbility(instance.AbilityTag);
        }

        runtimeAbilities.Add(instance);
        abilityMap.Add(instance.AbilityTag, instance);

        return true;
    }

    // 태그로 Ability를 제거한다.
    public bool RemoveAbility(GameplayTag abilityTag)
    {
        if (!abilityMap.TryGetValue(abilityTag, out NovaAbility ability)) return false;

        runtimeAbilities.Remove(ability);
        abilityMap.Remove(abilityTag);

        if (activeAbility == ability)
        {
            activeAbility = null;
        }

        return true;
    }

    // Animation Event: 다음 콤보 공격 구간으로 넘긴다.
    public void AdvanceCombo()
    {
        if (!isComboActive) return;

        if (combat) combat.AdvanceCombo();
    }

    // Animation Event: 지정한 총구/공격 인덱스로 기본 공격을 발동한다.
    public void TryActivateBasicAttack(int index)
    {
        if (!isComboActive) return;

        if (combat) combat.SetMuzzleIndex(index);

        TryActivateAbility(basicAttackTag, true);
    }

    // Animation Event: 이 시점부터 이동/점프하면 콤보가 해당 애니메이션으로 블렌드된다.
    public void UnlockComboMove()
    {
        if (!isComboActive) return;
        if (queuedComboCount > GetCurrentComboStep()) return;

        UnlockComboMovement();
        TryCancelComboIntoMove();
    }

    // Animation Event: 현재 Ability를 종료하고 이동을 다시 허용한다.
    public void EndActiveAbility()
    {
        bool abortComboAnimation = isComboActive && IsPlayingComboAnimation();

        ResetComboState();

        if (abortComboAnimation && animator)
        {
            bool cancelToJump = movement && (movement.HasJumpInput || movement.DidJump);
            string stateName = cancelToJump ? "Lea_JumStart" : "Move";

            animator.ResetTrigger("isJump");
            animator.CrossFadeInFixedTime(stateName, comboToMoveBlendTime);

            if (cancelToJump)
            {
                animator.SetBool("isInAir", true);
            }
        }

        if (activeAbility)
        {
            activeAbility.EndAbility(Owner);
            activeAbility = null;
        }

        if (combat) combat.StopAttackFacing();

        if (movement)
        {
            movement.SetMovementEnabled(true);
            movement.SetRootMotionEnabled(false);
        }

        if (animationComponent)
        {
            animationComponent.SetRootMotion(false);
        }
    }

    // 회피로 공격을 끊는다. 이동 잠금도 함께 풀고, 남은 공격 애니메이션/이벤트는 무시한다.
    public void InterruptForDodge()
    {
        bool stopComboAnimation = isComboActive || IsPlayingComboAnimation();

        ResetComboState();

        if (movement)
        {
            movement.SetRootMotionEnabled(false);
            movement.SetMovementEnabled(true);
        }

        if (animationComponent)
        {
            animationComponent.SetRootMotion(false);
        }

        if (animator)
        {
            animator.applyRootMotion = false;
            animator.ResetTrigger(basicAttackTrigger);
            animator.ResetTrigger("isJump");

            if (stopComboAnimation)
            {
                animator.Play("Move", 0, 0f);
            }
        }

        if (activeAbility)
        {
            activeAbility.EndAbility(Owner);
            activeAbility = null;
        }

        if (combat) combat.StopAttackFacing();
    }

    // Combo01을 시작하고 이동을 잠근 뒤 적을 바라보게 한다.
    private void StartCombo()
    {
        queuedComboCount = 1;
        isComboActive = true;
        comboTimer = 0f;
        comboGraceTimer = comboStartGrace;
        trackedComboClipName = string.Empty;
        comboMoveUnlocked = false;

        SetComboIndex(1);
        PlayAttackAnimation(basicAttackTrigger);

        if (combat) combat.StartAttackFacing();
    }

    // 콤보 중에 추가 클릭이 들어오면 다음 타까지 이어지게 Combo Index를 올린다.
    private void TryQueueNextCombo()
    {
        if (queuedComboCount >= maxComboCount) return;

        queuedComboCount++;
        SetComboIndex(queuedComboCount);

        if (combat) combat.StartAttackFacing();
    }

    // 콤보 애니메이션이 끝났는지 확인하고, UnlockComboMove 이후 이동 캔슬을 처리한다.
    private void TickCombo(float deltaTime)
    {
        if (!isComboActive) return;

        comboTimer += deltaTime;
        RefreshComboClipTracking();
        TryCancelComboIntoMove();

        if (comboGraceTimer > 0f)
        {
            comboGraceTimer -= deltaTime;
            return;
        }

        if (comboTimer >= comboTimeout || !IsPlayingComboAnimation())
        {
            EndActiveAbility();
        }
    }

    // 다음 콤보 클립으로 넘어가면 다시 이동을 잠근다.
    private void RefreshComboClipTracking()
    {
        string clipName = GetCurrentComboClipName();

        if (string.IsNullOrEmpty(clipName)) return;
        if (clipName == trackedComboClipName) return;

        trackedComboClipName = clipName;
        comboMoveUnlocked = false;

        LockComboMovement();
    }

    // 이동이 풀린 뒤 이동/점프 입력이 들어오면 해당 애니메이션으로 블렌드한다.
    private void TryCancelComboIntoMove()
    {
        if (!comboMoveUnlocked) return;
        if (!movement) return;
        if (queuedComboCount > GetCurrentComboStep()) return;
        if (!movement.HasMoveInput && !movement.HasJumpInput && !movement.DidJump) return;

        EndActiveAbility();
    }

    // 콤보 중 이동과 Root Motion을 잠근다.
    private void LockComboMovement()
    {
        if (!movement) return;

        movement.SetMovementEnabled(false);
        movement.SetRootMotionEnabled(true);
    }

    // 공격 판정이 끝난 뒤 이동 캔슬이 가능하게 한다.
    private void UnlockComboMovement()
    {
        comboMoveUnlocked = true;

        if (!movement) return;

        movement.SetMovementEnabled(true);
        movement.SetRootMotionEnabled(false);
    }

    // 현재 재생 중인 콤보 단계(1~3)를 반환한다.
    private int GetCurrentComboStep()
    {
        if (string.IsNullOrEmpty(trackedComboClipName)) return queuedComboCount;

        if (trackedComboClipName.Contains("Combo03")) return 3;
        if (trackedComboClipName.Contains("Combo02")) return 2;
        if (trackedComboClipName.Contains("Combo01")) return 1;

        return queuedComboCount;
    }

    // 현재 재생 중인 콤보 클립 이름을 반환한다.
    private string GetCurrentComboClipName()
    {
        if (!animator) return string.Empty;

        string clipName = FindComboClipName(animator.GetCurrentAnimatorClipInfo(0));

        if (!string.IsNullOrEmpty(clipName)) return clipName;

        if (animator.IsInTransition(0))
        {
            return FindComboClipName(animator.GetNextAnimatorClipInfo(0));
        }

        return string.Empty;
    }

    // 클립 목록에서 콤보 공격 클립 이름을 찾는다.
    private string FindComboClipName(AnimatorClipInfo[] clipInfos)
    {
        if (clipInfos == null) return string.Empty;

        for (int i = 0; i < clipInfos.Length; i++)
        {
            AnimationClip clip = clipInfos[i].clip;

            if (!clip) continue;
            if (clip.name.StartsWith("Lea_Combo")) return clip.name;
        }

        return string.Empty;
    }

    // 현재 Animator가 콤보 공격 상태에 있는지 확인한다.
    private bool IsPlayingComboAnimation()
    {
        if (!animator) return false;

        if (HasComboClip(animator.GetCurrentAnimatorClipInfo(0))) return true;

        if (animator.IsInTransition(0) && HasComboClip(animator.GetNextAnimatorClipInfo(0))) return true;

        return false;
    }

    // 클립 이름이 콤보 공격인지 확인한다.
    private bool HasComboClip(AnimatorClipInfo[] clipInfos)
    {
        if (clipInfos == null) return false;

        for (int i = 0; i < clipInfos.Length; i++)
        {
            AnimationClip clip = clipInfos[i].clip;

            if (!clip) continue;
            if (clip.name.StartsWith("Lea_Combo")) return true;
        }

        return false;
    }

    // Combo Index 파라미터를 갱신한다.
    private void SetComboIndex(int index)
    {
        if (animationComponent)
        {
            animationComponent.SetInt(comboIndexParameter, index);
            return;
        }

        if (!animator) return;

        animator.SetInteger(comboIndexParameter, index);
    }

    // 콤보 입력 상태를 초기화한다.
    private void ResetComboState()
    {
        isComboActive = false;
        queuedComboCount = 0;
        comboTimer = 0f;
        comboGraceTimer = 0f;
        trackedComboClipName = string.Empty;
        comboMoveUnlocked = false;

        SetComboIndex(0);
    }

    // 공격 애니메이션을 재생하고 이동을 잠시 잠근다.
    private void PlayAttackAnimation(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName)) return;

        if (movement) movement.SetMovementEnabled(false);

        if (animationComponent) animationComponent.PlayTrigger(triggerName, true);
        else if (movement) movement.HandleAttackAnimation(triggerName);
    }
}
