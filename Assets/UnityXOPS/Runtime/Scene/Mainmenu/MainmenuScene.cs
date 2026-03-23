using JJLUtility;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityXOPS
{
    /// <summary>
    /// 메인메뉴 씬을 관리하고 데모 맵 로드 및 미션 씬 전환을 처리하는 컨트롤러.
    /// </summary>
    public class MainmenuScene : MonoBehaviour
    {
        public static bool    IsAddonTab          = false;
        public static int     OfficialScrollIndex = 0;
        public static int     AddonScrollIndex    = 0;

        [SerializeField]
        private GameObject switchCanvas;

        /// <summary>
        /// 랜덤 데모 맵을 로드하고, 애드온 미션이 없을 경우 탭 전환 UI를 비활성화한다.
        /// </summary>
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
            
            MapLoader.LoadSkyData(demo.skyIndex);


            int addonDataCount = DataManager.Instance.MissionData.addonMissions.Count;
            if (addonDataCount == 0)
            {
                switchCanvas.SetActive(false);
            }
        }

        /// <summary>
        /// ESC 키 입력을 처리한다.
        /// </summary>
        private void Update()
        {
            if (InputManager.Keyboard.escapeKey.wasPressedThisFrame)
            {
                Debugger.Log("exit.");
            }
        }

        /// <summary>
        /// 선택된 미션 데이터를 로드하고 브리핑 씬으로 전환한다.
        /// </summary>
        /// <param name="index">미션 인덱스.</param>
        /// <param name="mif">true이면 애드온 미션, false이면 공식 미션.</param>
        public void Load(int index, bool mif)
        {
            MapLoader.UnloadBlockData();
            //
            MapLoader.UnloadBlockData();
            MapLoader.LoadMissionData(index, mif);
            Camera.main.gameObject.SetActive(false);
            SceneManager.LoadScene(3);
        }
    }
}
