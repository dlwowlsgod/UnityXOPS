using System;

namespace UnityXOPS
{
    [Serializable]
    public class HumanArmParameterJSON : ParameterJSON
    {
        public string[] armModelsLeft;
        public string[] armModelsRight;
    }
}