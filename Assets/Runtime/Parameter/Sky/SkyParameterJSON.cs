using System;

namespace UnityXOPS
{
    [Serializable]
    public class SkyParameterJSON : ParameterJSON
    {
        public ScaleAndOffset frontScaleAndOffset;
        public ScaleAndOffset backScaleAndOffset;
        public ScaleAndOffset leftScaleAndOffset;
        public ScaleAndOffset rightScaleAndOffset;
        public ScaleAndOffset upScaleAndOffset;
        public ScaleAndOffset downScaleAndOffset;
        public string[] skyTextures;
    }
}