using UnityEngine;
using System;

namespace UnityXOPS
{
    [Serializable]
    public class VisualData
    {
        public string modelPath;
        public RGBATextureData baseTexture = new()
        {
            color = Color.white,
            alpha = 1.0f
        };
        public FloatTextureData metallicTexture = new()
        {
            value = 0.0f
        };
        public FloatTextureData roughnessTexture = new()
        {
            value = 0.5f
        };
        public FloatTextureData normalTexture = new()
        {
            value = 1.0f
        };
        public FloatTextureData occlusionTexture = new()
        {
            value = 1.0f
        };
        public RGBTextureData emissiveTexture  = new()
        {
            color = Color.black
        };
    }
    
    [Serializable]
    public class RGBATextureData : RGBTextureData
    {
        public float alpha = 1.0f;
    }

    [Serializable]
    public class RGBTextureData : TextureData
    {
        public Color color;
    }

    [Serializable]
    public class FloatTextureData : TextureData
    {
        public float value;
    }

    [Serializable]
    public class TextureData
    {
        public string texturePath;
    }
}