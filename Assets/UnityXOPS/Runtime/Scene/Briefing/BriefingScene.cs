using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityXOPS
{
    /// <summary>
    /// 브리핑 씬을 관리하고 ESC 입력 시 메인메뉴로 복귀하는 컨트롤러.
    /// </summary>
    public class BriefingScene : MonoBehaviour
    {
        private void Start()
        {
            InputManager.MouseCursorMode(true, false, false);
        }

        private void Update()
        {
            if (InputManager.Keyboard.escapeKey.wasPressedThisFrame)
            {
                MapLoader.UnloadBlockData();
                MapLoader.UnloadPointData();
                MapLoader.UnloadMissionData();
                MapLoader.UnloadSkyData();
                Camera.main.gameObject.SetActive(false);
                SceneManager.LoadScene(2);
                return;
            }

            if (InputManager.Mouse.leftButton.wasPressedThisFrame)
            {
                Camera.main.gameObject.SetActive(false);
                SceneManager.LoadScene(4);
            }
        }
    }
}
