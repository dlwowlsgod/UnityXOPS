using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class PlayModeStartInitScene
{
    private const string MenuName = "UnityXOPS/Play Mode Start Init Scene";
    private const string PrefKey = "PlayModeStartInitScene_Enabled";
    
    private static bool IsEnabled
    {
        get => EditorPrefs.GetBool(PrefKey, true);
        set => EditorPrefs.SetBool(PrefKey, value);
    }
    
    static PlayModeStartInitScene()
    {
        ApplyPlayModeStartScene();
        EditorApplication.delayCall += () => Menu.SetChecked(MenuName, IsEnabled);
    }
    
    [MenuItem(MenuName)]
    private static void TogglePlayModeStartScene()
    {
        IsEnabled = !IsEnabled;
        Menu.SetChecked(MenuName, IsEnabled);
        ApplyPlayModeStartScene();
        
        Debug.Log($"Play Mode Start Init Scene: {(IsEnabled ? "Enabled" : "Disabled")}");
    }
    
    [MenuItem(MenuName, true)]
    private static bool TogglePlayModeStartSceneValidate()
    {
        Menu.SetChecked(MenuName, IsEnabled);
        return true;
    }
    
    private static void ApplyPlayModeStartScene()
    {
        if (!IsEnabled)
        {
            EditorSceneManager.playModeStartScene = null;
            return;
        }
        
        if (EditorBuildSettings.scenes.Length == 0)
        {
            Debug.LogWarning("Add Init scene first order on Build Settings/Scenes In Build");
            return;
        }
        
        var scene = EditorBuildSettings.scenes[0].path;
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene);
        EditorSceneManager.playModeStartScene = sceneAsset;
    }
}