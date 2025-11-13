using UnityEditor;
using UnityXOPS;
using UnityEngine;

[CustomEditor(typeof(HumanLegParameterSO))]
public class HumanLegParameterSOEditor : Editor
{
    private SerializedProperty _legModels;
    
    private void OnEnable()
    {
        _legModels = serializedObject.FindProperty("legModels");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        _legModels.isExpanded = true;
        
        EditorGUILayout.PropertyField(_legModels, true);
        
        serializedObject.ApplyModifiedProperties();
    }
}