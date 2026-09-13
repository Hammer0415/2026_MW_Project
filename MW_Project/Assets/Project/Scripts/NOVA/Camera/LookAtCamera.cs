using UnityEngine;
using UnityEngine.InputSystem;

public class LookAtCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("타겟")]
    [SerializeField] private Transform defaultTarget;

    [Header("Look Settings")]
    [Tooltip("마우스 민감도")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [Tooltip("회전 속도")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Vertical Limits")]
    [Tooltip("시아 각 제한 최소")]
    [SerializeField] private float minPitch = -60f;
    [Tooltip("시아 각 제한 최대")]
    [SerializeField] private float maxPitch = 60f;
    
    private Transform lookTarget;
    private float yaw;
    private float pitch;
    private Vector2 lookInput;
    
    private void Awake()
    {
        lookTarget = defaultTarget;

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
        RotateCamera();
    }

    private void HandleLookInput()
    {
        yaw += lookInput.x * mouseSensitivity;
        pitch -= lookInput.y * mouseSensitivity;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }
    
    private void RotateCamera()
    {
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
    
    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
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
    
    public void SetCursorLock(bool locked)
    { 
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
