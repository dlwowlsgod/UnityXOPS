using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using JJLUtility;
using UnityEngine;
using UnityXOPS.Modding;

namespace UnityXOPS
{
    /// <summary>
    /// 게임 설정(General/Graphic/Input + 입력 바인딩)을 JSON에서 로드/저장하고 전역 접근을 제공하는 싱글톤.
    /// 스칼라 설정은 타입 태그(int/float/bool/string/range)로 다루며, 모드가 Lua로 섹션/설정을 추가할 수 있다.
    /// 입력 바인딩은 구조가 달라 제네릭 설정에서 제외하고 그대로 보관하며, InputManager가 이 데이터를 받아 빌드한다.
    /// Init 씬의 GameObject에 직접 부착한다.
    /// </summary>
    public partial class ConfigManager : SingletonBehavior<ConfigManager>
    {
        [SerializeField]
        private GameConfig config;
        public GameConfig Config => config;

        // 섹션명 → (설정명 → 설정). 대소문자 무시 조회.
        private readonly Dictionary<string, Dictionary<string, ConfigSetting>> m_lookup =
            new(StringComparer.OrdinalIgnoreCase);

        // 시점 회전 핫패스에서 매 프레임 문자열 파싱을 피하려 감도만 캐시한다. 로드/감도 변경 시에만 갱신.
        private float m_mouseSensitivity = 0.1f;

        /// <summary>
        /// 마우스 감도(0~1). PlayerController가 매 프레임 시점 회전 배율로 읽으므로 캐시 값을 반환한다.
        /// </summary>
        public float MouseSensitivity => m_mouseSensitivity;

        /// <summary>
        /// InputManager가 액션을 빌드할 때 소비하는 기본 입력 바인딩 정의.
        /// </summary>
        public InputActionDefinition[] Bindings => config?.bindings;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }

