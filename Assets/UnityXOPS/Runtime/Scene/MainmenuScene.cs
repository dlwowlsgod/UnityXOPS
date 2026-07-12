using JJLUtility;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 메인메뉴 씬 부트스트랩 — 데모 배경 맵을 로드한다. UI·상호작용·미션 전환은 mainmenu.lua가 담당한다.
    /// </summary>
    public class MainmenuScene : MonoBehaviour
    {
        private void Start()
        {
            InputManager.MouseCursorMode(true, false, true);

            var demoData = DataManager.Instance.MissionData.demoData;

            if (demoData.Count == 0)
            {
                return;
            }

            int random = Random.Range(0, demoData.Count);

            var demo = demoData[random];

            string bd1FullPath = SafePath.Combine(Application.streamingAssetsPath, demo.bd1Path);
            string pd1FullPath = SafePath.Combine(Application.streamingAssetsPath, demo.pd1Path);

            MapLoader.LoadBlockData(bd1FullPath);
            MapLoader.LoadPointData(pd1FullPath);
            MapLoader.LoadSkyData(demo.skyIndex);
            HumanController.TickEnabled = true;
        }
    }
}
