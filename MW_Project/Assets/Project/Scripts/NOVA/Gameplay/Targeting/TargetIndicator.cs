using UnityEngine;

public class TargetIndicator : MonoBehaviour
{
    [Header("Target Indicator")]
    [Tooltip("타겟팅 표시 UI")]
    [SerializeField] private GameObject indicator = null;

    private Camera mainCamera = null;

    private void Awake()
    {
        mainCamera = Camera.main;

        SetVisible(false);
    }

    public void LateUpdate()
    {
        if (!indicator) return;
        if (!indicator.activeSelf) return;

        transform.rotation = mainCamera.transform.rotation;
    }

    public void SetVisible(bool visible)
    {
        if (indicator == null) return;

        indicator.SetActive(visible);
    }
}
