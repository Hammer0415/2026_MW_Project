using UnityEngine;

[AddComponentMenu("NOVA/UI/Look Player Camera")]
public class LookPlayerCamera : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("바라볼 카메라. 비어 있으면 Main Camera를 사용한다.")]
    [SerializeField] private Camera targetCamera;

    private void Awake()
    {
        if (!targetCamera) targetCamera = Camera.main;
    }

    private void Update()
    {
        if (!targetCamera) return;

        transform.rotation = Quaternion.LookRotation(targetCamera.transform.position - transform.position);
    }
}
