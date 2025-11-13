using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class InitSceneObserver
{
    public static bool IsInitScene { get; private set; }
    static InitSceneObserver()
    {
        EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
        UpdateSceneFlag();
    }

    private static void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        UpdateSceneFlag();
    }

    private static void UpdateSceneFlag()
    {
        IsInitScene = SceneManager.GetActiveScene().name == "Init";
    }
}