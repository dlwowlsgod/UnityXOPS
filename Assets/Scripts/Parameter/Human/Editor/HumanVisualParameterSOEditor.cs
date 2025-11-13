using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityXOPS;

[CustomEditor(typeof(HumanVisualParameterSO))]
public class HumanVisualParameterSOEditor : Editor
{
    private SerializedProperty _textures;
    private SerializedProperty _models;
    private SerializedProperty _textureIndices;
    private SerializedProperty _armIndex;
    private SerializedProperty _legIndex;
    
    private HumanArmParameterSO[] _armParameters;
    private HumanLegParameterSO[] _legParameters;

    public void OnEnable()
    {
        _textures = serializedObject.FindProperty("textures");
        _models = serializedObject.FindProperty("models");
        _textureIndices = serializedObject.FindProperty("textureIndices");
        _armIndex = serializedObject.FindProperty("armIndex");
        _legIndex = serializedObject.FindProperty("legIndex");

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
        
        int prevModelSize = _models.arraySize;
        int prevTextureIndicesSize = _textureIndices.arraySize;
        
        _textures.isExpanded = true;
        _models.isExpanded = true;
        _textureIndices.isExpanded = true;
        
        EditorGUILayout.PropertyField(_textures, true);
        
        EditorGUILayout.PropertyField(_models, true);
        EditorGUILayout.PropertyField(_textureIndices, true);

        if (_models.arraySize != prevModelSize)
        {
            _textureIndices.arraySize = _models.arraySize;
        }
        else if (_textureIndices.arraySize != prevTextureIndicesSize)
        {
            _models.arraySize = _textureIndices.arraySize;
        }
        
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
        
        serializedObject.ApplyModifiedProperties();

    }
}