using System;
using System.Diagnostics.SymbolStore;
using UnityEngine;
using UnityEditor;
using UnityXOPS;

[CustomEditor(typeof(MissionParameterSO))]
public class MissionParameterSOEditor : Editor
{
    private SerializedProperty _openingData;
    private SerializedProperty _openingCameraPositionSequence;
    private SerializedProperty _openingCameraRotationSequence;
    private SerializedProperty _openingTextSequence;
    private SerializedProperty _demoData;
    private SerializedProperty _officialMissionParameterSOs;
    private SerializedProperty _addonMissionParameterSOs;

    private void OnEnable()
    {
        _openingData = serializedObject.FindProperty("openingData");
        _openingCameraPositionSequence = serializedObject.FindProperty("openingCameraPositionSequence");
        _openingCameraRotationSequence = serializedObject.FindProperty("openingCameraRotationSequence");
        _openingTextSequence = serializedObject.FindProperty("openingTextSequence");
        _demoData = serializedObject.FindProperty("demoData");
        _officialMissionParameterSOs = serializedObject.FindProperty("officialMissionParameterSOs");
        _addonMissionParameterSOs = serializedObject.FindProperty("addonMissionParameterSOs");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        _openingData.isExpanded = true;
        _openingCameraPositionSequence.isExpanded = true;
        _openingCameraRotationSequence.isExpanded = true;
        _openingTextSequence.isExpanded = true;
        EditorGUILayout.LabelField("Opening Data & Sequences", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_openingData);
        EditorGUILayout.PropertyField(_openingCameraPositionSequence, new GUIContent("Camera Position Sequence"));
        EditorGUILayout.PropertyField(_openingCameraRotationSequence, new GUIContent("Camera Rotation Sequence"));
        EditorGUILayout.PropertyField(_openingTextSequence, new GUIContent("Text Sequence"));
        
        EditorGUILayout.Space(10);
        
        _demoData.isExpanded = true;
        EditorGUILayout.PropertyField(_demoData, new GUIContent("Demo Data"));
        
        EditorGUILayout.Space(10);
        
        _officialMissionParameterSOs.isExpanded = true;
        EditorGUILayout.PropertyField(_officialMissionParameterSOs, new GUIContent("Official Missions"));
        
        EditorGUILayout.Space(10);

        if (_addonMissionParameterSOs.arraySize > 0)
        {
            _addonMissionParameterSOs.isExpanded = true;
            EditorGUILayout.PropertyField(_addonMissionParameterSOs, new GUIContent("Addon Missions"));
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}