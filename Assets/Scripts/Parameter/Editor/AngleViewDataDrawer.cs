using UnityEditor;
using UnityEngine;

namespace UnityXOPS
{
    [CustomPropertyDrawer(typeof(ViewAngleData))]
    public class ViewAngleDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            EditorGUILayout.LabelField(label, EditorStyles.label);

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(property.FindPropertyRelative("horizontal"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("vertical"));
            
            EditorGUI.indentLevel--;
            
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => 0;
    }
}