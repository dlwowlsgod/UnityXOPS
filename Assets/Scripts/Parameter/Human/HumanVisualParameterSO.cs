using UnityEngine;

namespace UnityXOPS
{
    [CreateAssetMenu(fileName = "HumanVisualParameter", menuName = "XOPS Parameter/Human Visual Parameter", order = 3)]
    public class HumanVisualParameterSO : ParameterSO
    {
        public string[] textures;
        public string[] models;
        public int[] textureIndices;
        public int armIndex;
        public int legIndex;
        
        public override ParameterJSON Serialize()
        {
            return new HumanVisualParameterJSON
            {
                name = name,
                textures = textures,
                models = models,
                textureIndices = textureIndices,
                armIndex = armIndex, 
                legIndex = legIndex
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
            textureIndices = visualJson.textureIndices;
            armIndex = visualJson.armIndex;
            legIndex = visualJson.legIndex;

            return this;
        }
    }
}
