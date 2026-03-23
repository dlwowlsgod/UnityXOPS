using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace JJLUtility
{
    /// <summary>
    /// 에디터 전용 로그 출력 유틸리티. 빌드에서는 완전히 제거된다.
    /// </summary>
    public static partial class Debugger
    {
        /// <summary>
        /// 일반 로그 메시지를 콘솔에 출력한다. 에디터 전용.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Log(object message, Object context = null, string label = "[UnityXOPS]")
        {
            Debug.Log($"{label} {message}", context);
        }

        /// <summary>
        /// 경고 로그 메시지를 콘솔에 출력한다. 에디터 전용.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void LogWarning(object message, Object context = null, string label = "[UnityXOPS]")
        {
            Debug.LogWarning($"{label} {message}", context);
        }

        /// <summary>
        /// 에러 로그 메시지를 콘솔에 출력한다. 에디터 전용.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void LogError(object message, Object context = null, string label = "[UnityXOPS]")
        {
            Debug.LogError($"{label} {message}", context);
        }
    }
}