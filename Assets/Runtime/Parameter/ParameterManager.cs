using System.IO;
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
        
        public static void Initialize()
        {
            // human
            var humanParameterPath = Path.Combine(Application.streamingAssetsPath, HumanParameterPath);
            if (File.Exists(humanParameterPath))
            {
                var humanText = File.ReadAllText(humanParameterPath);
                var humanJson = JsonUtility.FromJson<HumanParameterJSON>(humanText);
                var humanSO = Instantiate(Instance.humanParameterSO);
                Instance.humanParameterSO = (HumanParameterSO)humanSO.Deserialize(humanJson);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Human parameter file not found: {humanParameterPath}");
#endif
                var humanJson = (HumanParameterJSON)Instance.humanParameterSO.Serialize();
                File.WriteAllText(humanParameterPath, JsonUtility.ToJson(humanJson, true));
            }
            
            
            // sky
            var skyParameterPath = Path.Combine(Application.streamingAssetsPath, SkyParameterPath);
            if (File.Exists(skyParameterPath))
            {
                var skyText = File.ReadAllText(skyParameterPath);
                var skyJson = JsonUtility.FromJson<SkyParameterJSON>(skyText);
                var skySO = Instantiate(Instance.skyParameterSO);
                Instance.skyParameterSO = (SkyParameterSO)skySO.Deserialize(skyJson);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Sky parameter file not found: {skyParameterPath}");
#endif
                var skyJson = (SkyParameterJSON)Instance.skyParameterSO.Serialize();
                File.WriteAllText(skyParameterPath, JsonUtility.ToJson(skyJson, true));
            }
        }
    }
}
