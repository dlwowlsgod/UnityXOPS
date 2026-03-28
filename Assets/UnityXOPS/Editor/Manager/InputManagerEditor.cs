using UnityEngine;
using UnityEditor;
using UnityXOPS;

namespace UnityXOPSEditor
{
    /// <summary>
    /// InputManager 컴포넌트의 입력 값을 인스펙터에서 실시간으로 시각화하는 에디터 클래스.
    /// </summary>
    [CustomEditor(typeof(InputManager))]
    public class InputManagerEditor : Editor
    {
        private GUIStyle m_boolStyle;

        /// <summary>
        /// 플레이 중에는 인스펙터를 매 프레임 갱신하도록 true를 반환한다.
        /// </summary>
        public override bool RequiresConstantRepaint() => Application.isPlaying;

        /// <summary>
        /// Look·Move 벡터 시각화와 버튼 입력 상태를 인스펙터에 렌더링한다.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_boolStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = 11,
            };

            DrawVector2Visualizer("Look", serializedObject.FindProperty("lookValue").vector2Value, Color.red);
            EditorGUILayout.Space(4);
            DrawVector2Visualizer("Move", serializedObject.FindProperty("moveValue").vector2Value, Color.black);
            EditorGUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            DrawBoolLabel("Jump",     serializedObject.FindProperty("jumpValue").boolValue);
            DrawBoolLabel("Walk",     serializedObject.FindProperty("walkValue").boolValue);
            DrawBoolLabel("Drop",     serializedObject.FindProperty("dropValue").boolValue);
            DrawBoolLabel("Fire",     serializedObject.FindProperty("fireValue").boolValue);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawBoolLabel("Zoom",     serializedObject.FindProperty("zoomValue").boolValue);
            DrawBoolLabel("Previous", serializedObject.FindProperty("previousValue").boolValue);
            DrawBoolLabel("Next",     serializedObject.FindProperty("nextValue").boolValue);
            DrawBoolLabel("Reload",   serializedObject.FindProperty("reloadValue").boolValue);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawBoolLabel("First",    serializedObject.FindProperty("firstValue").boolValue);
            DrawBoolLabel("Second",   serializedObject.FindProperty("secondValue").boolValue);
            DrawBoolLabel("Interact", serializedObject.FindProperty("interactValue").boolValue);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 지정한 레이블과 색상으로 Vector2 값을 사각형 박스 안에 점으로 시각화한다.
        /// </summary>
        /// <param name="label">시각화 박스 위에 표시할 레이블.</param>
        /// <param name="value">시각화할 Vector2 입력 값.</param>
        /// <param name="dotColor">박스 안에 그릴 점의 색상.</param>
        private void DrawVector2Visualizer(string label, Vector2 value, Color dotColor)
        {
            const float boxSize   = 80f;
            const float dotRadius = 4f;

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            Rect rect = GUILayoutUtility.GetRect(boxSize, boxSize, GUILayout.Width(boxSize), GUILayout.Height(boxSize));

            // 배경 및 테두리
            EditorGUI.DrawRect(rect, new Color(0.88f, 0.88f, 0.88f));
            EditorGUI.DrawRect(new Rect(rect.x,        rect.y,        rect.width, 1),          Color.black);
            EditorGUI.DrawRect(new Rect(rect.x,        rect.yMax - 1, rect.width, 1),          Color.black);
            EditorGUI.DrawRect(new Rect(rect.x,        rect.y,        1,          rect.height), Color.black);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y,        1,          rect.height), Color.black);

            // 방향을 유지한 채 -1~1로 정규화
            Vector2 normalized = value.magnitude > 1f ? value.normalized : value;

            float margin  = dotRadius + 2f;
            float centerX = rect.x + rect.width  * 0.5f;
            float centerY = rect.y + rect.height * 0.5f;
            float dotX    = centerX + normalized.x * (rect.width  * 0.5f - margin);
            float dotY    = centerY - normalized.y * (rect.height * 0.5f - margin); // Y축 반전

            Handles.BeginGUI();
            Handles.color = dotColor;
            Handles.DrawSolidDisc(new Vector3(dotX, dotY, 0f), Vector3.forward, dotRadius);
            Handles.EndGUI();

            EditorGUILayout.LabelField($"({value.x:F2}, {value.y:F2})", EditorStyles.centeredGreyMiniLabel);
        }

        /// <summary>
        /// bool 값에 따라 아웃라인 색을 달리한 라벨 박스를 인스펙터에 그린다.
        /// </summary>
        /// <param name="label">박스에 표시할 텍스트.</param>
        /// <param name="value">true면 초록, false면 검정 아웃라인으로 그린다.</param>
        private void DrawBoolLabel(string label, bool value)
        {
            // 다크 모드: 박스는 밝게(반대), 글자는 어둡게(같음)
            // 라이트 모드: 박스는 어둡게(반대), 글자는 밝게(같음)
            bool   isDark      = EditorGUIUtility.isProSkin;
            Color  boxColor    = isDark ? new Color(0.75f, 0.75f, 0.75f) : new Color(0.25f, 0.25f, 0.25f);
            Color  textColor   = isDark ? Color.black                    : Color.white;
            Color  outlineColor = value ? Color.green                    : Color.black;

            m_boolStyle.normal.textColor = m_boolStyle.hover.textColor = textColor;

            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, m_boolStyle, GUILayout.Width(60), GUILayout.Height(22));

            // 아웃라인 (1px 테두리)
            EditorGUI.DrawRect(new Rect(rect.x - 1, rect.y - 1, rect.width + 2, rect.height + 2), outlineColor);
            // 배경
            EditorGUI.DrawRect(rect, boxColor);
            // 라벨
            GUI.Label(rect, label, m_boolStyle);
        }
    }
}
