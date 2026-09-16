using UnityEngine;
using UnityEngine.InputSystem;

[AddComponentMenu("NOVA/Camera/Camera Arm Controller")]
[DefaultExecutionOrder(-50)]
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

    [Header("Collision Settings")]
    [Tooltip("카메라 충돌을 감지할 레이어")]
    [SerializeField] private LayerMask collisionLayer;
    [Tooltip("카메라 충돌 감지 반지름")]
    [SerializeField] private float collisionRadius = 0.2f;
    [Tooltip("벽에서 카메라를 띄울 여유 거리")]
    [SerializeField] private float collisionOffset = 0.15f;
    [Tooltip("벽과 충돌했을 때 허용하는 최소 거리")]
    [SerializeField] private float minDistance = 0.4f;
    [Tooltip("충돌이 끝난 뒤 원래 거리로 돌아가는 속도")]
    [SerializeField] private float collisionReturnSpeed = 12f;

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
    private float currentDistance;

    private void Awake()
    {
        lookTarget = defaultTarget;
        cameraComponent = Camera.main;
        currentDistance = distance;

        if (collisionLayer.value == 0)
            collisionLayer = LayerMask.GetMask("Wall");

        Vector3 currentRotation = transform.eulerAngles;

        yaw = currentRotation.y;
        pitch = NormalizeAngle(currentRotation.x);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void Update()
    {
        HandleLookInput();
    }

    private void LateUpdate()
    {
        HandleTransform();
        HandleArmDistance();
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

    // 카메라 로컬 위치를 암 길이에 맞게 유지하되, 벽과 충돌하면 거리를 줄인다.
    private void HandleArmDistance()
    {
        if (!cameraComponent) return;

        float desiredDistance = GetCollisionAdjustedDistance();

        if (desiredDistance < currentDistance)
            currentDistance = desiredDistance;
        else
            currentDistance = Mathf.Lerp(currentDistance, desiredDistance, collisionReturnSpeed * Time.deltaTime);

        cameraComponent.transform.localPosition = new Vector3(0f, 0f, -currentDistance);
    }

    // 암 피벗에서 카메라 방향으로 SphereCast 해서 벽 안쪽으로 들어가지 않는 거리를 구한다.
    private float GetCollisionAdjustedDistance()
    {
        float maxDistance = Mathf.Max(distance, 0f);
        float clampedMinDistance = Mathf.Clamp(minDistance, 0f, maxDistance);
        Vector3 origin = transform.position;
        Vector3 direction = -transform.forward;
        float radius = Mathf.Max(collisionRadius, 0.01f);

        if (Physics.SphereCast(origin, radius, direction, out RaycastHit hit, maxDistance, collisionLayer, QueryTriggerInteraction.Ignore))
            return Mathf.Clamp(hit.distance - collisionOffset, clampedMinDistance, maxDistance);

        return maxDistance;
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
        SetLookTarget(target, false);
    }

    // 카메라가 따라갈 대상을 바꾸고, 필요하면 즉시 그 위치로 붙인다.
    public void SetLookTarget(Transform target, bool snap)
    {
        lookTarget = target;

        if (!snap || !lookTarget) return;

        velocity = Vector3.zero;
        transform.position = new Vector3(lookTarget.position.x, lookTarget.position.y + eyeHeight, lookTarget.position.z);
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
