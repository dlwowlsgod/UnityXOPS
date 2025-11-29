using System;
using UnityEngine;

namespace UnityXOPS
{
    [Serializable]
    public class HumanVisualParameterJSON : ParameterJSON
    {
        public string[] textures;
        public ModelData[] models;
        public int[] textureIndices;
        public int armIndex;
        public int armTextureIndex;
        public int legIndex;
        public int legTextureIndex;
    }
}