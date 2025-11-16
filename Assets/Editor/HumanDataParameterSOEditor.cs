using UnityEditor;
using UnityEngine;
using UnityXOPS;

[CustomEditor(typeof(HumanDataParameterSO))]
public class HumanDataParameterSOEditor : Editor
{
    private SerializedProperty _hp;
    private SerializedProperty _weapon0Index;
    private SerializedProperty _weapon1Index;
    private SerializedProperty _visualIndex;
    
    private HumanVisualParameterSO[] _visualParameters;
    
    private void OnEnable()
    {
        _hp = serializedObject.FindProperty("hp");
        _weapon0Index = serializedObject.FindProperty("weapon0Index");
        _weapon1Index = serializedObject.FindProperty("weapon1Index");
        _visualIndex = serializedObject.FindProperty("visualIndex");
        
        if (!InitSceneObserver.IsInitScene)
        {
            Debug.LogWarning("Open init scene first.");
            return;
        }
        
        if (ParameterManager.Instance.HumanParameterSO != null)
        {
            var humanSO = ParameterManager.Instance.HumanParameterSO;
            if (humanSO.humanVisualParameterSOs != null)
            {
                _visualParameters = humanSO.humanVisualParameterSOs;
            }
        }
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.PropertyField(_hp);
        
        EditorGUILayout.PropertyField(_weapon0Index);
        EditorGUILayout.PropertyField(_weapon1Index);
        
        EditorGUILayout.PropertyField(_visualIndex);
        using (new EditorGUI.DisabledScope(true))
        {
            var index = _visualIndex.intValue;
            HumanVisualParameterSO visualSO = null;
            if (_visualParameters != null && index >= 0 && index < _visualParameters.Length)
            {
                visualSO = _visualParameters[index];
            }
            EditorGUILayout.ObjectField(" ", visualSO, typeof(HumanVisualParameterSO), false);
        }
        
        
        
        serializedObject.ApplyModifiedProperties();
    }
}