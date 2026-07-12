using System.Collections.Generic;
using JJLUtility;
using XLua;

namespace UnityXOPS.Modding
{
    /// <summary>
    /// C#(게임) → Lua 이벤트 디스패처. 게임이 결정적 순간(사망/발사 등)에 Emit하면 등록된 Lua 콜백들이 호출된다.
    /// 게임 로직을 뜯어고치지 않고 "그 순간에 Emit 한 줄"만 심는 관측점 패턴이다.
    /// 콜백(LuaFunction)은 씬 전환 시 Clear로 정리한다(각 씬이 M.start에서 다시 등록).
    /// </summary>
    public static class XOPSEventBus
    {
        private static readonly Dictionary<string, List<LuaFunction>> s_handlers = new Dictionary<string, List<LuaFunction>>();

        /// <summary>
        /// 이벤트에 Lua 콜백을 등록한다(XOPS.Events:On 경유).
        /// </summary>
        /// <param name="name">이벤트 이름</param>
        /// <param name="callback">이벤트 발생 시 호출할 Lua 함수</param>
        public static void On(string name, LuaFunction callback)
        {
            if (string.IsNullOrEmpty(name) || callback == null)
            {
                return;
            }
            if (!s_handlers.TryGetValue(name, out List<LuaFunction> list))
            {
                list = new List<LuaFunction>();
                s_handlers[name] = list;
            }
            list.Add(callback);
        }

        /// <summary>
        /// 이벤트를 발생시켜 등록된 모든 Lua 콜백을 인자와 함께 호출한다. 등록이 없으면 무동작.
        /// LuaEnv가 죽었으면(앱/Play 종료 중) 무시한다. 한 콜백의 오류는 격리해 게임을 멈추지 않는다.
        /// </summary>
        /// <param name="name">이벤트 이름</param>
        /// <param name="args">콜백에 넘길 인자들</param>
        public static void Emit(string name, params object[] args)
        {
            if (!LuaManager.IsEnvAlive || !s_handlers.TryGetValue(name, out List<LuaFunction> list))
            {
                return;
            }
            for (int i = 0; i < list.Count; i++)
            {
                try
                {
                    list[i].Call(args);
                }
                catch (System.Exception e)
                {
                    Debugger.LogError($"[Event:{name}] 콜백 오류: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 모든 등록을 해제한다. 씬 전환 시(LuaSceneController.OnDestroy) 호출해 이전 씬 콜백이 남지 않게 한다.
        /// env가 살아있으면 각 LuaFunction을 Dispose하고, 죽었으면 참조만 비운다.
        /// </summary>
        public static void Clear()
        {
            if (LuaManager.IsEnvAlive)
            {
                foreach (List<LuaFunction> list in s_handlers.Values)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        list[i].Dispose();
                    }
                }
            }
            s_handlers.Clear();
        }
    }
}
