using UnityEngine;

namespace UnityXOPS
{
    [CreateAssetMenu(fileName = "OfficialMissionParameter", menuName = "XOPS Parameter/Official Mission Parameter", order = 1001)]
    public class OfficialMissionParameterSO : ParameterSO
    {
        public string fullName;
        public string txtPath;
        public string bd1Path;
        public string pd1Path;
        public bool adjustCollision;
        public bool darkScreen;
        
        public override ParameterJSON Serialize()
        {
            return new OfficialMissionParameterJSON
            {
                name = name,
                fullName = fullName,
                txtPath = txtPath,
                bd1Path = bd1Path,
                pd1Path = pd1Path,
                adjustCollision = adjustCollision,
                darkScreen = darkScreen
            };
        }

        public override ParameterSO Deserialize(ParameterJSON json)
        {
            if (!(json is OfficialMissionParameterJSON missionJson))
            {
                return null;
            }
            
            name = missionJson.name;
            fullName = missionJson.fullName;
            txtPath = missionJson.txtPath;
            bd1Path = missionJson.bd1Path;
            pd1Path = missionJson.pd1Path;
            adjustCollision = missionJson.adjustCollision;
            darkScreen = missionJson.darkScreen;
            
            return this;
        }
    }
}
