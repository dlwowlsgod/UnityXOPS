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
    private SerializedProperty _humanScale;
    private SerializedProperty _armRootPosition;
    private SerializedProperty _armRootScale;
    private SerializedProperty _legRootPosition;
    private SerializedProperty _legRootScale;
    private SerializedProperty _headColliderCenter;
    private SerializedProperty _headColliderSize;
    private SerializedProperty _bodyColliderCenter;
    private SerializedProperty _bodyColliderSize;
    private SerializedProperty _legColliderCenter;
    private SerializedProperty _legColliderSize;
    private SerializedProperty _controllerHeight;
    private SerializedProperty _controllerRadius;
    private SerializedProperty _slopeAngle;
    private SerializedProperty _stepHeight;
    private SerializedProperty _viewportHeight;
    private SerializedProperty _tpsCameraDistance;
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
        _humanScale = serializedObject.FindProperty("humanScale");
        _armRootPosition = serializedObject.FindProperty("armRootPosition");
        _armRootScale = serializedObject.FindProperty("armRootScale");
        _legRootPosition = serializedObject.FindProperty("legRootPosition");
        _legRootScale = serializedObject.FindProperty("legRootScale");
        _headColliderCenter = serializedObject.FindProperty("headColliderCenter");
        _headColliderSize = serializedObject.FindProperty("headColliderSize");
        _bodyColliderCenter = serializedObject.FindProperty("bodyColliderCenter");
        _bodyColliderSize = serializedObject.FindProperty("bodyColliderSize");
        _legColliderCenter = serializedObject.FindProperty("legColliderCenter");
        _legColliderSize = serializedObject.FindProperty("legColliderSize");
        _controllerHeight = serializedObject.FindProperty("controllerHeight");
        _controllerRadius = serializedObject.FindProperty("controllerRadius");
        _slopeAngle = serializedObject.FindProperty("slopeAngle");
        _stepHeight = serializedObject.FindProperty("stepHeight");
        _viewportHeight = serializedObject.FindProperty("viewportHeight");
        _tpsCameraDistance = serializedObject.FindProperty("tpsCameraDistance");
        _humanDataParameterSOs = serializedObject.FindProperty("humanDataParameterSOs");
        _humanVisualParameterSOs = serializedObject.FindProperty("humanVisualParameterSOs");
        _humanTypeParameterSOs = serializedObject.FindProperty("humanTypeParameterSOs");
        _humanAIParameterSOs = serializedObject.FindProperty("humanAIParameterSOs");
        _humanArmParameterSOs = serializedObject.FindProperty("humanArmParameterSOs");
        _humanLegParameterSOs = serializedObject.FindProperty("humanLegParameterSOs");
        
        _armRootScale.vector3Value = new Vector3(1, 1, 1);
        _legRootScale.vector3Value = new Vector3(1, 1, 1);
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

        
        EditorGUILayout.LabelField("Arm & Leg Root Position Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        EditorGUILayout.PropertyField(_humanScale);
        EditorGUILayout.PropertyField(_armRootPosition);
        EditorGUILayout.PropertyField(_armRootScale);
        EditorGUILayout.PropertyField(_legRootPosition);
        EditorGUILayout.PropertyField(_legRootScale);
        EditorGUI.indentLevel--;
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Collider Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        EditorGUILayout.PropertyField(_headColliderCenter);
        EditorGUILayout.PropertyField(_headColliderSize);
        EditorGUILayout.PropertyField(_bodyColliderCenter);
        EditorGUILayout.PropertyField(_bodyColliderSize);
        EditorGUILayout.PropertyField(_legColliderCenter);
        EditorGUILayout.PropertyField(_legColliderSize);
        EditorGUI.indentLevel--;
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Controller Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        EditorGUILayout.PropertyField(_controllerHeight);
        EditorGUILayout.PropertyField(_controllerRadius);
        EditorGUILayout.PropertyField(_slopeAngle);
        EditorGUILayout.PropertyField(_stepHeight);
        EditorGUI.indentLevel--;
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Camera Settings", EditorStyles.boldLabel);
        
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(_viewportHeight);
        EditorGUILayout.PropertyField(_tpsCameraDistance);
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