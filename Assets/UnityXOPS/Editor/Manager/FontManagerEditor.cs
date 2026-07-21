using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityXOPS;
using TMPro;

namespace UnityXOPSEditor
{
    /// <summary>
    /// FontManager 컴포넌트가 구성한 OS 폰트 대체 체인을 인스펙터에서 표시하는 에디터 클래스.
    /// 체인은 런타임에 만들어지므로 플레이 모드에서만 내용이 보인다.
    /// </summary>
    [CustomEditor(typeof(FontManager))]
    public class FontManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("OS Font Chain", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("폰트 체인은 플레이 모드에서 구성된다.", MessageType.Info);
                return;
            }

            TMP_FontAsset font = FontManager.OSFont;
            if (font == null)
            {
                EditorGUILayout.HelpBox("OS 폰트를 찾지 못했다.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField($"1. {font.name}");

            List<TMP_FontAsset> fallbacks = font.fallbackFontAssetTable;
            if (fallbacks == null)
            {
                return;
            }

            for (int i = 0; i < fallbacks.Count; i++)
            {
                if (fallbacks[i] != null)
                {
                    EditorGUILayout.LabelField($"{i + 2}. {fallbacks[i].name}");
                }
            }
        }
    }
}
