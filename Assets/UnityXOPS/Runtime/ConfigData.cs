using System;

namespace UnityXOPS
{
    /// <summary>
    /// 화면 해상도 enum. 4:3 그룹은 0~4, 16:9 그룹은 10~15. JSON 에는 정수값으로 저장/편집(예: 1920x1080 = 13).
    /// 매핑되지 않는 정수(5~9 등)는 ConfigManager 에서 기본값(640x480)으로 폴백한다.
    /// </summary>
    public enum ScreenResolution
    {
        // 4:3
        Res640x480   = 0,
        Res800x600   = 1,
        Res1024x768  = 2,
        Res1280x960  = 3,
        Res1600x1200 = 4,

        // 16:9
        Res960x540   = 10,
        Res1280x720  = 11,
        Res1600x900  = 12,
        Res1920x1080 = 13,
        Res2560x1440 = 14,
        Res3840x2160 = 15,
    }

    /// <summary>
    /// 외부(StreamingAssets/unitydata/config_data.json)에서 불러오는 사용자 설정 — 마우스 감도/전체화면/해상도.
    /// 원본 OpenXOPS config.dat(MouseSensitivity/FullscreenFlag/ScreenWidth·Height)의 외부 설정 로딩 구조 대응.
    /// 옵션 UI 는 추후 — 현재는 파일 직접 수정으로만 변경한다.
    /// </summary>
    [Serializable]
    public class ConfigData
    {
        // 마우스 감도(0~1). PlayerController 의 시점 회전 배율로 직접 사용. 0.1 ≈ 기존 기본 감도.
        public float mouseSensitivity = 0.1f;

        // 전체화면 여부. true = 보더리스 윈도우(디스플레이 네이티브와 동기), false = 윈도우 모드.
        public bool fullscreen = false;

        // 렌더 해상도. 전체화면이어도 이 값이 렌더 해상도로 적용된다(창은 네이티브).
        public ScreenResolution resolution = ScreenResolution.Res640x480;
    }
}
