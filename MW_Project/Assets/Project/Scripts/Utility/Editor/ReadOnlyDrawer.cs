#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(
        Rect position,
        SerializedProperty property,
        GUIContent label)
    {
        bool previousGUIEnabled = GUI.enabled;

        GUI.enabled = false;

        EditorGUI.PropertyField(
            position,
            property,
            label,
            true
        );

        GUI.enabled = previousGUIEnabled;
    }


    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(
            property,
            label,
            true
        );
    }
}

#endif
