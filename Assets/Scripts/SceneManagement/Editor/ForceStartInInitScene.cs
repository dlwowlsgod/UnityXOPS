using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityXOPS.Editor
{
    /// <summary>
    /// 유니티 에디터 상에서 기본적으로 Init 씬에서 실행되게 설정하는 클래스입니다.
    /// </summary>
    [InitializeOnLoad]
    public class ForceStartInInitScene
    {
        static ForceStartInInitScene()
        {
            var scenePath = EditorBuildSettings.scenes[0].path;
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            EditorSceneManager.playModeStartScene = sceneAsset;
        }
    }
}