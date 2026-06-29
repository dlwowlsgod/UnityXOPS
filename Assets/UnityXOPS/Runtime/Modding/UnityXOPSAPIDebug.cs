using UnityEngine;
using JJLUtility;
using XLua;

namespace UnityXOPS.Modding
{
    public partial class UnityXOPSAPI
    {
        private DebugAPI m_debug;
        public DebugAPI Debug => m_debug ??= new DebugAPI();
    }

    /// <summary>
    /// 모드에 로그 출력을 제공하는 API 그룹. Lua에서는 XOPS.Debug 로 접근한다.
    /// </summary>
    [LuaCallCSharp]
    public class DebugAPI
    {
        /// <summary>
        /// 모드에서 일반 로그를 출력한다.
        /// </summary>
        /// <param name="message">출력할 메시지</param>
        /// <param name="context">로그를 강조 표시할 대상 Unity 오브젝트 (선택)</param>
        /// <param name="label">로그 태그. 기본값은 "MOD"</param>
        public void Log(object message, Object context = null, string label = "MOD")
        {
            Debugger.Log(message, context, label);
        }

        /// <summary>
        /// 모드에서 경고 로그를 출력한다.
        /// </summary>
        /// <param name="message">출력할 메시지</param>
        /// <param name="context">로그를 강조 표시할 대상 Unity 오브젝트 (선택)</param>
        /// <param name="label">로그 태그. 기본값은 "MOD"</param>
        public void LogWarning(object message, Object context = null, string label = "MOD")
        {
            Debugger.LogWarning(message, context, label);
        }

        /// <summary>
        /// 모드에서 에러 로그를 출력한다.
        /// </summary>
        /// <param name="message">출력할 메시지</param>
        /// <param name="context">로그를 강조 표시할 대상 Unity 오브젝트 (선택)</param>
        /// <param name="label">로그 태그. 기본값은 "MOD"</param>
        public void LogError(object message, Object context = null, string label = "MOD")
        {
            Debugger.LogError(message, context, label);
        }
    }
}
