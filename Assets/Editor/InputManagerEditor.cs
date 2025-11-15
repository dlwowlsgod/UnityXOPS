using UnityEditor;
using UnityEngine;
using UnityXOPS;

[CustomEditor(typeof(InputManager))]
public class InputManagerEditor : Editor
{
    private SerializedProperty _inputActions;
    private SerializedProperty _moveInput;
    private SerializedProperty _lookInput;
    private SerializedProperty _fireInput;
    private SerializedProperty _zoomInput;
    private SerializedProperty _primaryWeaponInput;
    private SerializedProperty _secondaryWeaponInput;
    private SerializedProperty _jumpInput;
    private SerializedProperty _interactInput;
    private SerializedProperty _throwInput;
    private SerializedProperty _reloadInput;
    private SerializedProperty _previousInput;
    private SerializedProperty _nextInput;
    private SerializedProperty _walkInput;
    
    private void OnEnable()
    {
        _inputActions = serializedObject.FindProperty("inputActions");
        _moveInput = serializedObject.FindProperty("moveInput");
        _lookInput = serializedObject.FindProperty("lookInput");
        _fireInput = serializedObject.FindProperty("fireInput");
        _zoomInput = serializedObject.FindProperty("zoomInput");
        _primaryWeaponInput = serializedObject.FindProperty("primaryWeaponInput");
        _secondaryWeaponInput = serializedObject.FindProperty("secondaryWeaponInput");
        _jumpInput = serializedObject.FindProperty("jumpInput");
        _interactInput = serializedObject.FindProperty("interactInput");
        _throwInput = serializedObject.FindProperty("throwInput");
        _reloadInput = serializedObject.FindProperty("reloadInput");
        _previousInput = serializedObject.FindProperty("previousInput");
        _nextInput = serializedObject.FindProperty("nextInput");
        _walkInput = serializedObject.FindProperty("walkInput");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.PropertyField(_inputActions);
        
        EditorGUILayout.Space(10);

        GUI.enabled = false;
        EditorGUILayout.PropertyField(_moveInput);
        EditorGUILayout.PropertyField(_lookInput);
        EditorGUILayout.PropertyField(_fireInput);
        EditorGUILayout.PropertyField(_zoomInput);
        EditorGUILayout.PropertyField(_primaryWeaponInput);
        EditorGUILayout.PropertyField(_secondaryWeaponInput);
        EditorGUILayout.PropertyField(_jumpInput);
        EditorGUILayout.PropertyField(_interactInput);
        EditorGUILayout.PropertyField(_throwInput);
        EditorGUILayout.PropertyField(_reloadInput);
        EditorGUILayout.PropertyField(_previousInput);
        EditorGUILayout.PropertyField(_nextInput);
        EditorGUILayout.PropertyField(_walkInput);
        GUI.enabled = true;
        
        serializedObject.ApplyModifiedProperties();
    }
}