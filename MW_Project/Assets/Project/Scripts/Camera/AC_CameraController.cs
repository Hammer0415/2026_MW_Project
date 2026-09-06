using UnityEngine;
using UnityEngine.InputSystem;

public class AC_CameraController : MonoBehaviour
{
    //=====Components=====//
    private MG_Game gameManager = null;
    //====================//

    //=====CameraValues=====//
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

    private Vector3 currentVelocity = Vector3.zero;
    //======================//

    //=====InputValues=====//
    private Vector2 lookInput = Vector2.zero;
    private float yaw = 0.0f;
    private float pitch = 0.0f;
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

        // Get Mouse Input
        yaw += lookInput.x * mouseSensitivity;
        pitch -= lookInput.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);

        // Rotation & Position
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0.0f);
        Vector3 playerPivot = target.position + targetOffset;
        Vector3 targetPosition = playerPivot - (targetRotation * Vector3.forward * distance);

        // Camera Smooth
        transform.LookAt(playerPivot);
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, positionSmoothTime);
    }

    //======================================//
    private void StartInitSetup()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
        }

        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;

        if (!gameManager) gameManager = MG_Game.Instance;

        if (gameManager) gameManager.CursorOnOff();
    }
    //======================================//
    
    #region Input Value Functions
    //======================================//
    public void OnLook(InputAction.CallbackContext context)
    {
        if (context.canceled) lookInput = Vector2.zero;
        else lookInput = context.ReadValue<Vector2>();
    }
    //======================================//
    #endregion
}
