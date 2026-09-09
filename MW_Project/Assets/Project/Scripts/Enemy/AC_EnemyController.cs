using UnityEngine;

public class AC_EnemyController : MonoBehaviour
{
    //=====Components=====//
    private Rigidbody rb = null;
    private CapsuleCollider collider = null;
    //====================//

    private void Awake()
    {
        InitSetup();
    }

    //======================================//
    private void InitSetup()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (collider == null) collider = GetComponent<CapsuleCollider>();

        if (rb == null) Debug.LogError("Rigidbody를 찾을 수 없습니다.");
        if (collider == null) Debug.LogError("CapsuleCollider를 찾을 수 없습니다.");
    }
    //======================================//
}
