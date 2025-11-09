using System;
using UnityEngine;

namespace UnityXOPS
{
    [Serializable]
    public class SkyParameterJSON : IParameter
    {
        public string name;
        public string Name => name;
        
        public Vector2 frontTextureTilting;
        public Vector2 frontTextureOffset;
        public Vector2 backTextureTilting;
        public Vector2 backTextureOffset;
        public Vector2 leftTextureTilting;
        public Vector2 leftTextureOffset;
        public Vector2 rightTextureTilting;
        public Vector2 rightTextureOffset;
        public Vector2 upTextureTilting;
        public Vector2 upTextureOffset;
        public Vector2 downTextureTilting;
        public Vector2 downTextureOffset;
        public string[] skyboxTexturePath;
    }
}