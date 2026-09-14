using UnityEngine;

public class LookPlayerCamera : MonoBehaviour
{
    private void Update()
    {
        transform.rotation = Quaternion.LookRotation(Camera.main.transform.position - transform.position);
    }
}
