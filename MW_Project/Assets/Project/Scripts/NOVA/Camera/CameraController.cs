using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("카메라 시점 타겟")]
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [Tooltip("카메라 오프셋")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -5f);
    [Tooltip("타겟을 따라가는 속도")]
    [SerializeField] private float followSpeed = 10f;

    [Header("Look Settings")]
    [Tooltip("타겟을 바라보는 높이")]
    [SerializeField] private float lookHeight = 1.5f;
    [Tooltip("바라보는 속도")]
    [SerializeField] private float lookSpeed = 10f;

    private void LateUpdate()
    {
        if (target == null) return;

        FollowTarget();
    }

    private void FollowTarget()
    {
        Vector3 targetPosition = target.position + offset;

        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
