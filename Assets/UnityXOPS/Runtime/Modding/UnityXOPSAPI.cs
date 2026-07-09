using XLua;

namespace UnityXOPS.Modding
{
    /// <summary>
    /// 모드 Lua 스크립트에 노출되는 API 파사드의 루트.
    /// 기능 그룹별 서브 파사드(Debug / Input / UI / Camera / Scene / Map / Data / State / Events)를 프로퍼티로 노출하며,
    /// Lua는 XOPS.Input:IsPressed(...) 처럼 "그룹.메서드" 형태로 호출한다.
    /// </summary>
    [LuaCallCSharp]
    public partial class UnityXOPSAPI
    {
        private readonly LuaEnv m_luaEnv;

        /// <summary>
        /// 모드 API 파사드를 생성한다.
        /// </summary>
        /// <param name="luaEnv">Lua 테이블 생성 등에 사용할 LuaEnv</param>
        public UnityXOPSAPI(LuaEnv luaEnv)
        {
            m_luaEnv = luaEnv;
        }
    }
}
