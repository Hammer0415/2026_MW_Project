using UnityEngine;
using System.Collections;

public class MG_Game : MonoBehaviour
{
    //=====Singleton=====//
    public static MG_Game Instance { get; private set; }
    //===================//

    //=====GameSettings=====//
    [Header("Game Settings")]
    [Tooltip("마우스 커서 숨김 여부")]
    [ReadOnly]
    [SerializeField] private bool isCursorLocked = false;
    [Tooltip("플레이어 전투 여부")]
    [ReadOnly]
    public bool isBattle = false;
    //=====================//

    //=====StateSettings=====//
    [Header("State Settings")]
    [Tooltip("플레이어 게임오버 여부")]
    [ReadOnly]
    public bool isPlayerDead = false;
    //=======================//

    //=====OtherSettings=====//
    private int battleEnemyCount = 0;
    //=======================//

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void Start()
    {
        StartInitSetup();
    }

    public void CursorOnOff()
    {
        if (!isCursorLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            isCursorLocked = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            isCursorLocked = false;
        }
    }

    public void EnterBattle()
    {
        battleEnemyCount++;

        isBattle = battleEnemyCount > 0;

        if (MG_Audio.Instance != null) MG_Audio.Instance.PlayBattleBGM();
    }

    public void ExitBattle()
    {
        battleEnemyCount--;
        battleEnemyCount = Mathf.Max(battleEnemyCount, 0);

        isBattle = battleEnemyCount > 0;

        StartCoroutine(WaitBattleExit());
    }

    private IEnumerator WaitBattleExit()
    {
        yield return new WaitForSeconds(3.0f);
        
        if (!isBattle)
        {
            if (MG_Audio.Instance != null) MG_Audio.Instance.PlayDefaultBGM();
        }
    }

    //======================================//
    private void StartInitSetup()
    {
        isCursorLocked = false;
        isPlayerDead = false;
        isBattle = false;

        battleEnemyCount = 0;
        isBattle = false;
    }
    //======================================//
}
