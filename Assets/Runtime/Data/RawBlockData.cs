using System;
using UnityEngine;

namespace UnityXOPS
{
    [Serializable]
    public class RawBlockData
    {
        public Vector3[] vertices;
        public Vector2[] uvs;
        public int[] textureIndices;
        public int flag; 
        
        public int[] subMeshTextureIndices;
        public Material[] subMeshMaterials;
        public Vector3 center;
    }
}