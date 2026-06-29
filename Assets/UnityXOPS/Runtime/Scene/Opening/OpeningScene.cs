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
        [SerializeField]
        private OpeningData openingData;
        public OpeningData OpeningData => openingData;

        private const string k_openingDataPath = "unitydata/opening_data.json";

        private void Start()
        {
            InputManager.MouseCursorMode(true, false, true);

            string fullPath = Path.Combine(Application.streamingAssetsPath, k_openingDataPath);
            string openingDataFile = EncodingHelper.ReadAllText(fullPath);
            openingData = JsonUtility.FromJson<OpeningData>(openingDataFile);

            openingData.openingBD1Path = SafePath.Combine(Application.streamingAssetsPath, openingData.openingBD1Path);
            openingData.openingPD1Path = SafePath.Combine(Application.streamingAssetsPath, openingData.openingPD1Path);

            MapLoader.LoadBlockData(openingData.openingBD1Path);
            MapLoader.LoadPointData(openingData.openingPD1Path);
            MapLoader.LoadSkyData(openingData.openingSkyIndex);
            HumanController.TickEnabled = true;
        }

        private void OnDestroy()
        {
            if (BulletManager.Loaded) BulletManager.Instance.ClearPool();
            if (SoundManager.Loaded) SoundManager.Instance.ClearPool();
            if (EffectManager.Loaded) EffectManager.Instance.ClearPool();

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
