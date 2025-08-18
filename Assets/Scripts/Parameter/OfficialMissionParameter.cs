using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityXOPS
{
    [CreateAssetMenu(fileName = "OfficialMissionParameter", menuName = "UnityXOPS/OfficialMissionParameter")]
    public class OfficialMissionParameter : ScriptableObject
    {
        public string finalName;
        public string longName;
        public string bd1Path;
        public string pd1Path;
        public string txtPath;
        public bool adjustCollision;
        public bool darkScreen;
    }

    [Serializable]
    public class OfficialMissionParameterWrapper : IParameterData
    {
        public string finalName;
        public string longName;
        public string bd1Path;
        public string pd1Path;
        public string txtPath;
        public bool adjustCollision;
        public bool darkScreen;
        
        public string FinalName => finalName;
    }
    
    [Serializable]
    public class OfficialMissionParameterList : IParameterList<OfficialMissionParameterWrapper>
    {
        public List<OfficialMissionParameterWrapper> items;
        public List<OfficialMissionParameterWrapper> Items => items;
    }
}