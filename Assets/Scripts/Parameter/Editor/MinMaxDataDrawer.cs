using UnityEngine;
using UnityEditor;

namespace UnityXOPS
{
    [CustomPropertyDrawer(typeof(MinMaxData))]
    public class MinMaxDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            
            float labelWidth = 30;
            float spacing = 5;
            float fieldWidth = (position.width - labelWidth * 2 - spacing) / 2;
            
            var minLabelRect = new Rect(position.x, position.y, labelWidth, position.height);
            var minFieldRect = new Rect(minLabelRect.xMax, position.y, fieldWidth, position.height);
            
            var maxLabelRect = new Rect(minFieldRect.xMax + spacing, position.y, labelWidth, position.height);
            var maxFieldRect = new Rect(maxLabelRect.xMax, position.y, fieldWidth, position.height);
            
            EditorGUI.LabelField(minLabelRect, "Min");
            EditorGUI.PropertyField(minFieldRect, property.FindPropertyRelative("min"), GUIContent.none);
            EditorGUI.LabelField(maxLabelRect, "Max");
            EditorGUI.PropertyField(maxFieldRect, property.FindPropertyRelative("max"), GUIContent.none);
            
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }
}