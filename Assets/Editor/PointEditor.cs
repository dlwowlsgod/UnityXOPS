using UnityEditor;
using UnityEngine;
using UnityXOPS;

[CustomEditor(typeof(Point))]
public class PointEditor : Editor
{
    protected SerializedProperty _param0;
    protected SerializedProperty _param1;
    protected SerializedProperty _param2;

    protected virtual void OnEnable()
    {
        _param0 = serializedObject.FindProperty("param0");
        _param1 = serializedObject.FindProperty("param1");
        _param2 = serializedObject.FindProperty("param2");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        DrawProperties();

        serializedObject.ApplyModifiedProperties();
    }

    protected virtual void DrawProperties()
    {
        DrawPointProperty();
    }

    protected void DrawPointProperty()
    {
        EditorGUILayout.LabelField("Params", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(_param0);
            EditorGUILayout.PropertyField(_param1);
            EditorGUILayout.PropertyField(_param2);
        }
        EditorGUILayout.Space();
    }
}