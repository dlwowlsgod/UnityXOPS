using UnityEngine;
using UnityEditor;
using UnityXOPS;

[CustomEditor(typeof(HumanArmParameterSO))]
public class HumanArmParameterSOEditor : Editor
{
    private SerializedProperty _armModelsLeft;
    private SerializedProperty _armModelsRight;
    
    private HumanParameterSO _humanParameterSO;

    private void OnEnable()
    {
        _armModelsLeft = serializedObject.FindProperty("armModelsLeft");
        _armModelsRight = serializedObject.FindProperty("armModelsRight");
        
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
        
        int armNameLength = _humanParameterSO.armName.Length;

        if (armNameLength == 0)
        {
            EditorGUILayout.HelpBox("No arm name found. Do you add names on HumanParameterSO?", MessageType.Info);
            return;
        }
        
        _armModelsLeft.arraySize = armNameLength;
        _armModelsRight.arraySize = armNameLength;
        
        EditorGUILayout.LabelField("Left Arms", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        for (int i = 0; i < armNameLength; i++)
        {
            EditorGUILayout.BeginHorizontal();
            
            // armName의 값을 라벨로 표시
            string armNameLabel = _humanParameterSO.armName[i];
            EditorGUILayout.LabelField($"{armNameLabel}", GUILayout.Width(150));
            
            // Left Arm 인풋 필드
            SerializedProperty leftElement = _armModelsLeft.GetArrayElementAtIndex(i);
            EditorGUILayout.PropertyField(leftElement, GUIContent.none);
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUI.indentLevel--;
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Right Arms", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        for (int i = 0; i < armNameLength; i++)
        {
            EditorGUILayout.BeginHorizontal();
            
            // armName의 값을 라벨로 표시
            string armNameLabel = _humanParameterSO.armName[i];
            EditorGUILayout.LabelField($"{armNameLabel}", GUILayout.Width(150));
            
            // Right Arm 인풋 필드
            SerializedProperty rightElement = _armModelsRight.GetArrayElementAtIndex(i);
            EditorGUILayout.PropertyField(rightElement, GUIContent.none);
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUI.indentLevel--;

        serializedObject.ApplyModifiedProperties();
    }

}