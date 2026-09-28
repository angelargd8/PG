#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(XRDropdownScrollRect))]
[CanEditMultipleObjects]
public sealed class XRDropdownScrollRectEditor : ScrollRectEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("thumbstickScrollSpeed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("thumbstickDeadzone"));
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
