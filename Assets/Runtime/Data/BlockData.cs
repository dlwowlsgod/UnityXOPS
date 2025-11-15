using System;
using UnityEngine;

namespace UnityXOPS
{
    [Serializable]
    public class BlockData
    {
        public RawBlockData[] rawBlockData;
        public Material[] textures;
        public Mesh[] blocks;
    }
}