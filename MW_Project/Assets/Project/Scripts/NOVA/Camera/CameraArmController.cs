using UnityEngine;
using UnityEngine.InputSystem;

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

    private Camera camera = null;
    
    private Transform lookTarget;
    private Vector3 velocity = Vector3.zero;

    private Vector2 lookInput;
    private bool isMouseInput;

    private float yaw;
    private float pitch;
    
    private void Awake()
    {
        lookTarget = defaultTarget;

        camera = Camera.main;

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

    private void HandleLookInput()
    {
        yaw += (isMouseInput) ? lookInput.x * mouseSensitivity : lookInput.x * (mouseSensitivity * 5f);
        pitch -= (isMouseInput) ? lookInput.y * mouseSensitivity : lookInput.y * (mouseSensitivity * 5f);

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void HandleTransform()
    {
        Vector3 armPosition = new Vector3(lookTarget.position.x, lookTarget.position.y + eyeHeight, lookTarget.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, armPosition, ref velocity, smoothFollowSpeed);

        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void HandleArmDistance()
    {
        if (!camera) return;

        camera.transform.localPosition = new Vector3(0f, 0f, -distance);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();

        if (context.control.device is Mouse) isMouseInput = true;
        else isMouseInput = false;
    }
    
    public void SetLookTarget(Transform target)
    {
        lookTarget = target;
    }

    public void ResetLookTarget()
    {
        lookTarget = defaultTarget;
    }
    
    public Transform GetLookTarget()
    {
        return lookTarget;
    }
    
    private float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f; return angle;
    }
}
