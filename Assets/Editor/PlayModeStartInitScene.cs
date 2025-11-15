using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class PlayModeStartInitScene
{
    static PlayModeStartInitScene()
    {
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