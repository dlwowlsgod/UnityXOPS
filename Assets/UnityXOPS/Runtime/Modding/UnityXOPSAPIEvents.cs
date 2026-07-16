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

        /// <summary>
        /// 지금 표시할 이벤트 메시지의 번호를 반환한다. 메시지 내용이 바뀌었는지 판단하는 데 쓴다(번호가 같으면 같은 메시지).
        /// </summary>
        /// <returns>메시지 번호. 표시할 메시지가 없으면 -1.</returns>
        public int GetMessageId()
        {
            return EventManager.Loaded ? EventManager.Instance.CurrentMessageId : -1;
        }

        /// <summary>
        /// 지금 표시할 이벤트 메시지의 내용을 반환한다.
        /// </summary>
        /// <returns>메시지 문자열. 표시할 메시지가 없으면 빈 문자열.</returns>
        public string GetMessageText()
        {
            return EventManager.Loaded ? EventManager.Instance.CurrentMessageText : "";
        }

        /// <summary>
        /// 지금 표시할 이벤트 메시지의 진하기를 반환한다. 나타남·유지·사라짐이 이 값에 반영돼 있으므로
        /// 그대로 텍스트 알파에 넣으면 된다.
        /// </summary>
        /// <returns>진하기(0~1). 표시할 메시지가 없으면 0.</returns>
        public float GetMessageAlpha()
        {
            return EventManager.Loaded ? EventManager.Instance.CurrentMessageAlpha : 0f;
        }

        /// <summary>
        /// 미션이 시작된 횟수를 반환한다. 재시작할 때마다 1씩 는다 — 값이 바뀌었으면 미션이 처음부터
        /// 다시 시작된 것이므로, 진행 중에 쌓아둔 연출 상태를 이때 초기화하면 된다.
        /// </summary>
        /// <returns>미션 시작 누적 횟수. 이벤트 매니저가 없으면 0.</returns>
        public int GetMissionStartCount()
        {
            return EventManager.Loaded ? EventManager.Instance.StartCount : 0;
        }

        /// <summary>
        /// 현재 미션의 판정 상태를 반환한다. Result 화면이 성공/실패 표시에 읽는다.
        /// </summary>
        /// <returns>"complete"(성공) / "failed"(실패) / "inprogress"(진행 중·미확정). 이벤트 매니저가 없으면 "inprogress".</returns>
        public string GetResult()
        {
            if (!EventManager.Loaded)
            {
                return "inprogress";
            }

            switch (EventManager.Instance.Result)
            {
                case MissionResult.Complete: return "complete";
                case MissionResult.Failed: return "failed";
                default: return "inprogress";
            }
        }
    }
}
