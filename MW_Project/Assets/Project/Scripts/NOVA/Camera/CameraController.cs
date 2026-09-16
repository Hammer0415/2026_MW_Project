using UnityEngine;
using UnityEngine.InputSystem;

[AddComponentMenu("NOVA/Camera/Camera Controller")]
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

    [Header("Combat Zoom")]
    [Tooltip("락온 중 추가로 당길 FOV 값. 0이면 기존과 같다.")]
    [SerializeField] private float lockOnFOVOffset = 0f;

    private Camera mainCamera;
    private float targetFOV;
    private float zoomInput;
    private bool zoomIsMouse;

    private void Awake()
    {
        if (!cameraArm) cameraArm = transform.root;

        mainCamera = Camera.main;

        targetFOV = defaultFOV;

        if (mainCamera) mainCamera.fieldOfView = defaultFOV;
    }

    private void Start()
    {
        if (CursorController.Instance) CursorController.Instance.CursorOnOff();
    }

    private void Update()
    {
        HandleZoom();
    }

    private void LateUpdate()
    {
        HandleLook();
    }

    // 락온 중이면 타겟을, 아니면 카메라 암을 바라본다.
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

    // 줌 입력으로 목표 FOV를 바꾸고 부드럽게 보간한다.
    private void HandleZoom()
    {
        if (!mainCamera) return;

        if (Mathf.Abs(zoomInput) > 0.01f)
        {
            float zoomAmount = zoomIsMouse
                ? zoomInput * zoomSensitivity
                : zoomInput * zoomSensitivity * (Time.deltaTime * 10f);

            targetFOV -= zoomAmount;
            targetFOV = Mathf.Clamp(targetFOV, minFOV, maxFOV);
        }

        float desiredFOV = targetFOV;

        if (targetingComponent && targetingComponent.IsTargeting)
        {
            desiredFOV = Mathf.Clamp(targetFOV + lockOnFOVOffset, minFOV, maxFOV);
        }

        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, desiredFOV, zoomSmoothSpeed * Time.deltaTime);
    }

    // 플레이어와 락온 타겟 사이를 바라본다.
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

    // 카메라 암의 회전을 따라간다.
    private void HandleLookAtArm()
    {
        Quaternion targetRotation = Quaternion.Euler(cameraArm.eulerAngles.x, cameraArm.eulerAngles.y, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookAtSpeed * Time.deltaTime);
    }

    // 줌 입력을 받는다.
    public void OnZoom(InputAction.CallbackContext context)
    {
        zoomInput = context.ReadValue<float>();
        zoomIsMouse = context.control.device is Mouse;
    }
}
