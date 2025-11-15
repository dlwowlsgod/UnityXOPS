using UnityEngine;

namespace UnityXOPS
{
    [CreateAssetMenu(fileName = "HumanAIParameter", menuName = "XOPS Parameter/Human AI Parameter", order = 5)]
    public class HumanAIParameterSO : ParameterSO
    {
        public string aiName;
        
        public override ParameterJSON Serialize()
        {
            return new HumanAIParameterJSON
            {
                name = name,
                aiName = aiName
            };
        }

        public override ParameterSO Deserialize(ParameterJSON json)
        {
            if (!(json is HumanAIParameterJSON aiJson))
            {
                return null;
            }
            
            name = aiJson.name;
            aiName = aiJson.aiName;

            return this;
        }
    }
}
