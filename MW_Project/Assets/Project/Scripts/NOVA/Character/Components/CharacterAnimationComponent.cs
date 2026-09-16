using UnityEngine;

[AddComponentMenu("NOVA/Character/Character Animation Component")]
public class CharacterAnimationComponent : NovaComponent
{
    [Header("Animation")]
    [Tooltip("캐릭터 Animator. 비어 있으면 같은 오브젝트에서 찾는다.")]
    [SerializeField] private Animator animator;
    [Tooltip("이동 블렌드 댐핑 시간")]
    [SerializeField] private float moveDampTime = 0.1f;
    [Tooltip("이동 파라미터 이름")]
    [SerializeField] private string moveParameter = "Move";

    public Animator Animator => animator;

    protected override void Awake()
    {
        base.Awake();

        if (!animator) animator = GetComponent<Animator>();
    }

    // Animator Trigger를 재생한다.
    public void PlayTrigger(string triggerName, bool rootMotion = false)
    {
        if (!animator) return;
        if (string.IsNullOrEmpty(triggerName)) return;

        animator.applyRootMotion = rootMotion;
        animator.SetTrigger(triggerName);
    }

    // 이동 블렌드 값을 갱신한다.
    public void SetMove(float moveValue)
    {
        if (!animator) return;

        animator.SetFloat(moveParameter, moveValue, moveDampTime, Time.deltaTime);
    }

    // Root Motion 사용 여부를 설정한다.
    public void SetRootMotion(bool enabled)
    {
        if (!animator) return;

        animator.applyRootMotion = enabled;
    }

    // Bool 파라미터를 설정한다.
    public void SetBool(string parameterName, bool value)
    {
        if (!animator) return;
        if (string.IsNullOrEmpty(parameterName)) return;

        animator.SetBool(parameterName, value);
    }

    // Int 파라미터를 설정한다.
    public void SetInt(string parameterName, int value)
    {
        if (!animator) return;
        if (string.IsNullOrEmpty(parameterName)) return;

        animator.SetInteger(parameterName, value);
    }

    // 점프 Trigger와 공중 상태를 애니메이터에 전달한다.
    public void PlayJump()
    {
        if (IsPlayingJump())
        {
            SetBool("isInAir", true);
            return;
        }

        PlayTrigger("isJump", false);
        SetBool("isInAir", true);
    }

    // 이미 점프 애니메이션으로 넘어가는 중이면 Trigger를 다시 넣지 않는다.
    public bool IsPlayingJump()
    {
        if (!animator) return false;

        if (IsJumpState(animator.GetCurrentAnimatorStateInfo(0))) return true;

        if (animator.IsInTransition(0) && IsJumpState(animator.GetNextAnimatorStateInfo(0))) return true;

        return false;
    }

    private bool IsJumpState(AnimatorStateInfo state)
    {
        return state.IsName("Lea_JumStart") || state.IsName("Lea_InAir") || state.IsName("Lea_JumEnd");
    }

    // 공중 여부를 애니메이터에 전달한다.
    public void SetInAir(bool inAir)
    {
        SetBool("isInAir", inAir);
    }
}
