using UnityEngine;
using JJLUtility;
using System.IO;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 게임 데이터(스카이, 미션)를 JSON에서 로드하고 전역 접근을 제공하는 싱글톤 매니저.
    /// </summary>
    public class DataManager : SingletonBehavior<DataManager>
    {
        [SerializeField]
        private HumanParameterData humanParameterData;
        public HumanParameterData HumanParameterData => humanParameterData;

        [SerializeField]
        private WeaponParameterData weaponParameterData;
        public WeaponParameterData WeaponParameterData => weaponParameterData;

        [SerializeField]
        private SkyData skyData;
        public SkyData SkyData => skyData;

        [SerializeField]
        private MissionData missionData;
        public MissionData MissionData => missionData;

        private const string k_humanParameterDataPath = "unitydata/human_parameter_data.json";
        private const string k_weaponParameterDataPath = "unitydata/weapon_parameter_data.json";
        private const string k_skyDataPath = "unitydata/sky_data.json";
        private const string k_missionDataPath = "unitydata/mission_data.json";

        /// <summary>
        /// 스카이 데이터와 미션 데이터를 로드한다.
        /// </summary>
        public void Start()
        {
            LoadHumanParameterData();
            LoadWeaponParameterData();
            LoadSkyData();
            LoadMissionData();
        }

        private void LoadHumanParameterData()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, k_humanParameterDataPath);
            string json = File.ReadAllText(fullPath);
            humanParameterData = JsonUtility.FromJson<HumanParameterData>(json);
        }

        private void LoadWeaponParameterData()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, k_weaponParameterDataPath);
            string json = File.ReadAllText (fullPath);
            weaponParameterData = JsonUtility.FromJson<WeaponParameterData>(json);
        }

        /// <summary>
        /// StreamingAssets에서 스카이 데이터 JSON을 읽어 skyData에 역직렬화한다.
        /// </summary>
        private void LoadSkyData()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, k_skyDataPath);
            string json = File.ReadAllText(fullPath);
            skyData = JsonUtility.FromJson<SkyData>(json);
        }

        /// <summary>
        /// StreamingAssets에서 미션 데이터 JSON을 읽고, addon 폴더의 .mif 파일도 스캔해 추가한다.
        /// </summary>
        private void LoadMissionData()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, k_missionDataPath);
            string json = File.ReadAllText(fullPath);
            missionData = JsonUtility.FromJson<MissionData>(json);
            missionData.addonMissions = new List<AddonMissionData>();

            string addonPath = Path.Combine(Application.streamingAssetsPath, "addon");
            if (Directory.Exists(addonPath))
            {
                string[] mifPaths = Directory.GetFiles(addonPath, "*.mif");
                foreach (var path in mifPaths)
                {
                    var addonData = new AddonMissionData();
                    string mifFullPath = SafePath.Combine(addonPath, path);
                    addonData.mifPath = mifFullPath;
                    var name = File.ReadAllLines(addonData.mifPath)[0];
                    addonData.name = string.IsNullOrEmpty(name) ? string.Empty : name;
                    missionData.addonMissions.Add(addonData);
                }
            }
        }
    }
}