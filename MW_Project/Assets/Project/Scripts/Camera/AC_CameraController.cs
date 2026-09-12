using UnityEngine;
using UnityEngine.InputSystem;

public class AC_CameraController : MonoBehaviour
{
    //=====Components=====//
    private MG_Game gameManager = null;
    private AC_TargetingController targeting = null;
    //====================//

    //=====CameraSetting=====//
    [Header("Camera Settings")]
    [Tooltip("추적할 플레이어 Transform")]
    [SerializeField] private Transform target = null;
    [Tooltip("플레이어 피벗 오프셋 (머리/어깨 높이)")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0.0f, 0.0f, 0.0f);
    [Tooltip("플레이어와의 거리")]
    [SerializeField] private float distance = 0.0f;

    [Header("Sensitivity & Limits")]
    [Tooltip("마우스 감도")]
    [SerializeField] private float mouseSensitivity = 0.0f;
    [Tooltip("카메라 상하 최소 각도")]
    [SerializeField] private float minVerticalAngle = 0.0f;
    [Tooltip("카메라 상하 최대 각도")]
    [SerializeField] private float maxVerticalAngle = 0.0f;

    [Header("Smooth Settings")]
    [Tooltip("위치 추적 부드러움 시간 (작을수록 빠름)")]
    [SerializeField] private float positionSmoothTime = 0.0f;
    [Tooltip("락온 시 카메라 회전 부드러움")]
    [SerializeField] private float lockOnRotationSmoothTime = 0.0f;
    [Tooltip("락온 시 적을 바라보는 높이")]
    [SerializeField] private float lockOnTargetHeight = 0.0f;
    [Tooltip("락온 시 초점 위치 (0 = 플레이어 / 1 = 적)")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float lockOnFocusRatio = 0.0f;

    [Header("Camera Collision")]
    [Tooltip("카메라 충돌을 감지할 레이어")]
    [SerializeField] private LayerMask cameraCollisionLayer;
    [Tooltip("카메라 충돌 감지 반지름")]
    [SerializeField] private float cameraCollisionRadius = 0.0f;
    [Tooltip("벽에서 카메라를 띄울 여유 거리")]
    [SerializeField] private float cameraCollisionOffset = 0.0f;

    private Vector3 currentVelocity = Vector3.zero;
    //======================//

    //=====InputValues=====//
    private Vector2 lookInput = Vector2.zero;
    private float yaw = 0.0f;
    private float pitch = 0.0f;
    private float yawVelocity = 0.0f;
    private float pitchVelocity = 0.0f;
    //=====================//

    private void Start()
    {
        StartInitSetup();
    }

    private void LateUpdate()
    {
        HandleCamera();
    }

    private void HandleCamera()
    {
        if (target == null) return;

        Vector3 playerPivot = target.position + targetOffset;

        if (targeting != null &&
            targeting.IsTargeting &&
            targeting.CurrentTarget != null)
        { HandleLockOnCamera(playerPivot); }
        else { HandleFreeCamera(playerPivot); }
    }

    private void HandleFreeCamera(Vector3 playerPivot)
    {
        yaw += lookInput.x * mouseSensitivity;
        pitch -= lookInput.y * mouseSensitivity;

        pitch = Mathf.Clamp(
            pitch,
            minVerticalAngle,
            maxVerticalAngle
        );

        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0.0f);
        Vector3 targetPosition = GetCameraPosition(playerPivot, targetRotation);
        transform.position = Vector3.SmoothDamp( transform.position, targetPosition, ref currentVelocity, positionSmoothTime);

        transform.LookAt(playerPivot);
    }

    private void HandleLockOnCamera(Vector3 playerPivot)
    {
        Transform currentTarget = targeting.CurrentTarget;

        if (currentTarget == null) return;

        Vector3 targetPosition = currentTarget.position + Vector3.up * lockOnTargetHeight;
        Vector3 targetDirection = targetPosition - playerPivot;
        targetDirection.y = 0.0f;

        if (targetDirection.sqrMagnitude <= 0.01f) return;
        targetDirection.Normalize();

        float targetYaw = Mathf.Atan2( targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;
        Vector3 cameraHorizontalDirection = targetPosition - playerPivot;
        float horizontalDistance = new Vector2(cameraHorizontalDirection.x, cameraHorizontalDirection.z).magnitude;
        float verticalDistance = cameraHorizontalDirection.y;
        float targetPitch = -Mathf.Atan2(verticalDistance, horizontalDistance) * Mathf.Rad2Deg;

        targetPitch = Mathf.Clamp(targetPitch, minVerticalAngle, maxVerticalAngle);

        yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, lockOnRotationSmoothTime);
        pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVelocity, lockOnRotationSmoothTime);
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0.0f);
        Vector3 cameraPosition = GetCameraPosition(playerPivot, targetRotation);
        transform.position = Vector3.SmoothDamp(transform.position, cameraPosition, ref currentVelocity, positionSmoothTime);
        Vector3 lockOnFocusPoint = Vector3.Lerp(playerPivot, targetPosition, lockOnFocusRatio);

        transform.LookAt(lockOnFocusPoint);
    }

    private Vector3 GetCameraPosition(Vector3 playerPivot, Quaternion cameraRotation)
    {
        Vector3 direction = -(cameraRotation * Vector3.forward);
        float targetDistance = distance;

        if (Physics.SphereCast( playerPivot, cameraCollisionRadius, direction, out RaycastHit hit, distance, cameraCollisionLayer, QueryTriggerInteraction.Ignore))
        {
            targetDistance = Mathf.Max( hit.distance - cameraCollisionOffset, 0.0f);
        }

        return playerPivot + direction * targetDistance;
    }

    //======================================//
    private void StartInitSetup()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");

            if (player != null)
            {
                target = player.transform;
            }
        }

        if (targeting == null && target != null)
        {
            targeting = target.GetComponent<AC_TargetingController>();
        }

        if (targeting == null) Debug.LogError("AC_TargetingController를 찾을 수 없습니다.", this);

        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;

        if (!gameManager) gameManager = MG_Game.Instance;

        if (gameManager) gameManager.CursorOnOff();
    }
    //======================================//
    
    //======================================//
    public void OnLook(InputAction.CallbackContext context)
    {
        if (context.canceled) lookInput = Vector2.zero;
        else lookInput = context.ReadValue<Vector2>();
    }
    //======================================//

    private void OnDrawGizmos()
    {
        if (target == null) return;

        Vector3 playerPivot = target.position + targetOffset;
        Vector3 direction = -(transform.rotation * Vector3.forward);
        direction.Normalize();

        Vector3 targetPosition = playerPivot + direction * distance;

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(playerPivot, cameraCollisionRadius);
        Gizmos.DrawWireSphere(targetPosition, cameraCollisionRadius);
        Gizmos.DrawLine(playerPivot, targetPosition);

        if (Physics.SphereCast(playerPivot, cameraCollisionRadius, direction, out RaycastHit hit, distance, cameraCollisionLayer, QueryTriggerInteraction.Ignore))
        {
            Gizmos.color = Color.red;

            Gizmos.DrawWireSphere(hit.point, cameraCollisionRadius);
            Gizmos.DrawLine( hit.point, hit.point + hit.normal * 0.5f);

            Vector3 cameraPosition = playerPivot + direction * Mathf.Max(hit.distance - cameraCollisionOffset, 0.0f);

            Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(cameraPosition, cameraCollisionRadius);
            Gizmos.DrawLine(hit.point, cameraPosition);
        }
    }
}
