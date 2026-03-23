using UnityEngine;
using UnityEditor;
using JJLUtility.IO;

namespace JJLUtilityEditor.IO
{
    /// <summary>
    /// ImageLoader 컴포넌트의 텍스처 캐시를 인스펙터에서 시각화하는 에디터 클래스.
    /// </summary>
    [CustomEditor(typeof(ImageLoader))]
    public class ImageLoaderEditor : Editor
    {
        private SerializedProperty m_textureCache;

        /// <summary>
        /// 직렬화 프로퍼티 참조를 초기화한다.
        /// </summary>
        private void OnEnable()
        {
            m_textureCache = serializedObject.FindProperty("textureCacheList");
        }

        /// <summary>
        /// 텍스처 캐시 목록을 인스펙터에 렌더링한다.
        /// </summary>
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