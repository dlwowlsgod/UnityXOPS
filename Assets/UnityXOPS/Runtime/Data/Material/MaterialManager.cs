using JJLUtility;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 미션에서 사용되는 머티리얼을 관리하는 싱글톤 클래스.
    /// </summary>
    public class MaterialManager : SingletonBehavior<MaterialManager>
    {
        [SerializeField]
        private Material mainMaterial, blockMaterial, transparentMaterial, effectMaterial, skyMaterial;
        public Material MainMaterial => mainMaterial;
        public Material BlockMaterial => blockMaterial;
        public Material TransparentMaterial => transparentMaterial;
        public Material EffectMaterial => effectMaterial;
        public Material SkyMaterial => skyMaterial;
    }
}