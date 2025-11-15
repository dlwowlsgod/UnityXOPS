using System;
using UnityEditor;
using UnityEngine;
using UnityXOPS;

[CustomEditor(typeof(SkyParameterSO))]
public class SkyParameterSOEditor : Editor
{
    private SerializedProperty[] _scaleAndOffset;
    private SerializedProperty _skyTextures;
    private string[] _labels;

    public void OnEnable()
    {
        _scaleAndOffset = new[]
        {
            serializedObject.FindProperty("frontScaleAndOffset"),
            serializedObject.FindProperty("backScaleAndOffset"),
            serializedObject.FindProperty("leftScaleAndOffset"),
            serializedObject.FindProperty("rightScaleAndOffset"),
            serializedObject.FindProperty("upScaleAndOffset"),
            serializedObject.FindProperty("downScaleAndOffset"),
        };
        
        _skyTextures = serializedObject.FindProperty("skyTextures");

        _labels = new[]
        {
            "Front", "Back", "Left", "Right", "Up", "Down"
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        foreach (var property in _scaleAndOffset)
        {
            property.isExpanded = true;
        }
        _skyTextures.isExpanded = true;
        
        EditorGUILayout.LabelField("Scale And Offset", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        for (int i = 0; i < _scaleAndOffset.Length; i++)
        {
            EditorGUILayout.PropertyField(_scaleAndOffset[i], new GUIContent(_labels[i]));
        }
        EditorGUILayout.Space();
        EditorGUI.indentLevel--;
        
        EditorGUILayout.LabelField("Sky Textures", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        EditorGUILayout.PropertyField(_skyTextures, new GUIContent(""));
        EditorGUI.indentLevel--;

        serializedObject.ApplyModifiedProperties();
    }
}