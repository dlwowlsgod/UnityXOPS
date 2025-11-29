using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityXOPS;

[CustomEditor(typeof(HumanVisualParameterSO))]
public class HumanVisualParameterSOEditor : Editor
{
    private SerializedProperty _textures;
    private SerializedProperty _models;
    private SerializedProperty _armIndex;
    private SerializedProperty _armTextureIndex;
    private SerializedProperty _legIndex;
    private SerializedProperty _legTextureIndex;
    
    private HumanArmParameterSO[] _armParameters;
    private HumanLegParameterSO[] _legParameters;

    public void OnEnable()
    {
        _textures = serializedObject.FindProperty("textures");
        _models = serializedObject.FindProperty("models");
        _armIndex = serializedObject.FindProperty("armIndex");
        _armTextureIndex = serializedObject.FindProperty("armTextureIndex");
        _legIndex = serializedObject.FindProperty("legIndex");
        _legTextureIndex = serializedObject.FindProperty("legTextureIndex");
        
        if (!InitSceneObserver.IsInitScene)
        {
            Debug.LogWarning("Open init scene first.");
            return;
        }
        
        if (ParameterManager.Instance.HumanParameterSO != null)
        {
            var humanSO = ParameterManager.Instance.HumanParameterSO;
            if (humanSO.humanArmParameterSOs != null)
            {
                _armParameters = humanSO.humanArmParameterSOs;
            }
            if (humanSO.humanLegParameterSOs != null)
            {
                _legParameters = humanSO.humanLegParameterSOs;
            }
        }
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Textures
        EditorGUILayout.PropertyField(_textures);
        _textures.isExpanded = true;
        
        // models
        EditorGUILayout.PropertyField(_models);
        _models.isExpanded = true;
        
        EditorGUILayout.Space();
        
        // Arm Index
        EditorGUILayout.LabelField("Arm and Leg Index", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_armIndex);
        using (new EditorGUI.DisabledScope(true))
        {
            var index = _armIndex.intValue;
            HumanArmParameterSO armSO = null;
            if (_armParameters != null && index >= 0 && index < _armParameters.Length)
            {
                armSO = _armParameters[index];
            }
            EditorGUILayout.ObjectField(" ", armSO, typeof(HumanArmParameterSO), false);
        }
        EditorGUILayout.PropertyField(_armTextureIndex);
        
        EditorGUILayout.Space(10);
        
        // Leg Index
        EditorGUILayout.PropertyField(_legIndex);
        using (new EditorGUI.DisabledScope(true))
        {
            var index = _legIndex.intValue;
            HumanLegParameterSO legSO = null;
            if (_legParameters != null && index >= 0 && index < _legParameters.Length)
            {
                legSO = _legParameters[index];
            }
            EditorGUILayout.ObjectField(" ", legSO, typeof(HumanLegParameterSO), false);
        }
        EditorGUILayout.PropertyField(_legTextureIndex);
        
        serializedObject.ApplyModifiedProperties();
    }
}