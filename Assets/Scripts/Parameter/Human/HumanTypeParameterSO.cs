using UnityEngine;

namespace UnityXOPS
{
    [CreateAssetMenu(fileName = "HumanTypeParameter", menuName = "XOPS Parameter/Human Type Parameter", order = 4)]
    public class HumanTypeParameterSO : ParameterSO
    {
        public string type;
        
        public override ParameterJSON Serialize()
        {
            return new HumanTypeParameterJSON
            {
                name = name,
                type = type
            };
        }
        
        public override ParameterSO Deserialize(ParameterJSON json)
        {
            if (!(json is HumanTypeParameterJSON typeJson))
            {
                return null;
            }
            
            name = typeJson.name;
            type = typeJson.type;

            return this;
        }
    }
}
