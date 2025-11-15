using UnityEngine;

namespace UnityXOPS
{
    [CreateAssetMenu(fileName = "SkyParameter", menuName = "XOPS Parameter/Sky Parameter", order = 1000)]
    public class SkyParameterSO : ParameterSO
    {
        public ScaleAndOffset frontScaleAndOffset;
        public ScaleAndOffset backScaleAndOffset;
        public ScaleAndOffset leftScaleAndOffset;
        public ScaleAndOffset rightScaleAndOffset;
        public ScaleAndOffset upScaleAndOffset;
        public ScaleAndOffset downScaleAndOffset;
        public string[] skyTextures;
        
        public override ParameterJSON Serialize()
        {
            return new SkyParameterJSON
            {
                name = name,
                frontScaleAndOffset = frontScaleAndOffset,
                backScaleAndOffset = backScaleAndOffset,
                leftScaleAndOffset = leftScaleAndOffset,
                rightScaleAndOffset = rightScaleAndOffset,
                upScaleAndOffset = upScaleAndOffset,
                downScaleAndOffset = downScaleAndOffset,
                skyTextures = skyTextures
            };
        }

        public override ParameterSO Deserialize(ParameterJSON json)
        {
            if (!(json is SkyParameterJSON skyJson))
            {
                return null;
            }
            
            name = skyJson.name;
            frontScaleAndOffset = skyJson.frontScaleAndOffset;
            backScaleAndOffset = skyJson.backScaleAndOffset;
            leftScaleAndOffset = skyJson.leftScaleAndOffset;
            rightScaleAndOffset = skyJson.rightScaleAndOffset;
            upScaleAndOffset = skyJson.upScaleAndOffset;
            downScaleAndOffset = skyJson.downScaleAndOffset;
            skyTextures = skyJson.skyTextures;

            return this;
        }
    }
}