using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AC_Enemy : MonoBehaviour
{
    //=====Singleton=====//
    public static AC_Enemy Instance { get; private set; }
    //===================//

    //=====EnemyValues=====//
    [Header("Enemy Settings")]
    [Tooltip("적 최대 체력")]
    public float hp = 0.0f;
    private float curHp = 0.0f;

    // Using in UI
    public float HP() => hp > 0.0f ? curHp / hp : 0.0f;
    //======================//

    //=====UI=====//
    [Header("Enemy UIs")]
    [SerializeField] private Slider enemyHpBar = null;
    [SerializeField] private TextMeshProUGUI hpText = null;
    public Transform targetPoint = null;
    //============//

    //=====CheckingVars=====//
    [HideInInspector] public bool isDead = false;
    //======================//

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        StartInitSetup();
    }

    private void Update()
    {
        HandleEnemyUI();
        HandleDead();
    }

    public void TakeDamage(float damage)
    {
        if (curHp <= 0.0f)
            return;

        curHp -= damage;
        curHp = Mathf.Clamp(curHp, 0.0f, hp);

        AC_EnemyController controller =
            GetComponent<AC_EnemyController>();

        if (controller != null)
        {
            controller.OnHitByPlayer();
        }
    }

    private void HandleDead()
    {
        if (curHp <= 0.0f)
        {
            Destroy(this.gameObject);
        }
    }

    private void HandleEnemyUI()
    {
        if (enemyHpBar) enemyHpBar.value = HP();
        if (hpText) hpText.text = curHp.ToString("F0");
    }

    //======================================//
    private void StartInitSetup()
    {
        curHp = hp;

        isDead = false;
    }
    //======================================//
}
