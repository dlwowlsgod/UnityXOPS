using UnityEngine;
using UnityEditor;
using UnityXOPS;
using System.Reflection;
using System.IO;

namespace UnityXOPSEditor
{
    /// <summary>
    /// FontManager 컴포넌트의 OS 폰트 경로를 인스펙터에서 표시하는 에디터 클래스.
    /// </summary>
    [CustomEditor(typeof(FontManager))]
    public class FontManagerEditor : Editor
    {
        /// <summary>
        /// 현재 시스템 언어에 해당하는 OS 폰트 경로를 표시하고 폴더 열기 버튼을 렌더링한다.
        /// </summary>
        public override void OnInspectorGUI()
        {
            string fieldName = Application.systemLanguage switch
            {
                SystemLanguage.Korean   => "m_koreanOSFontPath",
                SystemLanguage.Japanese => "m_japaneseOSFontPath",
                _                       => "m_englishOSFontPath",
            };

            var field = typeof(FontManager).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            string path = field?.GetValue(target) as string;

            EditorGUILayout.LabelField($"OS Font Path: {(string.IsNullOrEmpty(path) ? "(The path is not displayed in editor mode.)" : path)}");

            GUI.enabled = !string.IsNullOrEmpty(path);
            if (GUILayout.Button("Open Folder"))
                EditorUtility.RevealInFinder(Path.GetDirectoryName(path));
            GUI.enabled = true;
        }
    }
}
