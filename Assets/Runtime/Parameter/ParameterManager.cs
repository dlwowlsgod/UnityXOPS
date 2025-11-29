using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityXOPS
{
    public class ParameterManager : Singleton<ParameterManager>
    {
        [SerializeField]
        private HumanParameterSO humanParameterSO;
        public HumanParameterSO HumanParameterSO => humanParameterSO;
        private const string HumanParameterPath = "common/parameter/human_parameter.json";
        
        [SerializeField]
        private SkyParameterSO skyParameterSO;
        public SkyParameterSO SkyParameterSO => skyParameterSO;
        private const string SkyParameterPath = "common/parameter/sky_parameter.json";
        
        [SerializeField]
        private MissionParameterSO missionParameterSO;
        public MissionParameterSO MissionParameterSO => missionParameterSO;
        private const string MissionParameterPath = "common/parameter/mission_parameter.json";
        
        public static void Initialize()
        {
            var humanSO = Instantiate(Instance.humanParameterSO);
            var skySO = Instantiate(Instance.skyParameterSO);
            var missionSO = Instantiate(Instance.missionParameterSO);
            
            // human
            var humanParameterPath = Path.Combine(Application.streamingAssetsPath, HumanParameterPath);
            if (File.Exists(humanParameterPath))
            {
                var humanText = File.ReadAllText(humanParameterPath);
                var humanJson = JsonUtility.FromJson<HumanParameterJSON>(humanText);
                Instance.humanParameterSO = (HumanParameterSO)humanSO.Deserialize(humanJson);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Human parameter file not found: {humanParameterPath}");
#endif
                Instance.humanParameterSO = humanSO;
                var humanJson = (HumanParameterJSON)Instance.humanParameterSO.Serialize();
                var dir = Path.GetDirectoryName(humanParameterPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(humanParameterPath, JsonUtility.ToJson(humanJson, true));
            }
            
            
            // sky
            var skyParameterPath = Path.Combine(Application.streamingAssetsPath, SkyParameterPath);
            if (File.Exists(skyParameterPath))
            {
                var skyText = File.ReadAllText(skyParameterPath);
                var skyJson = JsonUtility.FromJson<SkyParameterJSON>(skyText);
                Instance.skyParameterSO = (SkyParameterSO)skySO.Deserialize(skyJson);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Sky parameter file not found: {skyParameterPath}");
#endif
                Instance.skyParameterSO = skySO;
                var skyJson = (SkyParameterJSON)Instance.skyParameterSO.Serialize();
                var dir = Path.GetDirectoryName(skyParameterPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(skyParameterPath, JsonUtility.ToJson(skyJson, true));
            }
            
            // mission
            var missionParameterPath = Path.Combine(Application.streamingAssetsPath, MissionParameterPath);
            if (File.Exists(missionParameterPath))
            {
                var missionText = File.ReadAllText(missionParameterPath);
                var missionJson = JsonUtility.FromJson<MissionParameterJSON>(missionText);
                
                Instance.missionParameterSO = (MissionParameterSO)missionSO.Deserialize(missionJson);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Mission parameter file not found: {missionParameterPath}");
#endif
                Instance.missionParameterSO = missionSO;
                var missionJson = (MissionParameterJSON)Instance.missionParameterSO.Serialize();
                var dir = Path.GetDirectoryName(missionParameterPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(missionParameterPath, JsonUtility.ToJson(missionJson, true));
            }
            
            string[] mifPaths = null;
            var addonDirectory = SafeIO.Combine(Application.streamingAssetsPath, "addon");
            if (Directory.Exists(addonDirectory))
            {
                mifPaths = Directory.GetFiles(addonDirectory, "*.mif");
            }

            if (mifPaths != null && mifPaths.Length != 0)
            {
                List<AddonMissionParameterSO> addons = mifPaths.Select(mif => MIFLoader.LoadMIF(mif)).ToList();
                addons.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
                missionSO.addonMissionParameterSOs = addons.ToArray();
            }
            
            
        }
    }
}
