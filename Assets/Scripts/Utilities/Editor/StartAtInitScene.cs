using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class StartAtInitScene
{
    static StartAtInitScene()
    {
        if (EditorBuildSettings.scenes.Length == 0)
        {
            Debug.LogWarning("Add Init scene first order on Build Settings/Scenes In Build");
            return;
        }
        
        var scene = EditorBuildSettings.scenes[0].path;
        if (SceneManager.GetActiveScene() != SceneManager.GetSceneByPath(scene))
        {
            EditorSceneManager.OpenScene(scene);
        }
    }
}