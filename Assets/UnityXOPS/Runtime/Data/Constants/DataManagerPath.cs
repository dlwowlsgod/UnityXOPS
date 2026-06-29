using JJLUtility;
using UnityEngine;

namespace UnityXOPS
{
    public partial class DataManager : SingletonBehavior<DataManager>
    {
        private const string k_humanParameterDataPath = "unitydata/human_parameter_data.json";
        private const string k_weaponParameterDataPath = "unitydata/weapon_parameter_data.json";
        private const string k_objectParameterDataPath = "unitydata/object_parameter_data.json";
        private const string k_effectParameterDataPath = "unitydata/effect_parameter_data.json";
        private const string k_skyDataPath = "unitydata/sky_data.json";
        private const string k_missionDataPath = "unitydata/mission_data.json";
        private const string k_globalDataPath = "unitydata/global.json";
    }
}
