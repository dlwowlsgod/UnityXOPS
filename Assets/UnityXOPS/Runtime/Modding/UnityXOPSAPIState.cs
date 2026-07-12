using System.Collections.Generic;
using XLua;

namespace UnityXOPS.Modding
{
    public partial class UnityXOPSAPI
    {
        private StateAPI m_state;
        public StateAPI State => m_state ??= new StateAPI();
    }

    /// <summary>
    /// 모드가 씬을 넘어 유지되는 전역 값을 저장/조회하는 API 그룹. Lua에서는 XOPS.State 로 접근한다.
    /// LuaManager(및 이 파사드)가 앱 세션 동안 살아있어(DontDestroyOnLoad) 씬 전환에도 값이 유지된다.
    /// 디스크 저장이 아니라 앱 재시작 시 초기화된다(C# static 필드와 같은 수명).
    /// 씬마다 새로 로드되는 스크립트 모듈의 로컬 변수는 사라지므로, 스크롤 위치·선택 인덱스 같은 UI 상태를 여기에 둔다.
    /// </summary>
    [LuaCallCSharp]
    public class StateAPI
    {
        private readonly Dictionary<string, object> m_values = new Dictionary<string, object>();

        /// <summary>
        /// 키에 값을 저장한다(숫자/불리언/문자열 등). 같은 키면 덮어쓴다.
        /// </summary>
        /// <param name="key">값 키.</param>
        /// <param name="value">저장할 값.</param>
        public void Set(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }
            m_values[key] = value;
        }

        /// <summary>
        /// 키의 값을 반환한다. 저장돼 있지 않으면 fallback을 그대로 돌려준다.
        /// </summary>
        /// <param name="key">값 키.</param>
        /// <param name="fallback">키가 없을 때 반환할 기본값.</param>
        /// <returns>저장된 값, 없으면 fallback.</returns>
        public object Get(string key, object fallback)
        {
            if (!string.IsNullOrEmpty(key) && m_values.TryGetValue(key, out object value))
            {
                return value;
            }
            return fallback;
        }

        /// <summary>
        /// 키가 저장돼 있는지 여부를 반환한다.
        /// </summary>
        /// <param name="key">값 키.</param>
        /// <returns>저장돼 있으면 true.</returns>
        public bool Has(string key)
        {
            return !string.IsNullOrEmpty(key) && m_values.ContainsKey(key);
        }

        /// <summary>
        /// 키를 삭제한다. 없으면 무시한다.
        /// </summary>
        /// <param name="key">값 키.</param>
        public void Remove(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                m_values.Remove(key);
            }
        }
    }
}
