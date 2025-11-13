using UnityEngine;
using UnityEditor;
using UnityXOPS;

[CustomEditor(typeof(HumanArmParameterSO))]
public class HumanArmParameterSOEditor : Editor
{
    private SerializedProperty _armModelsLeft;
    private SerializedProperty _armModelsRight;

    private void OnEnable()
    {
        _armModelsLeft = serializedObject.FindProperty("armModelsLeft");
        _armModelsRight = serializedObject.FindProperty("armModelsRight");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        int prevLeftSize = _armModelsLeft.arraySize;
        int prevRightSize = _armModelsRight.arraySize;
        
        _armModelsLeft.isExpanded = true;
        _armModelsRight.isExpanded = true;
        
        EditorGUILayout.PropertyField(_armModelsLeft, true);
        EditorGUILayout.PropertyField(_armModelsRight, true);
        
        if (_armModelsLeft.arraySize != prevLeftSize)
        {
            _armModelsRight.arraySize = _armModelsLeft.arraySize;
        }
        else if (_armModelsRight.arraySize != prevRightSize)
        {
            _armModelsLeft.arraySize = _armModelsRight.arraySize;
        }

        serializedObject.ApplyModifiedProperties();
    }

}