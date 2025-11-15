using System;
using UnityEngine;

namespace UnityXOPS
{
    [Serializable]
    public class HumanVisualParameterJSON : ParameterJSON
    {
        public string[] textures;
        public string[] models;
        public Vector3[] positions;
        public Vector3[] rotations;
        public Vector3[] scales;
        public int[] textureIndices;
        public int armIndex;
        public int legIndex;
    }
}