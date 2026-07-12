using UnityEngine;
using XLua;

namespace UnityXOPS.Modding
{
    /// <summary>
    /// 씬에 부착해 지정 Lua 스크립트를 샌드박스로 로드하고 매 프레임 구동하는 컨트롤러.
    /// 스크립트가 반환한 테이블의 start() / update(elapsed, deltaTime) 함수를 호출한다.
    /// 두 함수 모두 선택이며, 콜론(self) 없이 점 정의(function M.start() ...)로 작성한다.
    /// </summary>
    public class LuaSceneController : MonoBehaviour
    {
        [SerializeField]
        private string scriptPath;

        private LuaTable m_module;
        private LuaFunction m_start;
        private LuaFunction m_update;
        private float m_startTime;

        private void Start()
        {
            m_startTime = Time.time;

            m_module = LuaManager.Instance.LoadSandboxedFile(scriptPath, scriptPath);
            if (m_module == null)
            {
                return;
            }

            m_start = m_module.Get<LuaFunction>("start");
            m_update = m_module.Get<LuaFunction>("update");
            m_start?.Call();
        }

        private void Update()
        {
            m_update?.Call(Time.time - m_startTime, Time.deltaTime);
        }

        private void OnDestroy()
        {
            // 이 씬이 등록한 이벤트 콜백 해제(다음 씬으로 잔존 방지). Clear는 env가 죽었으면 내부에서 dispose를 건너뛴다.
            XOPSEventBus.Clear();

            // 앱/Play 종료 시 LuaManager가 먼저 LuaEnv를 Dispose하면(순서 비결정) 핸들 Dispose가 예외를 던진다.
            // env가 죽으면 그 안의 Lua 객체도 함께 무효화되므로, 살아있을 때만 개별 정리한다.
            if (!LuaManager.IsEnvAlive)
            {
                return;
            }

            m_start?.Dispose();
            m_update?.Dispose();
            m_module?.Dispose();
        }
    }
}
