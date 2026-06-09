using UnityEngine;
using JJLUtility;
using System.IO;

namespace UnityXOPS
{
    /// <summary>
    /// 사용자 설정(config_data.json)을 로드하고 화면 설정(해상도/전체화면)을 적용하는 싱글톤 매니저.
    /// Init 씬에 배치 — 부팅 시 Start 에서 1회 로드+적용한다(원본 config.dat 외부 로딩 대응).
    /// 마우스 감도는 PlayerController 가 매 프레임 MouseSensitivity 를 읽으므로 별도 적용이 필요 없다.
    /// </summary>
    public class ConfigManager : SingletonBehavior<ConfigManager>
    {
        [SerializeField]
        private ConfigData config;
        public ConfigData Config => config;

        // 마우스 감도(0~1, clamp). config 미로드 시 기본값.
        public float MouseSensitivity => config != null ? config.mouseSensitivity : 0.1f;

        private const string k_configPath = "unitydata/config_data.json";

        public void Start()
        {
#if !UNITY_EDITOR
            LoadConfig();
            ApplyScreen();
#endif
        }

        private void LoadConfig()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, k_configPath);
            if (File.Exists(fullPath))
            {
                config = JsonUtility.FromJson<ConfigData>(File.ReadAllText(fullPath));
            }

            if (config == null) config = new ConfigData();
            config.mouseSensitivity = Mathf.Clamp01(config.mouseSensitivity);
        }

        /// <summary>
        /// config 의 해상도/전체화면을 화면에 적용. 옵션 UI 추가 시 외부에서 재호출 가능.
        /// 전체화면 = 보더리스 윈도우(FullScreenWindow): 창은 네이티브, enum 은 렌더 해상도로 적용.
        /// 설정 해상도가 유저 모니터보다 크면 기본값(640x480)으로 폴백한다.
        /// </summary>
        public void ApplyScreen()
        {
            if (config == null) config = new ConfigData();

            if (!TryGetResolutionSize(config.resolution, out int w, out int h))
            {
                // 매핑되지 않는 enum 값 → 기본값
                GetResolutionSize(ScreenResolution.Res640x480, out w, out h);
            }

            // 유저 모니터 네이티브 해상도보다 크면 기본값으로 폴백 (지원 불가 해상도 차단).
            int displayW = Display.main.systemWidth;
            int displayH = Display.main.systemHeight;
            if (w > displayW || h > displayH)
            {
                GetResolutionSize(ScreenResolution.Res640x480, out w, out h);
            }

            FullScreenMode mode = config.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            Screen.SetResolution(w, h, mode);
        }

        // ScreenResolution → (width, height). 매핑되면 true. 미매핑(5~9 등)이면 false.
        private static bool TryGetResolutionSize(ScreenResolution res, out int width, out int height)
        {
            switch (res)
            {
                case ScreenResolution.Res640x480:   width = 640;  height = 480;  return true;
                case ScreenResolution.Res800x600:   width = 800;  height = 600;  return true;
                case ScreenResolution.Res1024x768:  width = 1024; height = 768;  return true;
                case ScreenResolution.Res1280x960:  width = 1280; height = 960;  return true;
                case ScreenResolution.Res1600x1200: width = 1600; height = 1200; return true;

                case ScreenResolution.Res960x540:   width = 960;  height = 540;  return true;
                case ScreenResolution.Res1280x720:  width = 1280; height = 720;  return true;
                case ScreenResolution.Res1600x900:  width = 1600; height = 900;  return true;
                case ScreenResolution.Res1920x1080: width = 1920; height = 1080; return true;
                case ScreenResolution.Res2560x1440: width = 2560; height = 1440; return true;
                case ScreenResolution.Res3840x2160: width = 3840; height = 2160; return true;

                default: width = 640; height = 480; return false;
            }
        }

        private static void GetResolutionSize(ScreenResolution res, out int width, out int height)
            => TryGetResolutionSize(res, out width, out height);
    }
}
