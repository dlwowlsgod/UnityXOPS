using UnityEditor;
using UnityEngine;
using UnityXOPS;

[CustomPropertyDrawer(typeof(DemoData))]
public class DemoDataDrawer : PropertyDrawer
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
        
        SerializedProperty bd1 = property.FindPropertyRelative("bd1Path");
        currentLineRect.height = singleLineHeight;
        EditorGUI.PropertyField(currentLineRect, bd1, new GUIContent(bd1.displayName));
        
        currentLineRect.y += singleLineHeight + spacing;
        SerializedProperty pd1 = property.FindPropertyRelative("pd1Path");
        EditorGUI.PropertyField(currentLineRect, pd1, new GUIContent(pd1.displayName));
        
        currentLineRect.y += singleLineHeight + spacing;
        SerializedProperty skyIndex = property.FindPropertyRelative("skyIndex");
        EditorGUI.PropertyField(currentLineRect, skyIndex, new GUIContent(skyIndex.displayName));
        
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