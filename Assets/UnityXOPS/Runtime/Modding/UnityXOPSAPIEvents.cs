using XLua;

namespace UnityXOPS.Modding
{
    public partial class UnityXOPSAPI
    {
        private EventsAPI m_events;
        public EventsAPI Events => m_events ??= new EventsAPI();
    }

    /// <summary>
    /// 모드가 게임 이벤트에 콜백을 등록하는 API 그룹. Lua에서는 XOPS.Events 로 접근한다.
    /// 등록은 씬 진입(M.start)마다 다시 하며, 씬 전환 시 자동 해제된다(이전 씬 콜백 잔존 방지).
    /// </summary>
    [LuaCallCSharp]
    public class EventsAPI
    {
        /// <summary>
        /// 이벤트에 콜백을 등록한다. 예: XOPS.Events:On("humanDied", function(id, team, x, y, z) ... end)
        /// </summary>
        /// <param name="name">이벤트 이름</param>
        /// <param name="callback">이벤트 발생 시 인자와 함께 호출할 함수</param>
        public void On(string name, LuaFunction callback)
        {
            XOPSEventBus.On(name, callback);
        }
    }
}
