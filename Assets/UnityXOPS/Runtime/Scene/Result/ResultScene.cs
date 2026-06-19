using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityXOPS
{
    /// <summary>
    /// 결과 씬의 입력을 처리하는 컨트롤러. F12로 같은 미션 재시작, ESC/좌클릭으로 메인메뉴 복귀.
    /// </summary>
    public class ResultScene : MonoBehaviour
    {
        private void Update()
        {
            if (InputManager.Keyboard.f12Key.wasPressedThisFrame)
            {
                RestartMission();
                return;
            }

            if (InputManager.Keyboard.escapeKey.wasPressedThisFrame || InputManager.Mouse.leftButton.wasPressedThisFrame)
            {
                ReturnToMenu();
            }
        }

        /// <summary>
        /// 같은 미션 맵을 재로드해 Maingame(4)으로 다시 시작한다. 미션 데이터(경로/스카이)는 Result 진입 시 유지돼 그대로 재사용.
        /// </summary>
        private void RestartMission()
        {
            BulletManager.Instance.ClearPool();
            SoundManager.Instance.ClearPool();
            EffectManager.Instance.ClearPool();
            MapLoader.UnloadBlockData(); // Result 진입 시 이미 언로드됐지만 멱등 — 방어적으로 정리 후 재로드.
            MapLoader.UnloadPointData();
            MapLoader.UnloadSkyData();
            MapLoader.LoadBlockData(MapLoader.Instance.MissionBD1Path);
            MapLoader.LoadPointData(MapLoader.Instance.MissionPD1Path); // 통계 리셋 + 사람/무기/소품 재스폰
            MapLoader.LoadSkyData(MapLoader.Instance.SkyIndex);
            HumanController.TickEnabled = false; // Maingame.Start 가 true 로 + BeginMission 호출
            Camera.main.gameObject.SetActive(false);
            SceneManager.LoadScene(4);
        }

        /// <summary>
        /// 맵·미션 데이터를 언로드하고 메인메뉴(2)로 복귀한다. (원본 scene.cpp:78-83 ESC/클릭 배타적)
        /// </summary>
        private void ReturnToMenu()
        {
            MapLoader.UnloadBlockData();
            MapLoader.UnloadPointData();
            MapLoader.UnloadMissionData();
            MapLoader.UnloadSkyData();
            Camera.main.gameObject.SetActive(false);
            SceneManager.LoadScene(2);
        }
    }
}
