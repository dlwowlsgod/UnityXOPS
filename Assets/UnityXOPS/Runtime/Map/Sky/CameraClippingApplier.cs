using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 메인 카메라에 부착해, SkyData(sky_data.json) 기반 렌더 환경을 씬 진입 시 적용한다.
    /// - 카메라 near/far 클리핑 면 (원본 CLIPPINGPLANE_NEAR/FAR ×0.1)
    /// - 안개 (원본 SetFog) — RenderSettings 는 씬별 자산이라, 맵 로드 씬(Mainmenu)에서 걸면 씬 전환에 씻겨나간다.
    ///   그래서 fog 가 보여야 할 씬의 카메라가 직접 적용해야 한다. SkyData 는 Init 씬에서 미리 로드되므로 순서 안전.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraClippingApplier : MonoBehaviour
    {
        private void Awake()
        {
            var skyData = DataManager.Instance != null ? DataManager.Instance.SkyData : null;
            if (skyData == null) return;

            var cam = GetComponent<Camera>();
            cam.nearClipPlane = skyData.nearClippingPlane;
            cam.farClipPlane = skyData.farClippingPlane;

            // fog 는 RenderSettings(씬별)에 적용되므로 이 씬에서 직접 건다. skyIndex 는 이미 로드된 값 사용.
            MapLoader.ApplySkyFog(MapLoader.Instance.SkyIndex);
        }
    }
}
