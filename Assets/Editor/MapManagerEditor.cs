using UnityEngine;
using UnityEditor;
using UnityXOPS;

[CustomEditor(typeof(MapManager))]
public class MapManagerEditor : Editor
{
    private SerializedProperty _mapName;
    private SerializedProperty _mapFullName;
    private SerializedProperty _briefingText;
    private SerializedProperty _briefingImage0;
    private SerializedProperty _briefingImage1;
    private SerializedProperty _bd1Name;
    private SerializedProperty _textures;
    private SerializedProperty _blockCount;
    private SerializedProperty _pd1Name;
    private SerializedProperty _pointCount;
    private SerializedProperty _activeHumanCount;
    private SerializedProperty _deadHumanCount;
    private SerializedProperty _equippedWeaponCount;
    private SerializedProperty _droppedWeaponCount;
    private SerializedProperty _activeObjectCount;
    private SerializedProperty _destroyedObjectCount;
    private SerializedProperty _messages;
    private SerializedProperty _player;
    private SerializedProperty _time;
    private SerializedProperty _fired;
    private SerializedProperty _hit;
    private SerializedProperty _killed;
    private SerializedProperty _headshot;

    private void OnEnable()
    {
        _mapName = serializedObject.FindProperty("mapName");
        _mapFullName = serializedObject.FindProperty("mapFullName");
        _briefingText = serializedObject.FindProperty("briefingText");
        _briefingImage0 = serializedObject.FindProperty("briefingImage0");
        _briefingImage1 = serializedObject.FindProperty("briefingImage1");
        _bd1Name = serializedObject.FindProperty("bd1Name");
        _textures = serializedObject.FindProperty("textures");
        _blockCount = serializedObject.FindProperty("blockCount");
        _pd1Name = serializedObject.FindProperty("pd1Name");
        _pointCount = serializedObject.FindProperty("pointCount");  
        _activeHumanCount = serializedObject.FindProperty("activeHumanCount");
        _deadHumanCount = serializedObject.FindProperty("deadHumanCount");
        _equippedWeaponCount = serializedObject.FindProperty("equippedWeaponCount");
        _droppedWeaponCount = serializedObject.FindProperty("droppedWeaponCount");
        _activeObjectCount = serializedObject.FindProperty("activeObjectCount");
        _destroyedObjectCount = serializedObject.FindProperty("destroyedObjectCount");
        _messages = serializedObject.FindProperty("messages");
        _player = serializedObject.FindProperty("player");
        _time = serializedObject.FindProperty("time");
        _fired = serializedObject.FindProperty("fired");
        _hit = serializedObject.FindProperty("hit");
        _killed = serializedObject.FindProperty("killed");
        _headshot = serializedObject.FindProperty("headshot");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        GUI.enabled = false;
        EditorGUILayout.LabelField("Briefing Data", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        EditorGUILayout.PropertyField(_mapName);
        EditorGUILayout.PropertyField(_mapFullName);
        EditorGUILayout.LabelField("Briefing Images");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(_briefingImage0, GUIContent.none, GUILayout.Height(150));
        EditorGUILayout.PropertyField(_briefingImage1, GUIContent.none, GUILayout.Height(150));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.PropertyField(_briefingText);

        
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Block Data", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        EditorGUILayout.PropertyField(_bd1Name);
        _textures.isExpanded = true;
        EditorGUILayout.PropertyField(_textures);
        EditorGUILayout.PropertyField(_blockCount);
        
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Point Data", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        EditorGUILayout.PropertyField(_pd1Name);
        EditorGUILayout.PropertyField(_pointCount);
        EditorGUILayout.PropertyField(_activeHumanCount);
        EditorGUILayout.PropertyField(_deadHumanCount);
        EditorGUILayout.PropertyField(_equippedWeaponCount);
        EditorGUILayout.PropertyField(_droppedWeaponCount);
        EditorGUILayout.PropertyField(_activeObjectCount);
        EditorGUILayout.PropertyField(_destroyedObjectCount);
        _messages.isExpanded = true;
        EditorGUILayout.PropertyField(_messages);
        
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Player Data", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        EditorGUILayout.PropertyField(_player);
        EditorGUILayout.PropertyField(_time);
        EditorGUILayout.PropertyField(_fired);
        EditorGUILayout.PropertyField(_hit);
        EditorGUILayout.PropertyField(_killed);
        EditorGUILayout.PropertyField(_headshot);
        EditorGUI.indentLevel--;
        GUI.enabled = true;
        
        serializedObject.ApplyModifiedProperties();
    }
}