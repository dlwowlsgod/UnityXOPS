using System;
using UnityEngine;

namespace UnityXOPS
{
    [Serializable]
    public class ModelData
    {
        public string path;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
        public int textureIndex;
    }
}