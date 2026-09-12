using UnityEngine;

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
    //=====================//

    //=====StateSettings=====//
    [Header("State Settings")]
    [Tooltip("플레이어 게임오버 여부")]
    [ReadOnly]
    public bool isPlayerDead = false;
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

    //======================================//
    private void StartInitSetup()
    {
        isCursorLocked = false;
        isPlayerDead = false;
    }
    //======================================//
}
