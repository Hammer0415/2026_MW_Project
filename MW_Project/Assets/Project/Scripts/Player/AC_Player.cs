using UnityEngine;

public class AC_Player : MonoBehaviour
{
    //=====Singleton=====//
    public static AC_Player Instance { get; private set; }
    //===================//

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

    // Using in UI
    public float HP() => hp > 0.0f ? curHp / hp : 0.0f;
    public float Stemina() => stemina > 0.0f ? curStemina / stemina : 0.0f;
    //======================//

    //=====CheckingVars=====//
    [HideInInspector] public bool isSprint = false;
    [HideInInspector] public bool isStaminaExhausted = false;
    [HideInInspector] public bool isDead = false;
    //======================//

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

            if (curStemina <= 0.0f)
            {
                isSprint = false;
                isStaminaExhausted = true;
            }
        }
        else
        {
            curStemina += chargeStamina * Time.deltaTime;

            if (isStaminaExhausted && Stemina() >= 0.2f)
            {
                isStaminaExhausted = false;
            }
        }

        curStemina = Mathf.Clamp(curStemina, 0.0f, stemina);
    }

    //======================================//
    private void StartInitSetup()
    {
        curHp = hp;
        curStemina = stemina;

        isSprint = false;
        isStaminaExhausted = false;
        isDead = false;
    }
    //======================================//
}
