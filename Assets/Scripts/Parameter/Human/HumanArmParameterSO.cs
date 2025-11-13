using UnityEngine;

namespace UnityXOPS
{
    [CreateAssetMenu(fileName = "HumanArmParameter", menuName = "XOPS Parameter/Human Arm Parameter", order = 6)]
    public class HumanArmParameterSO : ParameterSO
    {
        public string[] armModelsLeft;
        public string[] armModelsRight;
        
        public override ParameterJSON Serialize()
        {
            return new HumanArmParameterJSON
            {
                name = name,
                armModelsLeft = armModelsLeft,
                armModelsRight = armModelsRight
            };
        }
        public override ParameterSO Deserialize(ParameterJSON json)
        {
            if (!(json is HumanArmParameterJSON armJson))
            {
                return null;
            }
            
            name = armJson.name;
            armModelsLeft = armJson.armModelsLeft;
            armModelsRight = armJson.armModelsRight;

            return this;
        }
    }
}
