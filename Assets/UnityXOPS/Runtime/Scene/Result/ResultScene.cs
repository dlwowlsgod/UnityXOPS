using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityXOPS
{
    public class ResultScene : MonoBehaviour
    {
        
        private void Update()
        {
            // F12 — 같은 미션 맵을 재로드해 Maingame(4)으로 다시 시작. 미션 데이터(경로/스카이)는 Result 진입 시 유지돼 있어 그대로 재사용.
            // 맵을 씬 전환 전에 로드(싱글톤 루트에 스폰 → Maingame 까지 유지). TickEnabled/BeginMission 은 MaingameScene.Start 가 처리.
            if (InputManager.Keyboard.f12Key.wasPressedThisFrame)
            {
                BulletManager.Instance.ClearPool();
                SoundManager.Instance.ClearPool();
                EffectManager.Instance.ClearPool();
                MapLoader.UnloadBlockData();   // Result 진입 시 이미 언로드됐지만 멱등 — 방어적으로 정리 후 재로드.
                MapLoader.UnloadPointData();
                MapLoader.UnloadSkyData();
                MapLoader.LoadBlockData(MapLoader.Instance.MissionBD1Path);
                MapLoader.LoadPointData(MapLoader.Instance.MissionPD1Path);  // 통계 리셋 + 사람/무기/소품 재스폰
                MapLoader.LoadSkyData(MapLoader.Instance.SkyIndex);
                HumanController.TickEnabled = false; // Maingame.Start 가 true 로 + BeginMission 호출
                Camera.main.gameObject.SetActive(false);
                SceneManager.LoadScene(4);
                return;
            }

            // 원본: result 화면에서 ESC 또는 좌클릭 → 메인메뉴 (scene.cpp:78-83 ESC/클릭 배타적, statemachine.cpp:144-150 둘 다 메뉴행).
            if (InputManager.Keyboard.escapeKey.wasPressedThisFrame || InputManager.Mouse.leftButton.wasPressedThisFrame)
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
}
