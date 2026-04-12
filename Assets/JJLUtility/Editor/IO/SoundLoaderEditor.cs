using UnityEngine;
using UnityEditor;
using JJLUtility.IO;

namespace JJLUtilityEditor.IO
{
    /// <summary>
    /// SoundLoader 싱글톤의 오디오 캐시 목록을 에디터 인스펙터에서 시각화하는 커스텀 에디터.
    /// </summary>
    [CustomEditor(typeof(SoundLoader))]
    public class SoundLoaderEditor : Editor
    {
        private SerializedProperty m_audioCache;

        /// <summary>
        /// 직렬화된 audioCacheList 프로퍼티를 캐싱한다.
        /// </summary>
        private void OnEnable()
        {
            m_audioCache = serializedObject.FindProperty("audioCacheList");
        }

        /// <summary>
        /// 오디오 캐시 목록을 표로 표시한다.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Audio Cache", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            for (int i = 0; i < m_audioCache.arraySize; i++)
            {
                SerializedProperty clipProp = m_audioCache.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(clipProp, new GUIContent($"[{i}]"));
            }
            if (m_audioCache.arraySize == 0)
            {
                EditorGUILayout.LabelField("No cached audio clips.");
            }
            EditorGUI.indentLevel--;
        }
    }
}
