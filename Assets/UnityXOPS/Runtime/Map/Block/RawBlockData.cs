using System;
using UnityEngine;

namespace UnityXOPS
{
    [Serializable]
    public struct RawBlockData
    {
        public Vector3[] vertices;
        public Vector2[] uvs;
        public int[] textureIndices;
        public int flag;
    }
}