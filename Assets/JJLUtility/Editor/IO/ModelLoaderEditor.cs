using UnityEngine;
using UnityEditor;
using JJLUtility.IO;

namespace JJLUtilityEditor.IO
{
    /// <summary>
    /// ModelLoader 컴포넌트의 메시 캐시를 인스펙터에서 시각화하는 에디터 클래스.
    /// </summary>
    [CustomEditor(typeof(ModelLoader))]
    public class ModelLoaderEditor : Editor
    {
        private SerializedProperty m_meshCache;

        /// <summary>
        /// 직렬화 프로퍼티 참조를 초기화한다.
        /// </summary>
        private void OnEnable()
        {
            m_meshCache = serializedObject.FindProperty("meshCacheList");
        }

        /// <summary>
        /// 메시 캐시 목록을 인스펙터에 렌더링한다.
        /// </summary>
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
