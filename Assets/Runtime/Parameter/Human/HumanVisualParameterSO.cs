using UnityEngine;

namespace UnityXOPS
{
    [CreateAssetMenu(fileName = "HumanVisualParameter", menuName = "XOPS Parameter/Human Visual Parameter", order = 3)]
    public class HumanVisualParameterSO : ParameterSO
    {
        public string[] textures;
        public string[] models;
        public Vector3[] positions;
        public Vector3[] rotations;
        public Vector3[] scales;
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
                positions = positions, 
                rotations = rotations, 
                scales = scales,
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
            positions = visualJson.positions; 
            rotations = visualJson.rotations; 
            scales = visualJson.scales;
            textureIndices = visualJson.textureIndices;
            armIndex = visualJson.armIndex;
            legIndex = visualJson.legIndex;

            return this;
        }
    }
}
