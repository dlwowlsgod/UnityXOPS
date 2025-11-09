using UnityEditor;
using UnityXOPS;
using System.Collections.Generic;

[CustomEditor(typeof(SkyParameter))]
public class SkyParameterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        List<SerializedProperty> properties = new()
        {
            serializedObject.FindProperty("frontTextureScaleAndOffset"),
            serializedObject.FindProperty("backTextureScaleAndOffset"),
            serializedObject.FindProperty("leftTextureScaleAndOffset"),
            serializedObject.FindProperty("rightTextureScaleAndOffset"),
            serializedObject.FindProperty("upTextureScaleAndOffset"),
            serializedObject.FindProperty("downTextureScaleAndOffset"),
            serializedObject.FindProperty("skyboxTexturePath")
        };

        foreach (var property in properties)
        {
            property.isExpanded = true;
        }
        
        DrawDefaultInspector();
    }
}