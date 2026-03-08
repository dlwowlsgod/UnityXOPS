using UnityEngine;
using UnityEditor;
using JJLUtility.IO;

namespace JJLUtilityEditor.IO
{
    [CustomEditor(typeof(ImageLoader))]
    public class ImageLoaderEditor : Editor
    {
        private SerializedProperty m_textureCache;

        private void OnEnable()
        {
            m_textureCache = serializedObject.FindProperty("textureCacheList");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_textureCache.isExpanded = true;
            EditorGUILayout.LabelField("Texture Cache", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            for (int i = 0; i < m_textureCache.arraySize; i++)
            {
                SerializedProperty textureProp = m_textureCache.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(textureProp, new GUIContent($"[{i}]"));
            }
            if (m_textureCache.arraySize == 0)
            {
                EditorGUILayout.LabelField("No cached textures.");
            }
            EditorGUI.indentLevel--;
            serializedObject.ApplyModifiedProperties();
        }
    }

}