using UnityEditor;
using UnityEngine;
using UnityXOPS;

[CustomEditor(typeof(Human))]
public class HumanEditor : PointEditor
{
    private SerializedProperty _humanMotor;
    private SerializedProperty _humanVisual;
    private SerializedProperty _humanCamera;
    
    private SerializedProperty _humanDataSO;
    private SerializedProperty _humanTypeSO;
    private SerializedProperty _humanVisualSO;
    private SerializedProperty _hp;
    private SerializedProperty _alive;
    
    protected override void OnEnable()
    {
        base.OnEnable();
        
        _humanMotor = serializedObject.FindProperty("humanMotor");
        _humanVisual = serializedObject.FindProperty("humanVisual");
        _humanCamera = serializedObject.FindProperty("humanCamera");
        _humanDataSO = serializedObject.FindProperty("humanDataSO");
        _humanTypeSO = serializedObject.FindProperty("humanTypeSO");
        _humanVisualSO = serializedObject.FindProperty("humanVisualSO");
        _hp = serializedObject.FindProperty("hp");
        _alive = serializedObject.FindProperty("alive");
    }

    protected override void DrawProperties()
    {
        EditorGUILayout.LabelField("Editor Property", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_humanMotor);
        EditorGUILayout.PropertyField(_humanVisual);
        EditorGUILayout.PropertyField(_humanCamera);
        
        EditorGUILayout.Space();
        
        base.DrawProperties();
        
        EditorGUILayout.LabelField("Scriptable Object", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(_humanDataSO);
            EditorGUILayout.PropertyField(_humanTypeSO);
            EditorGUILayout.PropertyField(_humanVisualSO);
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(_hp);
            EditorGUILayout.PropertyField(_alive);
        }
    }
}