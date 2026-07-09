using System.Collections.Generic;
using JJLUtility;

namespace UnityXOPS
{
    public partial class ConfigManager : SingletonBehavior<ConfigManager>
    {
        private const string k_configPath = "unitydata/config.json";
        private const string k_configModPath = "unitydata/config.lua";

        // 섹션 이름(코어 4종). 모드는 이 외의 섹션을 추가할 수 있다.
        public const string SectionGeneral = "General";
        public const string SectionInput = "Input";
        public const string SectionGraphic = "Graphic";
        public const string SectionSound = "Sound";

        public const string TypeInt = "int";
        public const string TypeFloat = "float";
        public const string TypeBool = "bool";
        public const string TypeString = "string";

        // 코어 설정 이름(그래픽 적용/감도 조회에 쓰는 키).
        public const string KeyFullscreen = "fullscreen";
        public const string KeyResolution = "resolution";
        public const string KeySensitivity = "sensitivity";

        /// <summary>
        /// config.json이 없을 때 기록할 기본 설정을 코드로 구성한다. 코어 섹션 3종과 기본 입력 바인딩을 담는다.
        /// </summary>
        /// <returns>기본값으로 채운 GameConfig.</returns>
        private static GameConfig BuildDefaultConfig()
        {
            return new GameConfig
            {
                sections = BuildDefaultSections(),
                bindings = BuildDefaultBindings(),
            };
        }

        /// <summary>
        /// 코어 섹션(General/Graphic/Input)의 기본 스칼라 설정 목록을 만든다.
        /// </summary>
        /// <returns>기본 섹션 목록.</returns>
        private static List<ConfigSection> BuildDefaultSections()
        {
            return new List<ConfigSection>
            {
                new ConfigSection
                {
                    name = SectionGeneral,
                    settings = new List<ConfigSetting>
                    {
                        new ConfigSetting { name = "ShowFPS", type = TypeBool, value = "false", min = 0f, max = 0f },
                        new ConfigSetting { name = "aimLength", type = TypeInt, value = "10", min = 1f, max = 10f },
                        new ConfigSetting { name = "aimThick", type = TypeInt, value = "1", min = 1f, max = 5f },
                        new ConfigSetting { name = "aimGap", type = TypeInt, value = "3", min = 0f, max = 5f },
                        new ConfigSetting { name = "aimColorR", type = TypeFloat, value = "1", min = 0f, max = 1f },
                        new ConfigSetting { name = "aimColorG", type = TypeFloat, value = "0", min = 0f, max = 1f },
                        new ConfigSetting { name = "aimColorB", type = TypeFloat, value = "0", min = 0f, max = 1f },
                        new ConfigSetting { name = "aimColorA", type = TypeFloat, value = "1", min = 0f, max = 1f },
                        new ConfigSetting { name = "playerName", type = TypeString, value = "xopsPlayer", min = 0f, max = 0f },
                    },
                },
                new ConfigSection
                {
                    name = SectionInput,
                    settings = new List<ConfigSetting>
                    {
                        new ConfigSetting { name = "invertX", type = TypeBool, value = "false", min = 0f, max = 0f },
                        new ConfigSetting { name = KeySensitivity, type = TypeFloat, value = "0.1", min = 0.01f, max = 1f },
                    },
                },
                new ConfigSection
                {
                    name = SectionGraphic,
                    settings = new List<ConfigSetting>
                    {
                        new ConfigSetting { name = "brightness", type = TypeFloat, value = "0.1", min = 0.01f, max = 1f },
                        // 감마 보정 지수. 1.0=보정 없음, 표준 sRGB 2.2가 기본. 높을수록 중간톤이 밝아진다.
                        new ConfigSetting { name = "gamma", type = TypeFloat, value = "2.2", min = 1f, max = 3f },
                        new ConfigSetting { name = KeyFullscreen, type = TypeBool, value = "true", min = 0f, max = 0f },
                        // 해상도는 인덱스(4:3=0~4, 16:9=10~15). 기본 960x540=10. min/max는 인덱스 범위.
                        new ConfigSetting { name = KeyResolution, type = TypeInt, value = "10", min = 0f, max = 15f },
                        new ConfigSetting { name = "vsync", type = TypeBool, value = "false", min = 0f, max = 0f },
                        new ConfigSetting { name = "limitFrame", type = TypeBool, value = "true", min = 0f, max = 0f },
                        new ConfigSetting { name = "frameLimit", type = TypeInt, value = "60", min = 60f, max = 480f },
                        new ConfigSetting { name = "nearClippingPlane", type = TypeFloat, value = "0.1", min = 0.03f, max = 0.1f },
                        new ConfigSetting { name = "farClippingPlane", type = TypeFloat, value = "80", min = 80f, max = 1000f },
                        new ConfigSetting { name = "fov", type = TypeInt, value = "65", min = 60f, max = 90f },
                    },
                },
                new ConfigSection
                {
                    name = SectionSound,
                    settings = new List<ConfigSetting>
                    {
                        new ConfigSetting { name = "MasterVolume", type = TypeFloat, value = "1", min = 0f, max = 1f },
                    },
                },
            };
        }

        /// <summary>
        /// 기본 입력 바인딩 정의를 만든다(원본 XOPS 조작 기준). config.json이 없을 때만 쓰인다.
        /// </summary>
        /// <returns>기본 입력 액션 정의 배열.</returns>
        private static InputActionDefinition[] BuildDefaultBindings()
        {
            return new[]
            {
                new InputActionDefinition
                {
                    name = "look",
                    type = "PassThrough",
                    bindings = new[] { "<Mouse>/delta" },
                    composites = new[]
                    {
                        new InputCompositeDefinition
                        {
                            type = "2DVector",
                            up = "<Keyboard>/upArrow", down = "<Keyboard>/downArrow",
                            left = "<Keyboard>/leftArrow", right = "<Keyboard>/rightArrow",
                        },
                    },
                },
                new InputActionDefinition
                {
                    name = "move",
                    type = "Value",
                    composites = new[]
                    {
                        new InputCompositeDefinition
                        {
                            type = "2DVector",
                            up = "<Keyboard>/w", down = "<Keyboard>/s",
                            left = "<Keyboard>/a", right = "<Keyboard>/d",
                        },
                    },
                },
                new InputActionDefinition { name = "jump", type = "Button", bindings = new[] { "<Keyboard>/space" } },
                new InputActionDefinition { name = "walk", type = "Button", bindings = new[] { "<Keyboard>/leftShift" } },
                new InputActionDefinition { name = "drop", type = "Button", bindings = new[] { "<Keyboard>/g" } },
                new InputActionDefinition { name = "fire", type = "Button", bindings = new[] { "<Mouse>/leftButton" } },
                new InputActionDefinition { name = "zoom", type = "Button", bindings = new[] { "<Mouse>/rightButton" } },
                new InputActionDefinition { name = "previous", type = "Button", bindings = new[] { "<Keyboard>/z" } },
                new InputActionDefinition { name = "next", type = "Button", bindings = new[] { "<Keyboard>/x" } },
                new InputActionDefinition { name = "reload", type = "Button", bindings = new[] { "<Keyboard>/r" } },
                new InputActionDefinition { name = "first", type = "Button", bindings = new[] { "<Keyboard>/1" } },
                new InputActionDefinition { name = "second", type = "Button", bindings = new[] { "<Keyboard>/2" } },
                new InputActionDefinition { name = "interact", type = "Button", bindings = new[] { "<Keyboard>/F" } },
                new InputActionDefinition { name = "escape", type = "Button", bindings = new[] { "<Keyboard>/escape" } },
            };
        }
    }
}
