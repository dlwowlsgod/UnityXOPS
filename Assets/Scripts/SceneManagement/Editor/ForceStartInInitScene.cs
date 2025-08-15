using UnityEditor;
using UnityEditor.SceneManagement;

namespace UnityXOPS.Editor
{
    [InitializeOnLoad]
    public class ForceStartInInitScene
    {
        static ForceStartInInitScene()
        {
            var scenePath = EditorBuildSettings.scenes[0].path;
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            EditorSceneManager.playModeStartScene = scene;
        }
    }
}