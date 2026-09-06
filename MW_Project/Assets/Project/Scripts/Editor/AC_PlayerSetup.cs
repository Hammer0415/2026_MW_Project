using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class AC_PlayerSetupMenu
{
    [MenuItem("GameObject/Create Player", false, 10)]
    public static void CreatePlayerObject()
    {
        //=====Components=====//
        GameObject playerObj = new GameObject("Player");
        CapsuleCollider collider = playerObj.AddComponent<CapsuleCollider>();
        Rigidbody rb = playerObj.AddComponent<Rigidbody>();
        PlayerInput playerInput = playerObj.AddComponent<PlayerInput>();
        AC_PlayerController controller = playerObj.AddComponent<AC_PlayerController>();
        AC_CameraController cameraController = playerObj.AddComponent<AC_CameraController>();
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
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Player Input Setup
        playerInput.camera = Camera.main;
        playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

        // Player Object Setup
        Selection.activeGameObject = playerObj;
        Undo.RegisterCreatedObjectUndo(playerObj, "Create Player");

        // Player Tag, Layer Setup
        playerObj.tag = "Player";
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1) playerObj.layer = playerLayer;
        else Debug.LogWarning("[AC_PlayerSetupMenu] 'Player' 레이어가 프로젝트에 등록되어 있지 않아 Default 레이어로 설정됩니다.");
    }
}
