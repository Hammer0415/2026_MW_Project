using UnityEngine;

public class AC_Player : MonoBehaviour
{
    //=====Singleton=====//
    public static AC_Player Instance { get; private set; }
    //===================//

    //=====Components=====//
    private MG_Game gameManager = null;
    //====================//

    //=====PlayerValues=====//
    [Header("Player Settings")]
    [Tooltip("플레이어 최대 체력")]
    public float hp = 0.0f;
    private float curHp = 0.0f;
    [Tooltip("플레이어 최대 스테미나")]
    public float stemina = 0.0f;
    private float curStemina = 0.0f;
    [Tooltip("스테미나 사용값")]
    [SerializeField] private float decreaseStemina = 0.0f;
    [Tooltip("스테미나 자동 회복값")]
    [SerializeField] private float chargeStamina = 0.0f;
    [Tooltip("스테미나 회복 딜레이")]
    [SerializeField] private float staminaRecoveryDelay = 0.0f;

    // Using in UI
    public float HP() => hp > 0.0f ? curHp / hp : 0.0f;
    public float Stemina() => stemina > 0.0f ? curStemina / stemina : 0.0f;
    //======================//

    //=====CheckingVars=====//
    [HideInInspector] public bool isSprint = false;
    [HideInInspector] public bool isStaminaExhausted = false;
    [HideInInspector] public bool isDead = false;
    //======================//

    //=====OtherSettings=====//
    private float staminaRecoveryTimer = 0.0f;
    //===================//

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        StartInitSetup();
    }

    private void Update()
    {
        HandleStamina();
        HandleDead();
    }

    public void TakeDamage(float damage)
    {
        if (curHp > 0.0f)
        {
            curHp -= damage;
        }

        curHp = Mathf.Clamp(curHp, 0.0f, hp);
    }

    private void HandleStamina()
    {
        if (isSprint)
        {
            curStemina -= decreaseStemina * Time.deltaTime;
            staminaRecoveryTimer = staminaRecoveryDelay;

            if (curStemina <= 0.0f)
            {
                curStemina = 0.0f;
                isSprint = false;
                isStaminaExhausted = true;
            }
        }
        else
        {
            if (staminaRecoveryTimer > 0.0f)
            {
                staminaRecoveryTimer -= Time.deltaTime;
            }
            else
            {
                curStemina += chargeStamina * Time.deltaTime;
            }

            if (isStaminaExhausted && Stemina() >= 0.2f)
            {
                isStaminaExhausted = false;
            }
        }

        curStemina = Mathf.Clamp(curStemina, 0.0f, stemina);
    }

    public bool UseStamina(float amount)
    {
        if (curStemina < amount)
            return false;

        curStemina -= amount;
        return true;
    }

    private void HandleDead()
    {
        if (curHp <= 0.0f)
        {
            gameManager.isPlayerDead = true;
        }
    }

    //======================================//
    private void StartInitSetup()
    {
        if (gameManager == null) gameManager = MG_Game.Instance;

        if (gameManager == null) Debug.LogError("MG_Game를 찾을 수 없습니다.");

        curHp = hp;
        curStemina = stemina;

        isSprint = false;
        isStaminaExhausted = false;
        isDead = false;
    }
    //======================================//
}
