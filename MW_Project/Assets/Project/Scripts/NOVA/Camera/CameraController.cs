using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    [Tooltip("Camera Arm")]
    [SerializeField] private Transform cameraArm = null;

    [Header("Target")]
    [Tooltip("플레이어의 TargetingComponent")]
    [SerializeField] private TargetingComponent targetingComponent = null;
    [Tooltip("타겟을 바라보는 속도")]
    [SerializeField] private float lookAtSpeed = 10f;

    private void Awake()
    {
        if (!cameraArm) cameraArm = transform.root;

        CursorController.Instance.CursorOnOff();
    }

    private void LateUpdate()
    {
        if (CursorController.Instance.IsLocked()) return;

        HandleLook();
    }

    private void HandleLook()
    {
        if (cameraArm == null) return;

        if (targetingComponent && targetingComponent.IsTargeting)
        {
            HandleLookAtTarget();
        }
        else
        {
            HandleLookAtArm();
        }
    }

    private void HandleLookAtTarget()
    {
        NovaCharacter target = targetingComponent.CurrentTarget;

        float lockOnFocusRatio = targetingComponent.LockOnFocusRatio;

        if (!target) return;

        Vector3 playerPivot = cameraArm.position;
        Vector3 targetPosition = target.transform.position + Vector3.up * targetingComponent.TargetHeight;
        Vector3 lockOnFocusPoint = Vector3.Lerp(playerPivot, targetPosition, lockOnFocusRatio);

        Vector3 direction = lockOnFocusPoint - transform.position;

        if (direction.sqrMagnitude <= 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        targetRotation = Quaternion.Euler(targetRotation.eulerAngles.x, targetRotation.eulerAngles.y, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookAtSpeed * Time.deltaTime);
    }

    private void HandleLookAtArm()
    {
        Quaternion targetRotation = Quaternion.Euler(cameraArm.eulerAngles.x, cameraArm.eulerAngles.y, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookAtSpeed * Time.deltaTime);
    }
}
