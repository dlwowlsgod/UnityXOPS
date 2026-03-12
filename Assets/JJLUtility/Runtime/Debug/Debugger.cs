using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace JJLUtility
{
    public static partial class Debugger
    {
        [Conditional("UNITY_EDITOR")]
        public static void Log(object message, Object context = null, string label = "[UnityXOPS]")
        {
            Debug.Log($"{label} {message}", context);
        }

        [Conditional("UNITY_EDITOR")]
        public static void LogWarning(object message, Object context = null, string label = "[UnityXOPS]")
        {
            Debug.LogWarning($"{label} {message}", context);
        }

        [Conditional("UNITY_EDITOR")]
        public static void LogError(object message, Object context = null, string label = "[UnityXOPS]")
        {
            Debug.LogError($"{label} {message}", context);
        }
    }
}