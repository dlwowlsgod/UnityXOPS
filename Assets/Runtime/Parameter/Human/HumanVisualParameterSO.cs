using UnityEngine;

namespace UnityXOPS
{
    [CreateAssetMenu(fileName = "HumanVisualParameter", menuName = "XOPS Parameter/Human Visual Parameter", order = 3)]
    public class HumanVisualParameterSO : ParameterSO
    {
        public string[] textures;
        public ModelData[] models;
        public int armIndex;
        public int armTextureIndex;
        public int legIndex;
        public int legTextureIndex;
        
        public override ParameterJSON Serialize()
        {
            return new HumanVisualParameterJSON
            {
                name = name,
                textures = textures,
                models = models,
                armIndex = armIndex, 
                armTextureIndex = armTextureIndex,
                legIndex = legIndex,
                legTextureIndex = legTextureIndex
            };
        }
        
        public override ParameterSO Deserialize(ParameterJSON json)
        {
            if (!(json is HumanVisualParameterJSON visualJson))
            {
                return null;
            }
            
            name = visualJson.name;
            textures = visualJson.textures;
            models = visualJson.models;
            armIndex = visualJson.armIndex;
            armTextureIndex = visualJson.armTextureIndex;
            legIndex = visualJson.legIndex;
            legTextureIndex = visualJson.legTextureIndex;
            
            return this;
        }
    }
}
