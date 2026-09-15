using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Zoom Settings")]
    [Tooltip("기본 FOV")]
    [SerializeField] private float defaultFOV = 60f;
    [Tooltip("최소 FOV")]
    [SerializeField] private float minFOV = 30f;
    [Tooltip("최대 FOV")]
    [SerializeField] private float maxFOV = 80f;
    [Tooltip("마우스 휠 줌 감도")]
    [SerializeField] private float zoomSensitivity = 5f;
    [Tooltip("FOV 변화 부드러움")]
    [SerializeField] private float zoomSmoothSpeed = 10f;

    private Camera mainCamera = null;

    private float targetFOV;

    private float zoomInput;
    private bool zoomIsMouse;

    private void Awake()
    {
        if (!cameraArm) cameraArm = transform.root;

        mainCamera = Camera.main;

        targetFOV = defaultFOV;
        mainCamera.fieldOfView = defaultFOV;
    }

    private void Start()
    {
        CursorController.Instance.CursorOnOff();
    }

    private void Update()
    {
        HandleZoom();
    }

    private void LateUpdate()
    {
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

    private void HandleZoom()
    {
        if (!mainCamera) return;

        if (Mathf.Abs(zoomInput) > 0.01f)
        {
            float zoomAmount;

            if (zoomIsMouse) zoomAmount = zoomInput * zoomSensitivity;
            else zoomAmount = zoomInput * zoomSensitivity * (Time.deltaTime * 10f);

            targetFOV -= zoomAmount;

            targetFOV = Mathf.Clamp(targetFOV, minFOV, maxFOV);
        }

        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, targetFOV, zoomSmoothSpeed * Time.deltaTime);
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

        Vector3 currentEuler = transform.eulerAngles;
        currentEuler.z = 0f;
        transform.rotation = Quaternion.Euler(currentEuler);
    }

    private void HandleLookAtArm()
    {
        Quaternion targetRotation = Quaternion.Euler(cameraArm.eulerAngles.x, cameraArm.eulerAngles.y, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookAtSpeed * Time.deltaTime);
    }

    public void OnZoom(InputAction.CallbackContext context)
    {
        zoomInput = context.ReadValue<float>();

        if (context.control.device is Mouse) zoomIsMouse = true;
        else zoomIsMouse = false;
    }
}
