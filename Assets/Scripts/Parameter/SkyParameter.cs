using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityXOPS
{
    [CreateAssetMenu(fileName = "SkyParameter", menuName = "UnityXOPS/SkyParameter")]
    public class SkyParameter : ScriptableObject
    {
        public string finalName;
        public string skyTexturePath;
        public string billboardTexturePath;
        public string cloudTexturePath;
        public string lightTexturePath;
        public bool light;
        public float lightStrength;
        public Color lightColor;
        public Vector2 lightDirection;
    }
    
    [Serializable]
    public class SkyParameterWrapper : IParameterData
    {
        public string finalName;
        public string skyTexturePath;
        public string billboardTexturePath;
        public string cloudTexturePath;
        public string lightTexturePath;
        public bool light;
        public float lightStrength;
        public Color lightColor;
        public Vector2 lightDirection;
        
        public string FinalName => finalName;
    }
    
    [Serializable]
    public class SkyParameterList : IParameterList<SkyParameterWrapper>
    {
        public List<SkyParameterWrapper> items;
        public List<SkyParameterWrapper> Items => items;
    }
}