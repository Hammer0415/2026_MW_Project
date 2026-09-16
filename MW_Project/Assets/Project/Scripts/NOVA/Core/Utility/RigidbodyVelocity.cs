using UnityEngine;

public static class RigidbodyVelocity
{
    // Rigidbody의 현재 속도를 반환한다.
    public static Vector3 Get(Rigidbody rigidbody)
    {
        if (!rigidbody) return Vector3.zero;

#if UNITY_6000_0_OR_NEWER
        return rigidbody.linearVelocity;
#else
        return rigidbody.velocity;
#endif
    }

    // Rigidbody 속도를 설정한다.
    public static void Set(Rigidbody rigidbody, Vector3 velocity)
    {
        if (!rigidbody) return;

#if UNITY_6000_0_OR_NEWER
        rigidbody.linearVelocity = velocity;
#else
        rigidbody.velocity = velocity;
#endif
    }
}
