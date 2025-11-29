using System;

namespace UnityXOPS
{
    [Serializable]
    public class OfficialMissionParameterJSON : ParameterJSON
    {
        public string fullName;
        public string txtPath;
        public string bd1Path;
        public string pd1Path;
        public bool adjustCollision;
        public bool darkScreen;
    }
}