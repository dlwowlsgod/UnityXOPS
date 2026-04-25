using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using JJLUtility;

namespace UnityXOPS
{
    /// <summary>
    /// 오프닝 시퀀스를 관리하고 종료 조건에 따라 메인메뉴 씬으로 전환하는 컨트롤러.
    /// </summary>
    public class OpeningScene : MonoBehaviour
    {
        [SerializeField]
        private OpeningData openingData;
        public OpeningData OpeningData => openingData;

        private const string k_openingDataPath = "unitydata/opening_data.json";

        private float m_time;
        private float m_endTime = 0;

        /// <summary>
        /// 오프닝 데이터를 로드하고 맵 및 스카이 데이터를 초기화한다.
        /// </summary>
        private void Start()
        {
            m_time = Time.time;
            InputManager.MouseCursorMode(true, false, true);

            string fullPath = Path.Combine(Application.streamingAssetsPath, k_openingDataPath);
            string openingDataFile = File.ReadAllText(fullPath);
            openingData = JsonUtility.FromJson<OpeningData>(openingDataFile);

            openingData.openingBD1Path = SafePath.Combine(Application.streamingAssetsPath, openingData.openingBD1Path);
            openingData.openingPD1Path = SafePath.Combine(Application.streamingAssetsPath, openingData.openingPD1Path);

            MapLoader.LoadBlockData(openingData.openingBD1Path);
            MapLoader.LoadPointData(openingData.openingPD1Path);
            MapLoader.LoadSkyData(openingData.openingSkyIndex);
            HumanController.TickEnabled = true;

            m_endTime = openingData.openingFadeData.fadeOutEnd + 1.1f;
        }

        /// <summary>
        /// 오프닝 종료 시간 초과 또는 입력 감지 시 씬을 전환한다.
        /// </summary>
        private void Update()
        {
            float t = Time.time - m_time;
            bool pressed = InputManager.Keyboard.escapeKey.wasPressedThisFrame || InputManager.Mouse.leftButton.wasPressedThisFrame;
            
            if (t > m_endTime || pressed)
            {
                MapLoader.UnloadBlockData();
                MapLoader.UnloadPointData();
                MapLoader.UnloadSkyData();
                HumanController.TickEnabled = false;
                Camera.main.gameObject.SetActive(false);
                SceneManager.LoadScene(2);
            }
        }
    }
}