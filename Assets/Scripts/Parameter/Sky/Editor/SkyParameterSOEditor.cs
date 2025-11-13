using UnityEditor;
using UnityEngine;
using UnityXOPS;

[CustomEditor(typeof(SkyParameterSO))]
public class SkyParameterSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var properties = new[]
        {
            serializedObject.FindProperty("frontScaleAndOffset"),
            serializedObject.FindProperty("backScaleAndOffset"),
            serializedObject.FindProperty("leftScaleAndOffset"),
            serializedObject.FindProperty("rightScaleAndOffset"),
            serializedObject.FindProperty("upScaleAndOffset"),
            serializedObject.FindProperty("downScaleAndOffset"),
            serializedObject.FindProperty("skyTextures")
        };
        
        foreach (var property in properties)
        {
            property.isExpanded = true;
        }

        DrawDefaultInspector();
    }
}