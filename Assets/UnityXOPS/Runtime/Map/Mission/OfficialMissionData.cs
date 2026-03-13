using UnityEngine;
using System;

namespace UnityXOPS
{
    [Serializable]
    public class OfficialMissionData
    {
        public string name;
        public string fullname;
        public string bd1Path;
        public string pd1Path;
        public string txtPath;
        public bool adjustCollision;
        public bool darkScreen;
    }
}