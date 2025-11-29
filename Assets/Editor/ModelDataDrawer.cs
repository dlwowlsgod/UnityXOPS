using UnityEditor;
using UnityEngine;
using UnityXOPS;

[CustomPropertyDrawer(typeof(ModelData))]
public class ModelDataDrawer : PropertyDrawer
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
        
        SerializedProperty path = property.FindPropertyRelative("path");
        currentLineRect.height = singleLineHeight;
        EditorGUI.PropertyField(currentLineRect, path, new GUIContent(path.displayName));
        
        currentLineRect.y += singleLineHeight + spacing;
        SerializedProperty positionProp = property.FindPropertyRelative("position"); // "position" 이름 충돌 방지를 위해 변수명 변경
        EditorGUI.PropertyField(currentLineRect, positionProp, new GUIContent(positionProp.displayName));
        
        currentLineRect.y += singleLineHeight + spacing;
        SerializedProperty rotation = property.FindPropertyRelative("rotation");
        EditorGUI.PropertyField(currentLineRect, rotation, new GUIContent(rotation.displayName));
        
        currentLineRect.y += singleLineHeight + spacing;
        SerializedProperty scale = property.FindPropertyRelative("scale");
        EditorGUI.PropertyField(currentLineRect, scale, new GUIContent(scale.displayName));
        
        currentLineRect.y += singleLineHeight + spacing;
        SerializedProperty textureIndex = property.FindPropertyRelative("textureIndex");
        EditorGUI.PropertyField(currentLineRect, textureIndex, new GUIContent(textureIndex.displayName));
        
        EditorGUI.indentLevel = originalIndentLevel;
        
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float singleLineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        
        return (singleLineHeight * 6) + (spacing * 5);
    }
}