using UnityEditor;
using UnityEditor.SceneManagement;

namespace UnityXOPSEditor
{
    /// <summary>
    /// 플레이 시 항상 첫 번째 씬에서 시작하도록 씬을 전환하고, 플레이 종료 후 원래 씬을 복원하는 에디터 유틸리티.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayFromFirstScene
    {
        const string k_menuPath = "UnityXOPS/Play From First Scene";
        const string k_prefKey = "UnityXOPS.PlayFromFirstScene";
        const string k_prevScenePrefKey = "UnityXOPS.PlayFromFirstScene.PrevScene";

        /// <summary>
        /// 에디터 로드 시 플레이모드 상태 변경 콜백을 등록한다.
        /// </summary>
        static PlayFromFirstScene()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static bool IsEnabled => EditorPrefs.GetBool(k_prefKey, false);

        /// <summary>
        /// 메뉴 항목 클릭 시 기능 활성화 여부를 토글한다.
        /// </summary>
        [MenuItem(k_menuPath, priority = 1)]
        static void Toggle()
        {
            EditorPrefs.SetBool(k_prefKey, !IsEnabled);
            Menu.SetChecked(k_menuPath, IsEnabled);
        }

        /// <summary>
        /// 메뉴 항목 렌더링 시 체크 상태를 현재 활성화 여부로 갱신한다.
        /// </summary>
        [MenuItem(k_menuPath, true)]
        static bool ToggleValidate()
        {
            Menu.SetChecked(k_menuPath, IsEnabled);
            return true;
        }

        /// <summary>
        /// 플레이모드 진입 시 첫 번째 씬으로 전환하고, 종료 시 이전 씬을 복원한다.
        /// </summary>
        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!IsEnabled) return;

            if (state == PlayModeStateChange.ExitingEditMode)
            {
                var scenes = EditorBuildSettings.scenes;
                if (scenes.Length == 0) return;

                string firstScenePath = scenes[0].path;
                if (string.IsNullOrEmpty(firstScenePath)) return;

                string currentPath = EditorSceneManager.GetActiveScene().path;

                // 이미 0번 씬이면 복원 대상 없음
                if (currentPath == firstScenePath)
                {
                    EditorPrefs.DeleteKey(k_prevScenePrefKey);
                    return;
                }

                // 현재 씬 기억 후 0번 씬으로 전환
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorPrefs.SetString(k_prevScenePrefKey, currentPath);
                    EditorSceneManager.OpenScene(firstScenePath);
                }
                else
                {
                    EditorApplication.isPlaying = false;
                }
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                // 플레이 종료 후 이전 씬 복원
                string prevPath = EditorPrefs.GetString(k_prevScenePrefKey, "");
                EditorPrefs.DeleteKey(k_prevScenePrefKey);

                if (!string.IsNullOrEmpty(prevPath))
                    EditorSceneManager.OpenScene(prevPath);
            }
        }
    }
}
