using UnityEngine;
using UnityEngine.InputSystem;

public class AC_PlayerWeakPointController : MonoBehaviour
{
    //=====Components=====//
    private AC_PlayerAttackController attackController = null;
    private AC_PlayerController playerController = null;
    //====================//

    //=====WeakPoint Settings=====//
    [Header("WeakPoint Settings")]
    [Tooltip("WeakPoint 유지 시간")]
    [SerializeField] private float weakPointDuration = 0.0f;
    [Tooltip("WeakPoint 발동 시 시간 배율")]
    [SerializeField] private float weakPointTimeScale = 0.0f;
    [Tooltip("강공격 데미지 배율")]
    public float strongAttackDamageMultiplier = 0.0f;
    [Tooltip("강공격 스턴 시간")]
    [SerializeField] private float strongAttackStunDuration = 0.0f;
    //============================//

    //=====CheckingVars=====//
    [ReadOnly]
    [Tooltip("WeakPoint 활성화 여부")]
    public bool isWeakPointActive = false;
    [ReadOnly]
    [Tooltip("WeakPoint 타이머")]
    [SerializeField] private float weakPointTimer = 0.0f;
    //======================//

    private void Awake()
    {
        InitSetup();
    }

    private void Update()
    {
        if (!isWeakPointActive) return;

        weakPointTimer -= Time.unscaledDeltaTime;

        if (weakPointTimer <= 0.0f)
        {
            EndWeakPoint();
        }
    }

    public void ActivateWeakPoint()
    {
        if (isWeakPointActive) return;

        isWeakPointActive = true;
        weakPointTimer = weakPointDuration;
        Time.timeScale = weakPointTimeScale;

        Debug.Log("WEAK POINT START!");
    }

    public bool TryStrongAttack()
    {
        if (!isWeakPointActive) return false;

        EndWeakPoint();

        return true;
    }

    private void EndWeakPoint()
    {
        isWeakPointActive = false;
        weakPointTimer = 0.0f;
        Time.timeScale = 1.0f;
    }

    //======================================//
    private void InitSetup()
    {
        if (attackController == null) attackController = GetComponent<AC_PlayerAttackController>();
        if (playerController == null) playerController = GetComponent<AC_PlayerController>();

        if (attackController == null) Debug.LogError("AC_PlayerAttackController를 찾을 수 없습니다.", this);
        if (playerController == null) Debug.LogError("AC_PlayerController를 찾을 수 없습니다.", this);
    }
    //======================================//
}
