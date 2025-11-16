using UnityEngine;
using UnityEngine.Serialization;

namespace UnityXOPS
{
    [CreateAssetMenu(fileName = "HumanDataParameter", menuName = "XOPS Parameter/Human Data Parameter", order = 2)]
    public class HumanDataParameterSO : ParameterSO
    {
        public int hp;
        public int weapon0Index;
        public int weapon1Index;
        public int visualIndex;
        public string typeClass;
        public string aiClass;
        
        public override ParameterJSON Serialize()
        {
            return new HumanDataParameterJSON
            {
                name = name,
                hp = hp,
                weapon0Index = weapon0Index,
                weapon1Index = weapon1Index,
                visualIndex = visualIndex,
                typeClass = typeClass,
                aiClass = aiClass
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
            typeClass = dataJson.typeClass;
            aiClass = dataJson.aiClass;

            return this;
        }
    }
}