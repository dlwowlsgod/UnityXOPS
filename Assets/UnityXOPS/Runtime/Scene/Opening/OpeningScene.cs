using UnityEngine;
using System.IO;
using JJLUtility;

namespace UnityXOPS
{
    /// <summary>
    /// 오프닝 씬을 부트스트랩하는 컨트롤러. 맵/스카이 데이터를 로드하고,
    /// 씬 종료 시 로드한 리소스를 정리한다. 연출과 씬 전환은 opening.lua가 담당한다.
    /// </summary>
    public class OpeningScene : MonoBehaviour
    {
        private void Start()
        {
            var openingData = DataManager.Instance.MissionData.openingData;

            var openingBD1Path = SafePath.Combine(Application.streamingAssetsPath, openingData.bd1Path);
            var openingPD1Path = SafePath.Combine(Application.streamingAssetsPath, openingData.pd1Path);

            MapLoader.LoadBlockData(openingBD1Path);
            MapLoader.LoadPointData(openingPD1Path);
            MapLoader.LoadSkyData(openingData.skyIndex);
            HumanController.TickEnabled = true;
        }

        private void OnDestroy()
        {
            if (MapLoader.Loaded)
            {
                MapLoader.UnloadBlockData();
                MapLoader.UnloadPointData();
                MapLoader.UnloadSkyData();
            }

            HumanController.TickEnabled = false;
        }
    }
}
