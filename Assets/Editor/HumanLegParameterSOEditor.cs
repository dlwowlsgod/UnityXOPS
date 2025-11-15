using UnityEditor;
using UnityXOPS;
using UnityEngine;

[CustomEditor(typeof(HumanLegParameterSO))]
public class HumanLegParameterSOEditor : Editor
{
    private SerializedProperty _legModels;
    
    private HumanParameterSO _humanParameterSO;
    
    private void OnEnable()
    {
        _legModels = serializedObject.FindProperty("legModels");
        
        if (!InitSceneObserver.IsInitScene)
        {
            Debug.LogWarning("Open init scene first.");
            return;
        }
        
        if (ParameterManager.Instance.HumanParameterSO != null)
        {
            _humanParameterSO = ParameterManager.Instance.HumanParameterSO;
        }
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        int legNameLength = _humanParameterSO.legName.Length;
        
        if (legNameLength == 0)
        {
            EditorGUILayout.HelpBox("No leg name found. Do you add names on HumanParameterSO?", MessageType.Info);
            return;
        }
        
        _legModels.arraySize = legNameLength;
        
        EditorGUILayout.LabelField("Legs", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        for (int i = 0; i < legNameLength; i++)
        {
            EditorGUILayout.BeginHorizontal();
            
            string legNameLabel = _humanParameterSO.legName[i];
            EditorGUILayout.LabelField($"{legNameLabel}", GUILayout.Width(150));
            
            SerializedProperty legElement = _legModels.GetArrayElementAtIndex(i);
            EditorGUILayout.PropertyField(legElement, GUIContent.none);
            
            EditorGUILayout.EndHorizontal();
        }
        EditorGUI.indentLevel--;
        
        serializedObject.ApplyModifiedProperties();
    }
}