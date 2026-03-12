using UnityEngine;
using System;

namespace UnityXOPS
{
    [Serializable]
    public class Block
    {
        public Mesh mesh;
        public int[] subMeshTextureIndices;
        public Vector3 position;
        public bool collider;
    }
}