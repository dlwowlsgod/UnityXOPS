using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 메인 카메라에 부착해, 씬 진입 시 렌더 환경을 적용한다.
    /// - 카메라 near/far 클리핑 면: Graphic 설정(config.json)에서. 옵션 UI엔 없고 json 직접 편집으로 조절한다.
    ///   far 는 fog가 완전히 가리는 거리보다 넉넉히 크게 두면 되고(그 너머는 fog가 가려 클리핑이 안 보임), fog로 시야 연출한다.
    /// - 안개 (원본 SetFog): SkyData(sky_data.json) 기반. RenderSettings 는 씬별 자산이라 fog가 보여야 할 씬의 카메라가 직접 적용한다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraClippingApplier : MonoBehaviour
    {
        private void Awake()
        {
            var cam = GetComponent<Camera>();

            // near/far 클리핑은 config에서(옵션 UI 미노출, json 편집 전용). ConfigManager 없으면 카메라 기존값 유지.
            if (ConfigManager.Loaded)
            {
                cam.nearClipPlane = ConfigManager.Instance.GetFloat("Graphic", "nearClippingPlane", cam.nearClipPlane);
                cam.farClipPlane = ConfigManager.Instance.GetFloat("Graphic", "farClippingPlane", cam.farClipPlane);
            }

            // fog 는 SkyData 기반, RenderSettings(씬별)에 직접 건다. skyIndex 는 Init에서 이미 로드된 값 사용.
            if (DataManager.Instance != null && DataManager.Instance.SkyData != null)
            {
                MapLoader.ApplySkyFog(MapLoader.Instance.SkyIndex);
            }
        }
    }
}
