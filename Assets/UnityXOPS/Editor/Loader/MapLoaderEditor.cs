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
        private SerializedProperty m_blockRoot, m_humanRoot, m_weaponRoot, m_objectRoot, m_skyRoot, m_humanPrefab, m_weaponPrefab, m_objectPrefab;
        private SerializedProperty m_blockCount, m_blockMaterials, m_blockColliders;
        private SerializedProperty m_pointCount, m_humanCount, m_weaponCount, m_objectCount, m_messages, m_player;
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
            m_humanRoot = serializedObject.FindProperty("humanRoot");
            m_weaponRoot = serializedObject.FindProperty("weaponRoot");
            m_objectRoot = serializedObject.FindProperty("objectRoot");
            m_skyRoot   = serializedObject.FindProperty("skyRoot");

            m_humanPrefab = serializedObject.FindProperty("humanPrefab");
            m_weaponPrefab = serializedObject.FindProperty("weaponPrefab");
            m_objectPrefab = serializedObject.FindProperty("objectPrefab");

            m_blockCount = serializedObject.FindProperty("blockCount");
            m_blockMaterials = serializedObject.FindProperty("blockMaterials");
            m_blockColliders = serializedObject.FindProperty("blockColliders");

            m_pointCount = serializedObject.FindProperty("pointCount");
            m_humanCount = serializedObject.FindProperty("humanCount");
            m_weaponCount = serializedObject.FindProperty("weaponCount");
            m_objectCount = serializedObject.FindProperty("objectCount");
            m_messages = serializedObject.FindProperty("messages");
            m_player = serializedObject.FindProperty("player");

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
                DrawPointData();
                EditorGUILayout.Space();
                DrawMissionData();
            }
            else
            {
                EditorGUILayout.PropertyField(m_blockRoot);
                EditorGUILayout.PropertyField(m_humanRoot);
                EditorGUILayout.PropertyField(m_weaponRoot);
                EditorGUILayout.PropertyField(m_objectRoot);
                EditorGUILayout.PropertyField(m_skyRoot);
                EditorGUILayout.PropertyField(m_humanPrefab);
                EditorGUILayout.PropertyField(m_weaponPrefab);
                EditorGUILayout.PropertyField(m_objectPrefab);
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
            EditorGUILayout.PropertyField(m_blockMaterials, new GUIContent("Block Materials"));
            EditorGUILayout.PropertyField(m_blockColliders, new GUIContent("Block Colliders"));
            EditorGUI.indentLevel--;
        }

        private void DrawPointData()
        {
            EditorGUILayout.LabelField("Point Data", new GUIStyle(EditorStyles.boldLabel));
            EditorGUILayout.LabelField($"Points: {m_pointCount.intValue}");
            EditorGUILayout.LabelField($"Humans: {m_humanCount.intValue}");
            EditorGUILayout.LabelField($"Weapons: {m_weaponCount.intValue}");
            EditorGUILayout.LabelField($"Objects: {m_objectCount.intValue}");
            EditorGUILayout.PropertyField(m_player, new GUIContent("Player"));
            EditorGUILayout.LabelField("Messages:");
            EditorGUI.indentLevel++;
            for (int i = 0; i < m_messages.arraySize; i++)
            {
                SerializedProperty msgProp = m_messages.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(msgProp, new GUIContent($"[{i}]"));
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
