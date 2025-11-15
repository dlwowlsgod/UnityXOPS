using UnityEditor;
using UnityEngine;
using UnityXOPS;

[CustomEditor(typeof(HumanParameterSO))]
public class HumanParameterSOEditor : Editor
{
    private SerializedProperty _armName;
    private SerializedProperty _legName;
    private SerializedProperty _walkAnimationIndices;
    private SerializedProperty _runAnimationIndices;
    private SerializedProperty _armRootPosition;
    private SerializedProperty _legRootPosition;
    private SerializedProperty _humanDataParameterSOs;
    private SerializedProperty _humanVisualParameterSOs;
    private SerializedProperty _humanTypeParameterSOs;
    private SerializedProperty _humanAIParameterSOs;
    private SerializedProperty _humanArmParameterSOs;
    private SerializedProperty _humanLegParameterSOs;
    
    private void OnEnable()
    {
        _armName = serializedObject.FindProperty("armName");
        _legName = serializedObject.FindProperty("legName");
        _walkAnimationIndices = serializedObject.FindProperty("walkAnimationIndices");
        _runAnimationIndices = serializedObject.FindProperty("runAnimationIndices");
        _armRootPosition = serializedObject.FindProperty("armRootPosition");
        _legRootPosition = serializedObject.FindProperty("legRootPosition");
        _humanDataParameterSOs = serializedObject.FindProperty("humanDataParameterSOs");
        _humanVisualParameterSOs = serializedObject.FindProperty("humanVisualParameterSOs");
        _humanTypeParameterSOs = serializedObject.FindProperty("humanTypeParameterSOs");
        _humanAIParameterSOs = serializedObject.FindProperty("humanAIParameterSOs");
        _humanArmParameterSOs = serializedObject.FindProperty("humanArmParameterSOs");
        _humanLegParameterSOs = serializedObject.FindProperty("humanLegParameterSOs");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.LabelField("Arm&Leg Naming Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        _armName.isExpanded = true;
        _legName.isExpanded = true;
        
        EditorGUILayout.PropertyField(_armName);
        EditorGUILayout.PropertyField(_legName);
        EditorGUI.indentLevel--;
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Animation Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        _walkAnimationIndices.isExpanded = true;
        _runAnimationIndices.isExpanded = true;
        
        EditorGUILayout.PropertyField(_walkAnimationIndices);
        EditorGUILayout.PropertyField(_runAnimationIndices);
        EditorGUI.indentLevel--;
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Arm&Leg Root Position Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        EditorGUILayout.PropertyField(_armRootPosition);
        EditorGUILayout.PropertyField(_legRootPosition);
        EditorGUI.indentLevel--;
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        _humanDataParameterSOs.isExpanded = true;
        _humanVisualParameterSOs.isExpanded = true;
        _humanTypeParameterSOs.isExpanded = true;
        _humanAIParameterSOs.isExpanded = true;
        _humanArmParameterSOs.isExpanded = true;
        _humanLegParameterSOs.isExpanded = true;
        
        EditorGUILayout.PropertyField(_humanDataParameterSOs, new GUIContent("Human Data Parameter"));
        EditorGUILayout.PropertyField(_humanVisualParameterSOs, new GUIContent("Human Visual Parameter"));
        EditorGUILayout.PropertyField(_humanTypeParameterSOs, new GUIContent("Human Type Parameter"));
        EditorGUILayout.PropertyField(_humanAIParameterSOs, new GUIContent("Human AI Parameter"));
        EditorGUILayout.PropertyField(_humanArmParameterSOs, new GUIContent("Human Arm Parameter"));
        EditorGUILayout.PropertyField(_humanLegParameterSOs, new GUIContent("Human Leg Parameter"));
        EditorGUI.indentLevel--;
        
        serializedObject.ApplyModifiedProperties();
    }
}