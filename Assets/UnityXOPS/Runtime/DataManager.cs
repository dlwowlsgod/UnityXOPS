using UnityEngine;
using JJLUtility;
using System.IO;

namespace UnityXOPS
{
    public class DataManager : SingletonBehavior<DataManager>
    {
        [SerializeField]
        private SkyData skyData;
        public SkyData SkyData => skyData;

        [SerializeField]
        private MissionData missionData;
        public MissionData MissionData => missionData;

        private const string skyDataPath = "unitydata/sky_data.json";
        private const string missionDataPath = "unitydata/mission_data.json";

        public void Start()
        {
            LoadSkyData();
            LoadMissionData();
        }

        private void LoadSkyData()
        {
            string fullPath = SafePath.Combine(Application.streamingAssetsPath, skyDataPath);
            string json = File.ReadAllText(fullPath);
            skyData = JsonUtility.FromJson<SkyData>(json);
        }

        private void LoadMissionData()
        {
            string fullPath = SafePath.Combine(Application.streamingAssetsPath, missionDataPath);
            string json = File.ReadAllText(fullPath);
            missionData = JsonUtility.FromJson<MissionData>(json);
        }
    }
}