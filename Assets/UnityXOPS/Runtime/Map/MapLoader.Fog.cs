using UnityEngine;

namespace UnityXOPS
{
    public partial class MapLoader
    {
        /// <summary>
        /// 하늘 번호별 fog 색 — SkyData.skyColor (sky_data.json) 에서 skyIndex 로 조회. 원본 SetFog skycolor switch 의 데이터화.
        /// AlphaClipBlend(블록/소물/사람/무기)만 multi_compile_fog 를 가져 fog 대상이 되고, SkyMesh·EffectBlend 는 제외된다.
        /// 데이터가 없거나 범위를 벗어나면 검정(원본 default) 반환.
        /// </summary>
        private static Color SkyFogColor(int skyIndex)
        {
            var colors = DataManager.Instance.SkyData?.skyColor;
            if (colors == null || skyIndex < 0 || skyIndex >= colors.Count)
                return new Color32(0, 0, 0, 255);
            return colors[skyIndex];
        }

        /// <summary>
        /// 원본 SetFog(true, skynumber) 대응. Linear fog 를 SkyData(fogClippingPlane ~ farClippingPlane) 범위, skyIndex 색으로 적용한다.
        /// SkyData.fog 가 false 거나 데이터가 없으면 fog 를 끈다. 맵/스카이 로드 후 호출 (skyIndex 가 세팅된 뒤).
        /// </summary>
        public static void ApplySkyFog(int skyIndex)
        {
            var skyData = DataManager.Instance.SkyData;
            if (skyData == null || !skyData.fog)
            {
                ClearFog();
                return;
            }

            RenderSettings.fog              = true;
            RenderSettings.fogMode          = FogMode.Linear;
            RenderSettings.fogStartDistance = skyData.fogStart;
            RenderSettings.fogEndDistance   = skyData.fogEnd;
            RenderSettings.fogColor         = SkyFogColor(skyIndex);
        }

        /// <summary>원본 SetFog(false, …) 대응. 맵 언로드 시 fog 해제.</summary>
        public static void ClearFog()
        {
            RenderSettings.fog = false;
        }
    }
}
