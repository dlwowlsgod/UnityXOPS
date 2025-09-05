using UnityEngine;
using UnityEditor;

namespace UnityXOPS
{
    /// <summary>
    /// HumanAIParameter의 Inspector를 재정의합니다.
    /// </summary>
    [CustomEditor(typeof(HumanAIParameter))]
    public class HumanAIParameterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("realName"), new GUIContent("Name"));

            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("View", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("lookDistance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("closedViewAngle"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rangedViewAngle"));
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Aiming", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("aimingPointHeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("aimingSphereRadius"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("aimingTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("properAimingTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("accuracyAdjust"));
            
            var fireChanceProp = serializedObject.FindProperty("fireChance");
            EditorGUILayout.Slider(fireChanceProp, 0f, 1f, new GUIContent(fireChanceProp.displayName));
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}