            LoadConfig();
            BuildLookup();
            RefreshSensitivityCache();
        }

        private void Start()
        {
            // 모드 설정 스키마 등록(config.lua). LuaEnv는 모든 Awake 이후라 살아 있다. 그래픽 적용 전에 실행해 기본값 덮어쓰기를 허용한다.
            LuaManager.Instance.LoadSandboxedFile(k_configModPath, "config");
            RefreshSensitivityCache();   // 모드가 감도 기본값을 바꿨을 수 있으므로 갱신
            ApplyGraphic();
        }

        /// <summary>
        /// 감도 캐시를 현재 설정값으로 다시 계산한다. 로드 후와 감도 변경 후에 호출한다.
        /// </summary>
        private void RefreshSensitivityCache()
        {
            m_mouseSensitivity = GetFloat(SectionInput, KeySensitivity, 0.1f);
        }

        /// <summary>
        /// config.json을 로드한다. 파일이 없으면 코드 기본값으로 새 파일을 생성한 뒤 로드한다.
        /// </summary>
        private void LoadConfig()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, k_configPath);
            if (File.Exists(fullPath))
            {
                // config.json은 유저/모더가 손으로 편집하는 파일이라 문법 오류가 있을 수 있다.
                // 파싱이 실패해도 부팅이 멈추지 않도록 기본값으로 폴백한다(손상 파일은 유저가 고치도록 덮어쓰지 않는다).
                try
                {
                    config = JsonUtility.FromJson<GameConfig>(EncodingHelper.ReadAllText(fullPath));
                }
                catch (Exception e)
                {
                    Debugger.LogWarning($"[Config] config.json 파싱 실패, 기본값으로 대체합니다: {e.Message}");
                    config = null;
                }
            }
            else
            {
                config = BuildDefaultConfig();
                WriteConfigFile(fullPath);
            }

            config ??= BuildDefaultConfig();
            config.sections ??= new List<ConfigSection>();
        }

        /// <summary>
        /// 섹션/설정 조회 사전을 config에서 다시 구성한다. 로드 후와 섹션/설정 추가 후에 호출한다.
        /// </summary>
        private void BuildLookup()
        {
            m_lookup.Clear();
            foreach (ConfigSection section in config.sections)
            {
                if (section == null || string.IsNullOrEmpty(section.name))
                {
                    continue;
                }

                Dictionary<string, ConfigSetting> map = GetOrCreateSectionMap(section.name);
                section.settings ??= new List<ConfigSetting>();
                foreach (ConfigSetting setting in section.settings)
                {
                    if (setting != null && !string.IsNullOrEmpty(setting.name))
                    {
                        map[setting.name] = setting;
                    }
                }
            }
        }

        private Dictionary<string, ConfigSetting> GetOrCreateSectionMap(string section)
        {
            if (!m_lookup.TryGetValue(section, out Dictionary<string, ConfigSetting> map))
            {
                map = new Dictionary<string, ConfigSetting>(StringComparer.OrdinalIgnoreCase);
                m_lookup[section] = map;
            }
            return map;
        }

        /// <summary>
        /// 섹션+이름으로 설정을 찾는다.
        /// </summary>
        /// <param name="section">섹션 이름.</param>
        /// <param name="name">설정 이름.</param>
        /// <returns>설정 객체, 없으면 null.</returns>
        public ConfigSetting FindSetting(string section, string name)
        {
            if (section != null && name != null
                && m_lookup.TryGetValue(section, out Dictionary<string, ConfigSetting> map)
                && map.TryGetValue(name, out ConfigSetting setting))
            {
                return setting;
            }
            return null;
        }

        /// <summary>
        /// 섹션이 없으면 추가한다. 이미 있으면 기존 섹션을 반환한다.
        /// </summary>
        /// <param name="name">섹션 이름.</param>
        /// <returns>추가되거나 기존인 섹션.</returns>
        public ConfigSection AddSection(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            foreach (ConfigSection existing in config.sections)
            {
                if (string.Equals(existing.name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return existing;
                }
            }

            ConfigSection section = new ConfigSection { name = name, settings = new List<ConfigSetting>() };
            config.sections.Add(section);
            GetOrCreateSectionMap(name);
            return section;
        }

        /// <summary>
        /// 설정을 스키마+기본값으로 등록한다. 이미 로드된 값이 있으면 값은 유지하고 타입/범위 메타만 갱신한다(영속 값 우선).
        /// 없으면 기본값으로 새로 추가한다. 모드 config.lua가 자기 설정을 정의할 때 쓴다.
        /// </summary>
        /// <param name="section">섹션 이름(없으면 생성).</param>
        /// <param name="name">설정 이름.</param>
        /// <param name="type">타입 태그(int/float/bool/string/intRange/floatRange).</param>
        /// <param name="defaultValue">문자열 기본값.</param>
        /// <param name="min">범위 최소(범위 타입에서만 의미).</param>
        /// <param name="max">범위 최대(범위 타입에서만 의미).</param>
        /// <returns>등록되거나 기존인 설정.</returns>
        public ConfigSetting RegisterSetting(string section, string name, string type, string defaultValue, float min, float max)
        {
            if (string.IsNullOrEmpty(section) || string.IsNullOrEmpty(name))
            {
                return null;
            }

            ConfigSection sectionObj = AddSection(section);
            ConfigSetting setting = FindSetting(section, name);
            if (setting == null)
            {
                setting = new ConfigSetting
                {
                    name = name,
                    type = type,
                    value = defaultValue ?? string.Empty,
                    min = min,
                    max = max,
                };
                sectionObj.settings.Add(setting);
                GetOrCreateSectionMap(section)[name] = setting;
            }
            else
            {
                // 유저가 편집한 값은 유지하고 스키마 메타(타입/범위)만 최신 정의로 갱신한다.
                setting.type = type;
                setting.min = min;
                setting.max = max;
            }
            return setting;
        }

        /// <summary>
        /// 등록된 모든 섹션 이름을 반환한다.
        /// </summary>
        /// <returns>섹션 이름 목록.</returns>
        public IEnumerable<string> GetSectionNames()
        {
            foreach (ConfigSection section in config.sections)
            {
                yield return section.name;
            }
        }

        /// <summary>
        /// 지정 섹션의 설정 이름들을 반환한다.
        /// </summary>
        /// <param name="section">섹션 이름.</param>
        /// <returns>설정 이름 목록. 섹션이 없으면 빈 목록.</returns>
        public IEnumerable<string> GetSettingNames(string section)
        {
            foreach (ConfigSection sec in config.sections)
            {
                if (!string.Equals(sec.name, section, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                foreach (ConfigSetting setting in sec.settings)
                {
                    yield return setting.name;
                }
                yield break;
            }
        }

        /// <summary>
        /// 설정을 정수로 읽는다. min이 max보다 작으면 min/max로 클램프한다.
        /// </summary>
        /// <param name="section">섹션 이름.</param>
        /// <param name="name">설정 이름.</param>
        /// <param name="fallback">설정이 없거나 파싱 실패 시 반환값.</param>
        /// <returns>정수 값.</returns>
        public int GetInt(string section, string name, int fallback = 0)
        {
            ConfigSetting setting = FindSetting(section, name);
            if (setting == null || !TryParseInt(setting.value, out int result))
            {
                return fallback;
            }
            if (HasRange(setting))
            {
                result = Mathf.Clamp(result, Mathf.RoundToInt(setting.min), Mathf.RoundToInt(setting.max));
            }
            return result;
        }

        /// <summary>
        /// 설정을 실수로 읽는다. min이 max보다 작으면 min/max로 클램프한다.
        /// </summary>
        /// <param name="section">섹션 이름.</param>
        /// <param name="name">설정 이름.</param>
        /// <param name="fallback">설정이 없거나 파싱 실패 시 반환값.</param>
        /// <returns>실수 값.</returns>
        public float GetFloat(string section, string name, float fallback = 0f)
        {
            ConfigSetting setting = FindSetting(section, name);
            if (setting == null || !TryParseFloat(setting.value, out float result))
            {
                return fallback;
            }
            if (HasRange(setting))
            {
                result = Mathf.Clamp(result, setting.min, setting.max);
            }
            return result;
        }

        /// <summary>
        /// 설정을 불리언으로 읽는다("true"/"1"이면 참).
        /// </summary>
        /// <param name="section">섹션 이름.</param>
        /// <param name="name">설정 이름.</param>
        /// <param name="fallback">설정이 없을 때 반환값.</param>
        /// <returns>불리언 값.</returns>
        public bool GetBool(string section, string name, bool fallback = false)
        {
            ConfigSetting setting = FindSetting(section, name);
            if (setting == null || string.IsNullOrEmpty(setting.value))
            {
                return fallback;
            }
            string v = setting.value.Trim();
            return v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1";
        }

        /// <summary>
        /// 설정을 문자열로 읽는다.
        /// </summary>
        /// <param name="section">섹션 이름.</param>
        /// <param name="name">설정 이름.</param>
        /// <param name="fallback">설정이 없을 때 반환값.</param>
        /// <returns>문자열 값.</returns>
        public string GetString(string section, string name, string fallback = "")
        {
            ConfigSetting setting = FindSetting(section, name);
            return setting != null ? setting.value ?? string.Empty : fallback;
        }

        /// <summary>
        /// 정수 값을 설정한다. min이 max보다 작으면 클램프한다. 설정이 없으면 무시된다(Save로 파일에 반영).
        /// </summary>
        /// <param name="section">섹션 이름.</param>
        /// <param name="name">설정 이름.</param>
        /// <param name="value">저장할 값.</param>
        public void SetInt(string section, string name, int value)
        {
            ConfigSetting setting = FindSetting(section, name);
            if (setting == null)
            {
                return;
            }
            if (HasRange(setting))
            {
                value = Mathf.Clamp(value, Mathf.RoundToInt(setting.min), Mathf.RoundToInt(setting.max));
            }
            setting.value = value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 실수 값을 설정한다. min이 max보다 작으면 클램프한다.
        /// </summary>
        /// <param name="section">섹션 이름.</param>
        /// <param name="name">설정 이름.</param>
        /// <param name="value">저장할 값.</param>
        public void SetFloat(string section, string name, float value)
        {
            ConfigSetting setting = FindSetting(section, name);
            if (setting == null)
            {
                return;
            }
            if (HasRange(setting))
            {
                value = Mathf.Clamp(value, setting.min, setting.max);
            }
            setting.value = value.ToString(CultureInfo.InvariantCulture);
            RefreshSensitivityCache();
        }

        /// <summary>
        /// 불리언 값을 설정한다("true"/"false"로 저장).
        /// </summary>
        /// <param name="section">섹션 이름.</param>
        /// <param name="name">설정 이름.</param>
        /// <param name="value">저장할 값.</param>
        public void SetBool(string section, string name, bool value)
        {
            ConfigSetting setting = FindSetting(section, name);
            if (setting != null)
            {
                setting.value = value ? "true" : "false";
            }
        }

        /// <summary>
        /// 문자열 값을 설정한다.
        /// </summary>
        /// <param name="section">섹션 이름.</param>
        /// <param name="name">설정 이름.</param>
        /// <param name="value">저장할 값.</param>
        public void SetString(string section, string name, string value)
        {
            ConfigSetting setting = FindSetting(section, name);
            if (setting != null)
            {
                setting.value = value ?? string.Empty;
            }
        }

        /// <summary>
        /// 현재 설정을 config.json에 기록한다. 옵션 UI가 변경을 확정할 때 호출한다.
        /// </summary>
        public void Save()
        {
            WriteConfigFile(Path.Combine(Application.streamingAssetsPath, k_configPath));
        }

        private void WriteConfigFile(string fullPath)
        {
            try
            {
                string directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(fullPath, JsonUtility.ToJson(config, true));
            }
            catch (Exception e)
            {
                Debugger.LogWarning($"[Config] config.json 저장 실패: {e.Message}");
            }
        }

        // 범위 제한은 별도 타입이 아니라 min &lt; max 여부로 결정한다. min &gt;= max 면 클램프하지 않는다(무제한).
        private static bool HasRange(ConfigSetting setting)
        {
            return setting.min < setting.max;
        }

        private static bool TryParseInt(string text, out int result)
        {
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            {
                return true;
            }
            // "1920.0"처럼 실수 문자열로 저장된 경우도 정수로 받아준다.
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float asFloat))
            {
                result = Mathf.RoundToInt(asFloat);
                return true;
            }
            return false;
        }

        private static bool TryParseFloat(string text, out float result)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }
    }
}
