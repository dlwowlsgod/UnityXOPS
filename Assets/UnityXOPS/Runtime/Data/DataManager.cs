using UnityEngine;
using JJLUtility;
using System.IO;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 게임 데이터(스카이, 미션)를 JSON에서 로드하고 전역 접근을 제공하는 싱글톤 매니저.
    /// </summary>
    public partial class DataManager : SingletonBehavior<DataManager>
    {
        [SerializeField]
        private HumanParameterData humanParameterData;
        public HumanParameterData HumanParameterData => humanParameterData;

        [SerializeField]
        private WeaponParameterData weaponParameterData;
        public WeaponParameterData WeaponParameterData => weaponParameterData;

        [SerializeField]
        private ObjectParameterData objectParameterData;
        public ObjectParameterData ObjectParameterData => objectParameterData;

        [SerializeField]
        private EffectParameterData effectParameterData;
        public EffectParameterData EffectParameterData => effectParameterData;

        [SerializeField]
        private SkyData skyData;
        public SkyData SkyData => skyData;

        [SerializeField]
        private MissionData missionData;
        public MissionData MissionData => missionData;

        [SerializeField]
        private GlobalData globalData;
        public GlobalData GlobalData => globalData;

        private void Start()
        {
            LoadHumanParameterData();
            LoadWeaponParameterData();
            LoadObjectParameterData();
            LoadEffectParameterData();
            LoadSkyData();
            LoadMissionData();
            LoadGlobalData();
        }

        private void LoadHumanParameterData()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, k_humanParameterDataPath);
            string json = EncodingHelper.ReadAllText(fullPath);
            humanParameterData = JsonUtility.FromJson<HumanParameterData>(json);
        }

        private void LoadWeaponParameterData()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, k_weaponParameterDataPath);
            string json = EncodingHelper.ReadAllText(fullPath);
            weaponParameterData = JsonUtility.FromJson<WeaponParameterData>(json);
        }

        private void LoadObjectParameterData()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, k_objectParameterDataPath);
            string json = EncodingHelper.ReadAllText(fullPath);
            objectParameterData = JsonUtility.FromJson<ObjectParameterData>(json);
        }

        private void LoadEffectParameterData()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, k_effectParameterDataPath);
            string json = EncodingHelper.ReadAllText(fullPath);
            effectParameterData = JsonUtility.FromJson<EffectParameterData>(json);
        }

        private void LoadSkyData()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, k_skyDataPath);
            string json = EncodingHelper.ReadAllText(fullPath);
            skyData = JsonUtility.FromJson<SkyData>(json);
        }

        private void LoadMissionData()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, k_missionDataPath);
            string json = EncodingHelper.ReadAllText(fullPath);
            missionData = JsonUtility.FromJson<MissionData>(json);
            missionData.addonMissions = new List<AddonMissionData>();

            string addonPath = Path.Combine(Application.streamingAssetsPath, "addon");
            if (Directory.Exists(addonPath))
            {
                string[] mifPaths = Directory.GetFiles(addonPath, "*.mif");
                foreach (var path in mifPaths)
                {
                    var addonData = new AddonMissionData();
                    addonData.mifPath = path;
                    var lines = EncodingHelper.ReadAllLines(addonData.mifPath);
                    var name = lines.Length > 0 ? lines[0] : string.Empty;
                    addonData.name = string.IsNullOrEmpty(name) ? string.Empty : name;
                    missionData.addonMissions.Add(addonData);
                }
            }
        }

        /// <summary>
        /// 전역 데이터를 JSON에서 로드한다. 파일이 없으면 기본값으로 새 JSON 파일을 생성한 뒤 로드한다.
        /// </summary>
        private void LoadGlobalData()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, k_globalDataPath);
            if (File.Exists(fullPath))
            {
                string json = EncodingHelper.ReadAllText(fullPath);
                globalData = JsonUtility.FromJson<GlobalData>(json);
            }
            else
            {
                string directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(fullPath, k_globalJSONOriginal);
                globalData = JsonUtility.FromJson<GlobalData>(k_globalJSONOriginal);
            }
        }
    }
}