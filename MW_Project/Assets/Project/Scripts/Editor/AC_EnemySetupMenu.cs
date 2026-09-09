using UnityEditor;
using UnityEngine;

public class AC_EnemySetupMenu
{
    [MenuItem("GameObject/Create Enemy", false, 10)]
    public static void CreateEnemyObject()
    {
        //=====Components=====//
        GameObject enemyObj = new GameObject("Enemy");
        CapsuleCollider collider = enemyObj.AddComponent<CapsuleCollider>();
        Rigidbody rb = enemyObj.AddComponent<Rigidbody>();
        AC_EnemyController enemyController = enemyObj.AddComponent<AC_EnemyController>();
        AC_Enemy enemy = enemyObj.AddComponent<AC_Enemy>();
        //====================//

        // Collider Setup
        collider.radius = 0.5f;
        collider.height = 2.0f;
        collider.direction = 1;
        collider.center = new Vector3(0, 1.0f, 0);

        // Rigidbody Setup
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

        // Player Tag, Layer Setup
        enemyObj.tag = "Enemy";
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1) enemyObj.layer = enemyLayer;
        else Debug.LogWarning("[AC_EnemySetupMenu] 'Enemy' 레이어가 프로젝트에 등록되어 있지 않아 Default 레이어로 설정됩니다.");
    }
}
