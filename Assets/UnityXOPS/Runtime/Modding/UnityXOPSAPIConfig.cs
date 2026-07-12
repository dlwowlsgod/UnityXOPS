using System.Globalization;
using XLua;

namespace UnityXOPS.Modding
{
    public partial class UnityXOPSAPI
    {
        private ConfigAPI m_config;
        public ConfigAPI Config => m_config ??= new ConfigAPI(m_luaEnv);
    }

    /// <summary>
    /// 모드에 게임 설정 등록/조회/저장을 제공하는 API 그룹. Lua에서는 XOPS.Config 로 접근한다.
    /// 스칼라 설정(int/float/bool/string/range)만 다루며, 구조가 다른 입력 바인딩은 XOPS.Input에서 처리한다.
    /// </summary>
    [LuaCallCSharp]
    public class ConfigAPI
    {
        private readonly LuaEnv m_luaEnv;

        /// <summary>
        /// 설정 API 그룹을 생성한다.
        /// </summary>
        /// <param name="luaEnv">조회 결과 테이블 생성에 사용할 LuaEnv</param>
        public ConfigAPI(LuaEnv luaEnv)
        {
            m_luaEnv = luaEnv;
        }

        /// <summary>
        /// 설정 섹션을 추가한다(이미 있으면 무시). 모드 전용 섹션을 만들 때 쓴다.
        /// </summary>
        /// <param name="section">섹션 이름</param>
        public void AddSection(string section)
        {
            ConfigManager.Instance.AddSection(section);
        }

        /// <summary>
        /// 설정을 스키마+기본값으로 등록한다. 유저가 이미 값을 바꿔뒀으면 그 값이 유지된다.
        /// 타입: "int"/"float"/"bool"/"string". int/float는 min &lt; max 로 넘기면 그 범위로 클램프되고, min &gt;= max(예: 0, 0)면 무제한이다.
        /// </summary>
        /// <param name="section">섹션 이름(없으면 생성)</param>
        /// <param name="name">설정 이름</param>
        /// <param name="type">타입 태그</param>
        /// <param name="defaultValue">기본값(숫자/불리언/문자열 모두 허용)</param>
        /// <param name="min">클램프 최소(min &lt; max 일 때만 적용)</param>
        /// <param name="max">클램프 최대(min &lt; max 일 때만 적용)</param>
        public void AddSetting(string section, string name, string type, object defaultValue, float min, float max)
        {
            ConfigManager.Instance.RegisterSetting(section, name, type, ToStringValue(defaultValue), min, max);
        }

        /// <summary>
        /// 설정을 정수로 읽는다.
        /// </summary>
        /// <param name="section">섹션 이름</param>
        /// <param name="name">설정 이름</param>
        /// <returns>정수 값, 없으면 0</returns>
        public int GetInt(string section, string name)
        {
            return ConfigManager.Instance.GetInt(section, name);
        }

        /// <summary>
        /// 설정을 실수로 읽는다.
        /// </summary>
        /// <param name="section">섹션 이름</param>
        /// <param name="name">설정 이름</param>
        /// <returns>실수 값, 없으면 0</returns>
        public float GetFloat(string section, string name)
        {
            return ConfigManager.Instance.GetFloat(section, name);
        }

        /// <summary>
        /// 설정을 불리언으로 읽는다.
        /// </summary>
        /// <param name="section">섹션 이름</param>
        /// <param name="name">설정 이름</param>
        /// <returns>불리언 값, 없으면 false</returns>
        public bool GetBool(string section, string name)
        {
            return ConfigManager.Instance.GetBool(section, name);
        }

        /// <summary>
        /// 설정을 문자열로 읽는다.
        /// </summary>
        /// <param name="section">섹션 이름</param>
        /// <param name="name">설정 이름</param>
        /// <returns>문자열 값, 없으면 빈 문자열</returns>
        public string GetString(string section, string name)
        {
            return ConfigManager.Instance.GetString(section, name);
        }

        /// <summary>
        /// 정수 값을 설정한다(범위 타입이면 클램프). 파일 반영은 Save로 한다.
        /// </summary>
        /// <param name="section">섹션 이름</param>
        /// <param name="name">설정 이름</param>
        /// <param name="value">저장할 값</param>
        public void SetInt(string section, string name, int value)
        {
            ConfigManager.Instance.SetInt(section, name, value);
        }

        /// <summary>
        /// 실수 값을 설정한다(범위 타입이면 클램프). 파일 반영은 Save로 한다.
        /// </summary>
        /// <param name="section">섹션 이름</param>
        /// <param name="name">설정 이름</param>
        /// <param name="value">저장할 값</param>
        public void SetFloat(string section, string name, float value)
        {
            ConfigManager.Instance.SetFloat(section, name, value);
        }

        /// <summary>
        /// 불리언 값을 설정한다. 파일 반영은 Save로 한다.
        /// </summary>
        /// <param name="section">섹션 이름</param>
        /// <param name="name">설정 이름</param>
        /// <param name="value">저장할 값</param>
        public void SetBool(string section, string name, bool value)
        {
            ConfigManager.Instance.SetBool(section, name, value);
        }

        /// <summary>
        /// 문자열 값을 설정한다. 파일 반영은 Save로 한다.
        /// </summary>
        /// <param name="section">섹션 이름</param>
        /// <param name="name">설정 이름</param>
        /// <param name="value">저장할 값</param>
        public void SetString(string section, string name, string value)
        {
            ConfigManager.Instance.SetString(section, name, value);
        }

