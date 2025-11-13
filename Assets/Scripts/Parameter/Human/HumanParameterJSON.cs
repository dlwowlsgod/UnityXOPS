using System;

namespace UnityXOPS
{
    [Serializable]
    public class HumanParameterJSON : ParameterJSON
    {
        public string[] armName;
        public string[] legName;
        public int[] walkAnimationIndices;
        public int[] runAnimationIndices;
        public float walkAnimationSpeed;
        public float runAnimationSpeed;
        public HumanDataParameterJSON[] humanDataParameterJSONs;
        public HumanVisualParameterJSON[] humanVisualParameterJSONs;
        public HumanTypeParameterJSON[] humanTypeParameterJSONs;   
        public HumanAIParameterJSON[] humanAIParameterJSONs;
        public HumanArmParameterJSON[] humanArmParameterJSONs;
        public HumanLegParameterJSON[] humanLegParameterJSONs;
    }
}