using UnityEngine;
using UnityEditor;
using UnityXOPS;

namespace UnityXOPSEditor
{
    /// <summary>
    /// MapLoader 컴포넌트의 블록 데이터와 미션 데이터를 인스펙터에서 시각화하는 에디터 클래스.
    /// </summary>
    [CustomEditor(typeof(MapLoader))]
    public class MapLoaderEditor : Editor
    {
        private SerializedProperty m_blockRoot, m_nullMaterial, m_transparentMaterial, m_mainMaterial, m_skyMaterial;
        private SerializedProperty m_blockCount, m_blockMaterials;
        private SerializedProperty m_missionName, m_missionFullname, m_missionBD1Path, m_missionPD1Path,
            m_missionAddonObjectPath, m_missionImage0, m_missionImage1, m_skyIndex, m_missionBriefing,
            m_adjustCollision, m_darkScreen;

        private bool     m_missionFoldout = true;
        private GUIStyle m_wrapStyle;

        /// <summary>
        /// 직렬화 프로퍼티 참조를 초기화한다.
        /// </summary>
        private void OnEnable()
        {
            m_blockRoot = serializedObject.FindProperty("blockRoot");
            m_nullMaterial = serializedObject.FindProperty("nullMaterial");
            m_transparentMaterial = serializedObject.FindProperty("transparentMaterial");
            m_mainMaterial = serializedObject.FindProperty("mainMaterial");
            m_skyMaterial = serializedObject.FindProperty("skyMaterial");
            m_blockCount = serializedObject.FindProperty("blockCount");
            m_blockMaterials = serializedObject.FindProperty("blockMaterials");

            m_missionName = serializedObject.FindProperty("missionName");
            m_missionFullname = serializedObject.FindProperty("missionFullname");
            m_missionBD1Path = serializedObject.FindProperty("missionBD1Path");
            m_missionPD1Path = serializedObject.FindProperty("missionPD1Path");
            m_missionAddonObjectPath = serializedObject.FindProperty("missionAddonObjectPath");
            m_missionImage0 = serializedObject.FindProperty("missionImage0");
            m_missionImage1 = serializedObject.FindProperty("missionImage1");
            m_skyIndex = serializedObject.FindProperty("skyIndex");
            m_missionBriefing = serializedObject.FindProperty("missionBriefing");
            m_adjustCollision = serializedObject.FindProperty("adjustCollision");
            m_darkScreen = serializedObject.FindProperty("darkScreen");
        }

        /// <summary>
        /// 플레이 중에는 블록·미션 데이터를, 편집 중에는 머티리얼 레퍼런스를 인스펙터에 렌더링한다.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (EditorApplication.isPlaying)
            {
                DrawBlockData();
                EditorGUILayout.Space();
                DrawMissionData();
            }
            else
            {
                EditorGUILayout.PropertyField(m_blockRoot);
                EditorGUILayout.PropertyField(m_nullMaterial);
                EditorGUILayout.PropertyField(m_transparentMaterial);
                EditorGUILayout.PropertyField(m_mainMaterial);
                EditorGUILayout.PropertyField(m_skyMaterial);
                serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// 블록 수와 텍스처 머티리얼 목록을 인스펙터에 렌더링한다.
        /// </summary>
        private void DrawBlockData()
        {
            EditorGUILayout.LabelField("Block Data", new GUIStyle(EditorStyles.boldLabel));
            EditorGUILayout.LabelField($"Blocks: {m_blockCount.intValue}");
            EditorGUILayout.LabelField($"Block Textures: {m_blockMaterials.arraySize}");
            EditorGUI.indentLevel++;
            for (int i = 0; i < m_blockMaterials.arraySize; i++)
            {
                SerializedProperty materialProp = m_blockMaterials.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(materialProp, new GUIContent($"[{i}]"));
            }
            if (m_blockMaterials.arraySize == 0)
            {
                EditorGUILayout.LabelField("No block textures.");
            }
            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// 현재 로드된 미션 정보를 상세 펼침과 브리핑 텍스트로 인스펙터에 렌더링한다.
        /// </summary>
        private void DrawMissionData()
        {
            m_wrapStyle ??= new GUIStyle(EditorStyles.label) { wordWrap = true };

            EditorGUILayout.LabelField("Mission Data", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Current Mission: {m_missionName.stringValue}", m_wrapStyle);
            EditorGUILayout.LabelField($"({m_missionFullname.stringValue})", m_wrapStyle);

            m_missionFoldout = EditorGUILayout.Foldout(m_missionFoldout, "Details", true);
            if (m_missionFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"BD1         {m_missionBD1Path.stringValue}", m_wrapStyle);
                EditorGUILayout.LabelField($"PD1         {m_missionPD1Path.stringValue}", m_wrapStyle);
                EditorGUILayout.LabelField($"AddonObject {m_missionAddonObjectPath.stringValue}", m_wrapStyle);
                EditorGUILayout.LabelField($"Sky         {m_skyIndex.intValue}");

                EditorGUILayout.LabelField("Image");
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(m_missionImage0.stringValue, m_wrapStyle);
                EditorGUILayout.LabelField(m_missionImage1.stringValue, m_wrapStyle);
                EditorGUI.indentLevel--;

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Toggle("AdjustCollision", m_adjustCollision.boolValue);
                EditorGUILayout.Toggle("DarkScreen",      m_darkScreen.boolValue);
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4);

            // \n 포함 여러 줄 브리핑 — CalcHeight로 실제 높이를 계산해 라벨에 반영
            EditorGUILayout.LabelField("Briefing Description");
            var briefingContent = new GUIContent(m_missionBriefing.stringValue);
            float briefingHeight = m_wrapStyle.CalcHeight(briefingContent, EditorGUIUtility.currentViewWidth - 20f);
            EditorGUILayout.LabelField(briefingContent, m_wrapStyle, GUILayout.Height(briefingHeight));
        }
    }
}
