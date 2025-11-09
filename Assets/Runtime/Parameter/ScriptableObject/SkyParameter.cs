using UnityEngine;
using UnityEngine.Serialization;

namespace UnityXOPS
{
    [CreateAssetMenu(fileName = "SkyParameter", menuName = "UnityXOPS/SkyParameter")]
    public class SkyParameter : ScriptableObject
    {
        [Header("Scale and Offset")]
        [Tooltip("Front (+Z) Scale and Offset.")]
        public ScaleAndOffset frontTextureScaleAndOffset;
        [Tooltip("Back (-Z) Scale and Offset.")]
        public ScaleAndOffset backTextureScaleAndOffset;
        [Tooltip("Left (+X) Scale and Offset.")]
        public ScaleAndOffset leftTextureScaleAndOffset;
        [Tooltip("Right (-X) Scale and Offset.")]
        public ScaleAndOffset rightTextureScaleAndOffset;
        [Tooltip("Up (+Y) Scale and Offset.")]
        public ScaleAndOffset upTextureScaleAndOffset;
        [Tooltip("Down (-Y) Scale and Offset.")]
        public ScaleAndOffset downTextureScaleAndOffset;
        [Header("Texture Paths")]
        public string[] skyboxTexturePath;
    }
}
