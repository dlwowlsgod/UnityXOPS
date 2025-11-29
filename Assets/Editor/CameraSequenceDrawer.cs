using UnityEngine;
using UnityEditor;
using UnityXOPS;

[CustomPropertyDrawer(typeof(CameraSequence))]
public class CameraSequenceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        float singleLineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        
        Rect currentLineRect = new Rect(position.x, position.y, position.width, singleLineHeight);
        EditorGUI.LabelField(currentLineRect, label);
        
        currentLineRect.y += singleLineHeight + spacing;
        
        int originalIndentLevel = EditorGUI.indentLevel;
        EditorGUI.indentLevel++;

        SerializedProperty start = property.FindPropertyRelative("start");
        currentLineRect.height = singleLineHeight;
        EditorGUI.PropertyField(currentLineRect, start, new GUIContent(start.displayName));
        
        currentLineRect.y += singleLineHeight + spacing;
        SerializedProperty end = property.FindPropertyRelative("end");
        EditorGUI.PropertyField(currentLineRect, end, new GUIContent(end.displayName));
        
        currentLineRect.y += singleLineHeight + spacing;
        SerializedProperty time = property.FindPropertyRelative("time");
        EditorGUI.PropertyField(currentLineRect, time, new GUIContent(time.displayName));
        
        EditorGUI.indentLevel = originalIndentLevel;
        
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float singleLineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        
        return (singleLineHeight * 4) + (spacing * 3) + 5;
    }
}