        /// <summary>
        /// 설정의 타입 태그를 반환한다. 옵션 UI가 위젯 종류를 고를 때 쓴다.
        /// </summary>
        /// <param name="section">섹션 이름</param>
        /// <param name="name">설정 이름</param>
        /// <returns>타입 문자열, 없으면 빈 문자열</returns>
        public string GetSettingType(string section, string name)
        {
            ConfigSetting setting = ConfigManager.Instance.FindSetting(section, name);
            return setting != null ? setting.type ?? "" : "";
        }

        /// <summary>
        /// 범위 설정의 최소값을 반환한다.
        /// </summary>
        /// <param name="section">섹션 이름</param>
        /// <param name="name">설정 이름</param>
        /// <returns>최소값, 없으면 0</returns>
        public float GetMin(string section, string name)
        {
            ConfigSetting setting = ConfigManager.Instance.FindSetting(section, name);
            return setting != null ? setting.min : 0f;
        }

        /// <summary>
        /// 범위 설정의 최대값을 반환한다.
        /// </summary>
        /// <param name="section">섹션 이름</param>
        /// <param name="name">설정 이름</param>
        /// <returns>최대값, 없으면 0</returns>
        public float GetMax(string section, string name)
        {
            ConfigSetting setting = ConfigManager.Instance.FindSetting(section, name);
            return setting != null ? setting.max : 0f;
        }

        /// <summary>
        /// 모든 섹션 이름을 Lua 배열 테이블(1-기반)로 반환한다.
        /// </summary>
        /// <returns>섹션 이름들이 담긴 LuaTable</returns>
        public LuaTable GetSectionNames()
        {
            return ToLuaArray(ConfigManager.Instance.GetSectionNames());
        }

        /// <summary>
        /// 지정 섹션의 설정 이름들을 Lua 배열 테이블(1-기반)로 반환한다.
        /// </summary>
        /// <param name="section">섹션 이름</param>
        /// <returns>설정 이름들이 담긴 LuaTable</returns>
        public LuaTable GetSettingNames(string section)
        {
            return ToLuaArray(ConfigManager.Instance.GetSettingNames(section));
        }

        /// <summary>
        /// 현재 설정을 config.json에 저장한다. 옵션 UI가 변경을 확정할 때 호출한다.
        /// </summary>
        public void Save()
        {
            ConfigManager.Instance.Save();
        }

        /// <summary>
        /// Graphic 섹션의 fullscreen/resolution 설정을 화면에 적용한다. 옵션에서 그래픽을 바꾸고 확정할 때 호출한다.
        /// </summary>
        public void ApplyGraphic()
        {
            ConfigManager.Instance.ApplyGraphic();
        }

        /// <summary>
        /// 선택 가능한 해상도 옵션 개수. 옵션 UI가 &lt;&lt; &gt;&gt;로 순회할 때 쓴다.
        /// </summary>
        /// <returns>옵션 개수</returns>
        public int GetResolutionOptionCount()
        {
            return ConfigManager.Instance.ResolutionOptionCount;
        }

        /// <summary>
        /// i번째(0-기반) 해상도 옵션의 저장 인덱스 값을 반환한다("Graphic"/"resolution"에 넣을 값).
        /// </summary>
        /// <param name="i">옵션 순번</param>
        /// <returns>저장 인덱스, 범위 밖이면 -1</returns>
        public int GetResolutionOptionIndex(int i)
        {
            return ConfigManager.Instance.ResolutionOptionIndexAt(i);
        }

        /// <summary>
        /// i번째(0-기반) 해상도 옵션의 표시 라벨("640x480")을 반환한다.
        /// </summary>
        /// <param name="i">옵션 순번</param>
        /// <returns>라벨, 범위 밖이면 빈 문자열</returns>
        public string GetResolutionOptionLabel(int i)
        {
            return ConfigManager.Instance.ResolutionOptionLabelAt(i);
        }

        /// <summary>
        /// 저장하지 않은 변경을 마지막 저장 상태로 되돌린다. 옵션 화면 BACK(취소)에 쓴다.
        /// </summary>
        public void RevertToSaved()
        {
            ConfigManager.Instance.RevertToSaved();
        }

        /// <summary>
        /// 설정의 초기값을 등록한다(RESET에서 이 값으로 복구). Lua가 옵션 기본값을 데이터로 등록한다.
        /// </summary>
        /// <param name="section">섹션 이름</param>
        /// <param name="name">설정 이름</param>
        /// <param name="value">초기값(숫자/불리언/문자열 허용)</param>
        public void SetDefault(string section, string name, object value)
        {
            ConfigManager.Instance.SetDefault(section, name, ToStringValue(value));
        }

        /// <summary>
        /// 등록된 초기값으로 설정을 복구한다. 옵션 화면 RESET에 쓴다(저장은 Save로 별도).
        /// </summary>
        public void ResetToDefaults()
        {
            ConfigManager.Instance.ResetToDefaults();
        }

        /// <summary>
        /// Lua에서 넘어온 값(숫자/불리언/문자열)을 저장용 불변 문자열로 변환한다.
        /// </summary>
        /// <param name="value">Lua 값</param>
        /// <returns>저장용 문자열</returns>
        private static string ToStringValue(object value)
        {
            switch (value)
            {
                case null:
                    return string.Empty;
                case bool b:
                    return b ? "true" : "false";
                case string s:
                    return s;
                case double d:
                    return d.ToString(CultureInfo.InvariantCulture);
                case float f:
                    return f.ToString(CultureInfo.InvariantCulture);
                case long l:
                    return l.ToString(CultureInfo.InvariantCulture);
                case int i:
                    return i.ToString(CultureInfo.InvariantCulture);
                default:
                    return System.Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }

        private LuaTable ToLuaArray(System.Collections.Generic.IEnumerable<string> items)
        {
            LuaTable table = m_luaEnv.NewTable();
            int index = 1;
            foreach (string item in items)
            {
                table.Set(index++, item);
            }
            return table;
        }
    }
}
