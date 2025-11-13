using System;

namespace UnityXOPS
{
    [Serializable]
    public class HumanVisualParameterJSON : ParameterJSON
    {
        public string[] textures;
        public string[] models;
        public int[] textureIndices;
        public int armIndex;
        public int legIndex;
    }
}