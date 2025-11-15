using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityXOPS;

[CustomEditor(typeof(HumanVisualParameterSO))]
public class HumanVisualParameterSOEditor : Editor
{
    private SerializedProperty _textures;
    private SerializedProperty _models;
    private SerializedProperty _positions;
    private SerializedProperty _rotations;
    private SerializedProperty _scales;
    private SerializedProperty _textureIndices;
    private SerializedProperty _armIndex;
    private SerializedProperty _legIndex;
    
    private HumanArmParameterSO[] _armParameters;
    private HumanLegParameterSO[] _legParameters;

    public void OnEnable()
    {
        _textures = serializedObject.FindProperty("textures");
        _models = serializedObject.FindProperty("models");
        _positions = serializedObject.FindProperty("positions");
        _rotations = serializedObject.FindProperty("rotations");
        _scales = serializedObject.FindProperty("scales");
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
        
        // Textures
        EditorGUILayout.LabelField("Textures", EditorStyles.boldLabel);
    
        var textureCount = _textures.arraySize;
    
        if (textureCount == 0)
        {
            EditorGUILayout.HelpBox("No textures added yet.", MessageType.Info);
        }
        else
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < textureCount; i++)
            {
                EditorGUILayout.BeginHorizontal();
        
                string elementLabel = $"Texture {i}";
                EditorGUILayout.LabelField(elementLabel, GUILayout.Width(100));
        
                SerializedProperty textureElement = _textures.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(textureElement, GUIContent.none);
        
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    if (EditorUtility.DisplayDialog("Remove Texture", 
                            $"Are you sure you want to remove Texture {i}?", "Yes", "No"))
                    {
                        _textures.DeleteArrayElementAtIndex(i);
                    }
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndHorizontal();
                    serializedObject.ApplyModifiedProperties();
                    return;
                }
        
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }
    
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
    
        if (GUILayout.Button("Add Texture", GUILayout.Width(100)))
        {
            _textures.arraySize++;
        }
    
        if (GUILayout.Button("Clear All", GUILayout.Width(100)) && textureCount > 0)
        {
            if (EditorUtility.DisplayDialog("Clear All Textures", 
                    "Are you sure you want to remove all textures?", "Yes", "No"))
            {
                _textures.arraySize = 0;
            }
        }
    
        EditorGUILayout.EndHorizontal();
    
        EditorGUILayout.Space();

        
        // Models
        EditorGUILayout.LabelField("Models", EditorStyles.boldLabel);
        
        var modelCount = _models.arraySize;
        
        if (modelCount == 0)
        {
            EditorGUILayout.HelpBox("No models added yet.", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < modelCount; i++)
            {
                DrawModelElement(i);
                EditorGUILayout.Space(10); 
            }
        }
        
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("Add Model", GUILayout.Width(100)))
        {
            _models.arraySize++;
            _positions.arraySize++;
            _rotations.arraySize++;
            _scales.arraySize++;
            _textureIndices.arraySize++;
            
            _scales.GetArrayElementAtIndex(_scales.arraySize - 1).vector3Value = Vector3.one;
        }
        
        if (GUILayout.Button("Clear All", GUILayout.Width(100)) && modelCount > 0)
        {
            if (EditorUtility.DisplayDialog("Clear All Models", 
                "Are you sure you want to remove all models?", "Yes", "No"))
            {
                _models.arraySize = 0;
                _positions.arraySize = 0;
                _rotations.arraySize = 0;
                _scales.arraySize = 0;
                _textureIndices.arraySize = 0;
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawModelElement(int index)
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        
        // Model Line
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Model {index}", GUILayout.Width(80));
        
        SerializedProperty modelElement = _models.GetArrayElementAtIndex(index);
        EditorGUILayout.PropertyField(modelElement, GUIContent.none);
        
        if (GUILayout.Button("×", GUILayout.Width(25)))
        {
            if (EditorUtility.DisplayDialog("Remove Model", 
                $"Are you sure you want to remove Model {index}?", "Yes", "No"))
            {
                _models.DeleteArrayElementAtIndex(index);
                _positions.DeleteArrayElementAtIndex(index);
                _rotations.DeleteArrayElementAtIndex(index);
                _scales.DeleteArrayElementAtIndex(index);
                _textureIndices.DeleteArrayElementAtIndex(index);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }
        EditorGUILayout.EndHorizontal();
        
        // Position, Rotation, Scale, TextureIndex
        EditorGUI.indentLevel++;
        
        SerializedProperty positionElement = _positions.GetArrayElementAtIndex(index);
        EditorGUILayout.PropertyField(positionElement, new GUIContent("Position"));
        
        SerializedProperty rotationElement = _rotations.GetArrayElementAtIndex(index);
        EditorGUILayout.PropertyField(rotationElement, new GUIContent("Rotation"));
        
        SerializedProperty scaleElement = _scales.GetArrayElementAtIndex(index);
        EditorGUILayout.PropertyField(scaleElement, new GUIContent("Scale"));
        
        SerializedProperty textureIndexElement = _textureIndices.GetArrayElementAtIndex(index);
        EditorGUILayout.PropertyField(textureIndexElement, new GUIContent("Texture Index"));
        
        EditorGUI.indentLevel--;
        
        EditorGUILayout.EndVertical();
    }
}