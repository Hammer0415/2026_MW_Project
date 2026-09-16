#if UNITY_EDITOR

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Unity 6 Collider3DEditorBase.OnEnable 이 target == null 인 인스펙터를 그리면
// SerializedObjectNotCreatableException 이 난다. 기본 콜라이더 에디터를 감싸서 막는다.
internal abstract class SafeColliderEditorBase : Editor
{
    private Editor inner;

    protected abstract string InnerEditorTypeName { get; }

    private void OnEnable()
    {
        RebuildInnerEditor();
    }

    private void OnDisable()
    {
        DestroyInnerEditor();
    }

    public override void OnInspectorGUI()
    {
        if (!HasValidTargets())
            return;

        if (inner == null)
            RebuildInnerEditor();

        if (inner != null)
            inner.OnInspectorGUI();
        else
            DrawDefaultInspector();
    }

    private void OnSceneGUI()
    {
        if (inner == null || !HasValidTargets())
            return;

        var method = inner.GetType().GetMethod(
            "OnSceneGUI",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        method?.Invoke(inner, null);
    }

    private void RebuildInnerEditor()
    {
        DestroyInnerEditor();

        if (!HasValidTargets())
            return;

        var editorType = FindEditorType(InnerEditorTypeName);
        if (editorType == null)
            return;

        try
        {
            inner = CreateEditor(targets, editorType);
        }
        catch (Exception)
        {
            inner = null;
        }
    }

    private void DestroyInnerEditor()
    {
        if (inner == null)
            return;

        DestroyImmediate(inner);
        inner = null;
    }

    private bool HasValidTargets()
    {
        if (targets == null || targets.Length == 0)
            return false;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
                return false;
        }

        return true;
    }

    private static Type FindEditorType(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(typeName);
            if (type != null)
                return type;
        }

        return null;
    }
}

[CustomEditor(typeof(CapsuleCollider))]
[CanEditMultipleObjects]
internal sealed class SafeCapsuleColliderEditor : SafeColliderEditorBase
{
    protected override string InnerEditorTypeName => "UnityEditor.CapsuleColliderEditor";
}

[CustomEditor(typeof(BoxCollider))]
[CanEditMultipleObjects]
internal sealed class SafeBoxColliderEditor : SafeColliderEditorBase
{
    protected override string InnerEditorTypeName => "UnityEditor.BoxColliderEditor";
}

[CustomEditor(typeof(SphereCollider))]
[CanEditMultipleObjects]
internal sealed class SafeSphereColliderEditor : SafeColliderEditorBase
{
    protected override string InnerEditorTypeName => "UnityEditor.SphereColliderEditor";
}

[CustomEditor(typeof(MeshCollider))]
[CanEditMultipleObjects]
internal sealed class SafeMeshColliderEditor : SafeColliderEditorBase
{
    protected override string InnerEditorTypeName => "UnityEditor.MeshColliderEditor";
}

#endif
