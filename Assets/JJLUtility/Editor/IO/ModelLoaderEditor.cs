using UnityEngine;
using UnityEditor;
using JJLUtility.IO;

namespace JJLUtilityEditor.IO
{
    [CustomEditor(typeof(ModelLoader))]
    public class ModelLoaderEditor : Editor
    {
        private SerializedProperty m_meshCache;

        private void OnEnable()
        {
            m_meshCache = serializedObject.FindProperty("meshCacheList");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_meshCache.isExpanded = true;
            EditorGUILayout.LabelField("Mesh Cache", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            for (int i = 0; i < m_meshCache.arraySize; i++)
            {
                SerializedProperty meshProp = m_meshCache.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(meshProp, new GUIContent($"[{i}]"));
            }
            if (m_meshCache.arraySize == 0)
            {
                EditorGUILayout.LabelField("No cached meshes.");
            }
            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
