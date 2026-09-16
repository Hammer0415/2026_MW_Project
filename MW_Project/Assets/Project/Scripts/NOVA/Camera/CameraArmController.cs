using UnityEngine;
using UnityEngine.InputSystem;

[AddComponentMenu("NOVA/Camera/Camera Arm Controller")]
public class CameraArmController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("바라볼 타겟")]
    [SerializeField] private Transform defaultTarget;

    [Header("Look Settings")]
    [Tooltip("마우스 민감도")]
    [SerializeField] private float mouseSensitivity = 0.3f;
    [Tooltip("회전 속도")]
    [SerializeField] private float rotationSpeed = 20f;
    [Tooltip("게임패드 시야 조작 배율")]
    [SerializeField] private float gamepadLookMultiplier = 5f;

    [Header("Arm Settings")]
    [Tooltip("CameraArm 길이")]
    [SerializeField] private float distance = 6f;
    [Tooltip("눈 높이")]
    [SerializeField] private float eyeHeight = 1.5f;

    [Header("Follow Settings")]
    [Tooltip("타겟을 따라가는 속도(작을 수록 빠름)")]
    [SerializeField] private float smoothFollowSpeed = 0.12f;

    [Header("Vertical Limits")]
    [Tooltip("시아 각 제한 최소")]
    [SerializeField] private float minPitch = -10f;
    [Tooltip("시아 각 제한 최대")]
    [SerializeField] private float maxPitch = 65f;

    private Camera cameraComponent;
    private Transform lookTarget;
    private Vector3 velocity = Vector3.zero;
    private Vector2 lookInput;
    private bool isMouseInput;
    private float yaw;
    private float pitch;

    private void Awake()
    {
        lookTarget = defaultTarget;
        cameraComponent = Camera.main;

        Vector3 currentRotation = transform.eulerAngles;

        yaw = currentRotation.y;
        pitch = NormalizeAngle(currentRotation.x);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void Update()
    {
        HandleLookInput();
        HandleArmDistance();
    }

    private void LateUpdate()
    {
        HandleTransform();
    }

    // 마우스/패드 입력으로 카메라 암의 yaw/pitch를 갱신한다.
    private void HandleLookInput()
    {
        float lookScale = isMouseInput ? mouseSensitivity : mouseSensitivity * gamepadLookMultiplier;

        yaw += lookInput.x * lookScale;
        pitch -= lookInput.y * lookScale;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    // 타겟 위치를 부드럽게 따라가며 암을 회전시킨다.
    private void HandleTransform()
    {
        if (!lookTarget) return;

        Vector3 armPosition = new Vector3(lookTarget.position.x, lookTarget.position.y + eyeHeight, lookTarget.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, armPosition, ref velocity, smoothFollowSpeed);

        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    // 카메라 로컬 위치를 암 길이에 맞게 유지한다.
    private void HandleArmDistance()
    {
        if (!cameraComponent) return;

        cameraComponent.transform.localPosition = new Vector3(0f, 0f, -distance);
    }

    // 시야 조작 입력을 받는다.
    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
        isMouseInput = context.control.device is Mouse;
    }

    // 카메라가 따라갈 대상을 바꾼다.
    public void SetLookTarget(Transform target)
    {
        lookTarget = target;
    }

    // 기본 타겟으로 되돌린다.
    public void ResetLookTarget()
    {
        lookTarget = defaultTarget;
    }

    public Transform GetLookTarget()
    {
        return lookTarget;
    }

    // 0~360 각도를 -180~180으로 변환한다.
    private float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;

        return angle;
    }
}
