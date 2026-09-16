#if UNITY_EDITOR

using UnityEditor;
using UnityEngine.InputSystem;

[InitializeOnLoad]
internal static class NovaPlayerInputAssetFix
{
    private const string SessionKey = "NovaPlayerInputAssetFix.Ran";
    private const string ActionsPath = "Assets/Project/InputSystems/Player Input.inputactions";

    static NovaPlayerInputAssetFix()
    {
        EditorApplication.delayCall += Run;
    }

    // PlayerInput 인스펙터가 액션 에셋 서브오브젝트를 못 찾을 때
    // SerializedObjectNotCreatableException 이 나지 않도록 한 번 재임포트한다.
    static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);

        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
        if (asset != null)
            return;

        AssetDatabase.ImportAsset(ActionsPath, ImportAssetOptions.ForceUpdate);
    }
}

#endif
