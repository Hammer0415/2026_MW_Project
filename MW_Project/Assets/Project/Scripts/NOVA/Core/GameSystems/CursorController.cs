using UnityEngine;

public class CursorController : MonoBehaviour
{
    public static CursorController Instance { get; private set; }

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool IsLocked()
    {
        return Cursor.lockState == CursorLockMode.Locked;
    }

    public void CursorOnOff()
    {
        if (IsLocked())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
