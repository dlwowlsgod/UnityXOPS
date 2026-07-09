using JJLUtility;
using UnityEngine;

namespace UnityXOPS
{
    public partial class ConfigManager : SingletonBehavior<ConfigManager>
    {
        // 지원 해상도. 정수로 직렬화되며, Graphic 섹션의 "resolution" 설정에 인덱스로 저장된다.
        // 4:3 = 0~4, 16:9 = 10~15. 매핑되지 않는 값은 640x480으로 폴백한다.
        private static readonly (int index, int width, int height)[] k_resolutions =
        {
            (0, 640, 480), (1, 800, 600), (2, 1024, 768), (3, 1280, 960), (4, 1600, 1200),
            (10, 960, 540), (11, 1280, 720), (12, 1600, 900), (13, 1920, 1080), (14, 2560, 1440), (15, 3840, 2160),
        };

        private const int k_fallbackWidth = 640;
        private const int k_fallbackHeight = 480;

        /// <summary>
        /// Graphic 섹션의 fullscreen/resolution 설정을 화면에 적용한다. 부팅 시 1회 호출되며 옵션 UI가 재호출할 수 있다.
        /// 전체화면은 보더리스 윈도우(창은 디스플레이 네이티브, 지정 해상도는 렌더 해상도), 창모드는 지정 해상도를 창 크기로 쓴다.
        /// 유저 모니터 네이티브보다 큰 해상도는 지원하지 않으므로 640x480으로 폴백한다.
        /// </summary>
        public void ApplyGraphic()
        {
            bool fullscreen = GetBool(SectionGraphic, KeyFullscreen);
            int resolutionIndex = GetInt(SectionGraphic, KeyResolution);
            (int width, int height) = ResolveResolution(resolutionIndex);

            FullScreenMode mode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            Screen.SetResolution(width, height, mode);
        }

        /// <summary>
        /// 해상도 인덱스를 실제 픽셀 크기로 변환한다. 매핑 실패나 모니터 네이티브 초과 시 640x480으로 폴백한다.
        /// </summary>
        /// <param name="index">해상도 인덱스(0~4, 10~15).</param>
        /// <returns>적용할 (width, height).</returns>
        private static (int width, int height) ResolveResolution(int index)
        {
            foreach ((int i, int w, int h) in k_resolutions)
            {
                if (i != index)
                {
                    continue;
                }

                // 유저 모니터가 감당 못 하는 해상도는 차단.
                if (w > Display.main.systemWidth || h > Display.main.systemHeight)
                {
                    return (k_fallbackWidth, k_fallbackHeight);
                }
                return (w, h);
            }
            return (k_fallbackWidth, k_fallbackHeight);
        }
    }
}
