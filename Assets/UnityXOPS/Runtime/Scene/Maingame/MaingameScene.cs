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
        private void Start()
        {
            InputManager.MouseCursorMode(true, true, true);
            HumanController.TickEnabled = true;
        }

        private void Update()
        {
            if (InputManager.Keyboard.escapeKey.wasPressedThisFrame)
            {
                MapLoader.UnloadBlockData();
                MapLoader.UnloadPointData();
                MapLoader.UnloadSkyData();
                MapLoader.UnloadMissionData();
                HumanController.TickEnabled = false;
                Camera.main.gameObject.SetActive(false);
                SceneManager.LoadScene(2);
            }

            if (InputManager.Keyboard.f12Key.wasPressedThisFrame)
            {

            }
        }

        /// <summary>
        /// Player가 없으면 카메라를 월드 원점에서 cameraAttachPosition 만큼 올린 위치에 정지시킨다.
        /// 씬 초기(스폰 전) 및 Player 사망 구간에 동일 경로로 동작.
        /// </summary>
        private void LateUpdate()
        {
            if (MapLoader.Player != null) return;

            Camera main = Camera.main;
            if (main == null) return;

            float     height = DataManager.Instance.HumanParameterData.humanGeneralData.cameraAttachPosition;
            Transform tr     = main.transform;

            if (tr.parent != null) tr.SetParent(null, true);
            tr.SetPositionAndRotation(new Vector3(0f, height, 0f), Quaternion.identity);
        }
    }
}
