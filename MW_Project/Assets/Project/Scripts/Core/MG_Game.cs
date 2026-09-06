using UnityEngine;

public class MG_Game : MonoBehaviour
{
    //=====Singleton=====//
    public static MG_Game Instance { get; private set; }
    //===================//

    //=====GameSetting=====//
    [Header("Game Settings")]
    [Tooltip("마우스 커서 숨김 여부")]
    public bool isCursorLocked = false;
    //=====================//

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
        isCursorLocked = false;
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
}
