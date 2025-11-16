using System;
using UnityEngine;

namespace UnityXOPS
{
    [Serializable]
    public class HumanParameterJSON : ParameterJSON
    {
        public string[] armName;
        public string[] legName;
        public int[] walkAnimationIndices;
        public int[] runAnimationIndices;
        public Vector3 armRootPosition;
        public Vector3 armRootScale;
        public Vector3 legRootPosition;
        public Vector3 legRootScale;
        public HumanDataParameterJSON[] humanDataParameterJSONs;
        public HumanVisualParameterJSON[] humanVisualParameterJSONs;
        public HumanArmParameterJSON[] humanArmParameterJSONs;
        public HumanLegParameterJSON[] humanLegParameterJSONs;
    }
}