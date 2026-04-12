using JJLUtility;
using JJLUtility.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityXOPS
{
    /// <summary>
    /// 메인게임 씬을 관리하고 ESC 입력 시 메인메뉴로 복귀하는 컨트롤러.
    /// </summary>
    public class MaingameScene : MonoBehaviour
    {
        /// <summary>
        /// ESC 입력 시 미션 데이터를 해제하고 메인메뉴 씬으로 전환한다.
        /// </summary>
        private void Update()
        {
            if (InputManager.Keyboard.escapeKey.wasPressedThisFrame)
            {
                MapLoader.UnloadBlockData();
                MapLoader.UnloadPointData();
                MapLoader.UnloadSkyData();
                MapLoader.UnloadMissionData();
                Camera.main.gameObject.SetActive(false);
                SceneManager.LoadScene(2);
            }

            if (InputManager.Keyboard.f12Key.wasPressedThisFrame)
            {

            }
        }
    }
}
