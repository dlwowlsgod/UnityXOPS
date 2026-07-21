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
        public const string KeyInvertY = "invertY";
        public const string KeyBrightness = "brightness";
        public const string KeyGamma = "gamma";
        public const string KeyMasterVolume = "MasterVolume";
        public const string KeyUIScale = "UIScale";
        public const string KeyLanguage = "language";

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
        /// 코드 기본값에는 있는데 로드된 config.json에는 없는 항목을 채워 넣는다.
        /// 버전이 올라가며 설정/액션이 새로 생겨도, 이전 버전에서 만들어진 config.json을 쓰는 유저가
        /// 그 항목을 통째로 잃지 않게 하려는 것이다(값이 없으면 옵션 화면에서 조작해도 반영되지 않는다).
        /// 유저가 이미 가진 값은 절대 건드리지 않고, 스키마(타입/범위)만 코드 정의로 맞춘다.
        /// 조회 사전이 있어야 기존 항목을 알아보므로 반드시 BuildLookup 뒤에 호출한다.
        /// </summary>
        private void MergeDefaults()
        {
            foreach (ConfigSection section in BuildDefaultSections())
            {
                foreach (ConfigSetting setting in section.settings)
                {
                    RegisterSetting(section.name, setting.name, setting.type, setting.value, setting.min, setting.max);
                }
            }

            MergeDefaultBindings();
        }

        /// <summary>
        /// 코드 기본 바인딩 중 config.json에 없는 액션만 뒤에 덧붙인다.
        /// 이미 있는 액션은 유저가 바꾼 키를 그대로 둔다.
        /// </summary>
        private void MergeDefaultBindings()
        {
            List<InputActionDefinition> merged = config.bindings != null
                ? new List<InputActionDefinition>(config.bindings)
                : new List<InputActionDefinition>();

            foreach (InputActionDefinition def in BuildDefaultBindings())
            {
                bool exists = false;
                foreach (InputActionDefinition existing in merged)
                {
                    if (existing != null && existing.name == def.name)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    merged.Add(def);
                }
            }

            config.bindings = merged.ToArray();
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
                        // UIScale 상한은 해상도별로 다르다(k_resolutions의 maxUIScale). 부팅/해상도 변경 시 ApplyUIScaleLimit가 min/max를 갱신한다.
                        // 여기 값은 기본 해상도(640x480)의 상한 = 1.0 고정.
                        new ConfigSetting { name = KeyUIScale, type = TypeFloat, value = "1", min = 1f, max = 1f },
                        new ConfigSetting { name = "aimLength", type = TypeInt, value = "10", min = 1f, max = 20f },
                        new ConfigSetting { name = "aimThick", type = TypeInt, value = "1", min = 1f, max = 5f },
                        new ConfigSetting { name = "aimGap", type = TypeInt, value = "3", min = 0f, max = 5f },
                        // 크로스헤어 정적/동적. true=고정, false=반동(ErrorRange)으로 벌어짐.
                        new ConfigSetting { name = "StaticAim", type = TypeBool, value = "false", min = 0f, max = 0f },
                        new ConfigSetting { name = "aimColorR", type = TypeFloat, value = "1", min = 0f, max = 1f },
                        new ConfigSetting { name = "aimColorG", type = TypeFloat, value = "0", min = 0f, max = 1f },
                        new ConfigSetting { name = "aimColorB", type = TypeFloat, value = "0", min = 0f, max = 1f },
                        new ConfigSetting { name = "aimColorA", type = TypeFloat, value = "1", min = 0f, max = 1f },
                        new ConfigSetting { name = "playerName", type = TypeString, value = "xopsPlayer", min = 0f, max = 0f },
                        // 폰트 선택용 언어. 0=자동(Application.systemLanguage를 따름) / 1=한국어 / 2=일본어 / 3=영어.
                        new ConfigSetting { name = KeyLanguage, type = TypeInt, value = "0", min = 0f, max = 3f },
                    },
                },
                new ConfigSection
                {
                    name = SectionInput,
                    settings = new List<ConfigSetting>
                    {
                        new ConfigSetting { name = "invertY", type = TypeBool, value = "false", min = 0f, max = 0f },
                        new ConfigSetting { name = KeySensitivity, type = TypeFloat, value = "0.1", min = 0.01f, max = 1f },
                    },
                },
                new ConfigSection
                {
                    name = SectionGraphic,
                    settings = new List<ConfigSetting>
                    {
                        // 밝기 곱(노출). ScreenColorAdjust 셰이더가 색에 곱한다. 1.0=원본, 0.5~1.5.
                        new ConfigSetting { name = KeyBrightness, type = TypeFloat, value = "1", min = 0.5f, max = 1.5f },
                        // 감마(모니터 관례). ScreenColorAdjust가 pow(c, gamma). 1.0=중립, 값이 클수록 어둡고 작을수록 밝다.
                        new ConfigSetting { name = KeyGamma, type = TypeFloat, value = "1", min = 0.5f, max = 2f },
                        new ConfigSetting { name = KeyFullscreen, type = TypeBool, value = "true", min = 0f, max = 0f },
                        // 해상도는 인덱스(4:3=0~5, 16:9=10~15). 기본 640x480=0. min/max는 인덱스 범위.
                        new ConfigSetting { name = KeyResolution, type = TypeInt, value = "0", min = 0f, max = 15f },
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
                        new ConfigSetting { name = KeyMasterVolume, type = TypeFloat, value = "1", min = 0f, max = 1f },
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
                new InputActionDefinition { name = "walk", type = "Button", bindings = new[] { "<Keyboard>/tab" } },
                new InputActionDefinition { name = "drop", type = "Button", bindings = new[] { "<Keyboard>/g" } },
                new InputActionDefinition { name = "fire", type = "Button", bindings = new[] { "<Mouse>/leftButton" } },
                new InputActionDefinition { name = "zoom", type = "Button", bindings = new[] { "<Keyboard>/leftShift" } },
                new InputActionDefinition { name = "previous", type = "Button", bindings = new[] { "<Keyboard>/z" } },
                new InputActionDefinition { name = "next", type = "Button", bindings = new[] { "<Keyboard>/x" } },
                new InputActionDefinition { name = "reload", type = "Button", bindings = new[] { "<Keyboard>/r" } },
                new InputActionDefinition { name = "first", type = "Button", bindings = new[] { "<Keyboard>/1" } },
                new InputActionDefinition { name = "second", type = "Button", bindings = new[] { "<Keyboard>/2" } },
                new InputActionDefinition { name = "interact", type = "Button", bindings = new[] { "<Keyboard>/f" } },
                new InputActionDefinition { name = "escape", type = "Button", bindings = new[] { "<Keyboard>/escape" } },
            };
        }
    }
}
