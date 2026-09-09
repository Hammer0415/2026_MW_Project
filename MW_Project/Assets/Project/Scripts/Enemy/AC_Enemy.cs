using UnityEngine;
using UnityEngine.UI;

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
    //============//

    //=====CheckingVars=====//
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
        HandleEnemyUI();
    }

    public void TakeDamage(float damage)
    {
        if (curHp > 0.0f)
        {
            curHp -= damage;
        }

        curHp = Mathf.Clamp(curHp, 0.0f, hp);
    }

    private void HandleEnemyUI()
    {
        enemyHpBar.value = HP();
    }

    //======================================//
    private void StartInitSetup()
    {
        curHp = hp;

        isDead = false;
    }
    //======================================//
}
