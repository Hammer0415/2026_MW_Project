using UnityEngine;

[AddComponentMenu("NOVA/Core/Cursor Controller")]
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

    // 커서가 잠겨 있는지 확인한다.
    public bool IsLocked()
    {
        return Cursor.lockState == CursorLockMode.Locked;
    }

    // 커서 잠금과 표시 상태를 서로 전환한다.
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

    // 커서를 잠그거나 해제한다.
    public void SetLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
