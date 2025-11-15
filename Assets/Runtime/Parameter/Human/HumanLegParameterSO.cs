using UnityEngine;

namespace UnityXOPS
{
    [CreateAssetMenu(fileName = "HumanLegParameter", menuName = "XOPS Parameter/Human Leg Parameter", order = 7)]
    public class HumanLegParameterSO : ParameterSO
    {
        public string[] legModels;
        
        public override ParameterJSON Serialize()
        {
            return new HumanLegParameterJSON
            {
                name = name,
                legModels = legModels
            };
        }
        
        public override ParameterSO Deserialize(ParameterJSON json)
        {
            if (!(json is HumanLegParameterJSON legJson))
            {
                return null;
            }
            
            name = legJson.name;
            legModels = legJson.legModels;

            return this;
        }
    }
}
