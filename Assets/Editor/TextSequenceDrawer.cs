using UnityEngine;
using UnityEditor;
using UnityXOPS;

[CustomPropertyDrawer(typeof(TextSequence))]
public class TextSequenceDrawer : PropertyDrawer
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
        
        SerializedProperty text = property.FindPropertyRelative("text");
        currentLineRect.height = singleLineHeight;
        EditorGUI.PropertyField(currentLineRect, text, new GUIContent(text.displayName));
        
        currentLineRect.y += singleLineHeight + spacing;
        SerializedProperty gameFont = property.FindPropertyRelative("gameFont");
        EditorGUI.PropertyField(currentLineRect, gameFont, new GUIContent(gameFont.displayName));
        
        currentLineRect.y += singleLineHeight + spacing;
        SerializedProperty start = property.FindPropertyRelative("start");
        EditorGUI.PropertyField(currentLineRect, start, new GUIContent(start.displayName));
        
        currentLineRect.y += singleLineHeight + spacing;
        SerializedProperty end = property.FindPropertyRelative("end");
        EditorGUI.PropertyField(currentLineRect, end, new GUIContent(end.displayName));
        
        currentLineRect.y += singleLineHeight + spacing;
        SerializedProperty time = property.FindPropertyRelative("time");
        EditorGUI.PropertyField(currentLineRect, time, new GUIContent(time.displayName));
        
        currentLineRect.y += singleLineHeight + spacing;
        SerializedProperty pivot = property.FindPropertyRelative("pivot");
        EditorGUI.PropertyField(currentLineRect, pivot, new GUIContent(pivot.displayName));
        
        currentLineRect.y += singleLineHeight + spacing;
        SerializedProperty anchorMin = property.FindPropertyRelative("anchorMin");
        EditorGUI.PropertyField(currentLineRect, anchorMin, new GUIContent(anchorMin.displayName));
        
        currentLineRect.y += singleLineHeight + spacing;
        SerializedProperty anchorMax = property.FindPropertyRelative("anchorMax");
        EditorGUI.PropertyField(currentLineRect, anchorMax, new GUIContent(anchorMax.displayName));
        
        currentLineRect.y += singleLineHeight + spacing;
        SerializedProperty anchoredPosition = property.FindPropertyRelative("anchoredPosition");
        EditorGUI.PropertyField(currentLineRect, anchoredPosition, new GUIContent(anchoredPosition.displayName));
        
        currentLineRect.y += singleLineHeight + spacing;
        SerializedProperty sizeDelta = property.FindPropertyRelative("sizeDelta");
        EditorGUI.PropertyField(currentLineRect, sizeDelta, new GUIContent(sizeDelta.displayName));
        
        currentLineRect.y += singleLineHeight + spacing;
        SerializedProperty alignment = property.FindPropertyRelative("alignment");
        EditorGUI.PropertyField(currentLineRect, alignment, new GUIContent(alignment.displayName));
        
        currentLineRect.y += singleLineHeight + spacing;
        SerializedProperty ratio = property.FindPropertyRelative("ratio");
        EditorGUI.PropertyField(currentLineRect, ratio, new GUIContent(ratio.displayName));
        
        EditorGUI.indentLevel = originalIndentLevel;
        
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float singleLineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        
        return (singleLineHeight * 13) + (spacing * 12) + 5;
    }
}