using UnityEngine;

namespace UnityXOPS
{
    [CreateAssetMenu(fileName = "HumanDataParameter", menuName = "XOPS Parameter/Human Data Parameter", order = 2)]
    public class HumanDataParameterSO : ParameterSO
    {
        public int hp;
        public int weapon0Index;
        public int weapon1Index;
        public int visualIndex;
        public int typeIndex;
        public int aiIndex;
        
        public override ParameterJSON Serialize()
        {
            return new HumanDataParameterJSON
            {
                name = name,
                hp = hp,
                weapon0Index = weapon0Index,
                weapon1Index = weapon1Index,
                visualIndex = visualIndex,
                typeIndex = typeIndex,
                aiIndex = aiIndex
            };
        }
        
        public override ParameterSO Deserialize(ParameterJSON json)
        {
            if (!(json is HumanDataParameterJSON dataJson))
            {
                return null;
            }

            name = dataJson.name;
            hp = dataJson.hp;
            weapon0Index = dataJson.weapon0Index;
            weapon1Index = dataJson.weapon1Index;
            visualIndex = dataJson.visualIndex;
            typeIndex = dataJson.typeIndex;
            aiIndex = dataJson.aiIndex;

            return this;
        }
    }
}