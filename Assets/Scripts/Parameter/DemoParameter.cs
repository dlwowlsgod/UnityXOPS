using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    [CreateAssetMenu(fileName = "DemoParameter", menuName = "UnityXOPS/DemoParameter")]
    public class DemoParameter : ScriptableObject
    {
        public string finalName;
        public string bd1Path;
        public string pd1Path;
        public int skyIndex;
    }
    
    [Serializable]
    public class DemoParameterWrapper : IParameterData
    {
        public string finalName;
        public string bd1Path;
        public string pd1Path;
        public int skyIndex;
        
        public string FinalName => finalName;
    }
    
    [Serializable]
    public class DemoParameterList : IParameterList<DemoParameterWrapper>
    {
        public List<DemoParameterWrapper> items;
        public List<DemoParameterWrapper> Items => items;
    }
}