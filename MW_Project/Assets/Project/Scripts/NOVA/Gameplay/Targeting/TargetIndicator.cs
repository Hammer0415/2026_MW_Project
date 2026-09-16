using UnityEngine;

[AddComponentMenu("NOVA/Targeting/Target Indicator")]
public class TargetIndicator : MonoBehaviour
{
    [Header("Target Indicator")]
    [Tooltip("타겟팅 표시 UI")]
    [SerializeField] private GameObject indicator = null;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;

        SetVisible(false);
    }

    public void LateUpdate()
    {
        if (!indicator) return;
        if (!indicator.activeSelf) return;
        if (!mainCamera) return;

        transform.rotation = mainCamera.transform.rotation;
    }

    // 타겟 인디케이터를 켜거나 끈다.
    public void SetVisible(bool visible)
    {
        if (indicator == null) return;

        indicator.SetActive(visible);
    }
}
