using JJLUtility;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 맵 로드/언로드 싱글톤의 공유 코어. 블록·포인트·스카이·미션 각 관심사는 동명 partial 파일
    /// (BlockData/PointData/Sky.SkyLoad/Mission.MissionData)에서 처리하고, 이 파일은 관심사 공용 유틸만 보유한다.
    /// </summary>
    public partial class MapLoader : SingletonBehavior<MapLoader>
    {
        // 공유 머티리얼(MaterialManager 원본)은 건드리지 않고 런타임 생성분만 파괴
        private static void DestroyIfRuntimeMaterial(Material material)
        {
            if (material == null) return;
            var mm = MaterialManager.Instance;
            if (material == mm.MainMaterial || material == mm.BlockMaterial ||
                material == mm.TransparentMaterial || material == mm.EffectMaterial ||
                material == mm.SkyMaterial)
            {
                return;
            }
            Destroy(material);
        }
    }
}
