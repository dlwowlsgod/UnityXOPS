using JJLUtility;
using UnityEngine;
using XLua;
using System.IO;

namespace UnityXOPS.Modding
{
    /// <summary>
    /// 모드 Lua 스크립트의 로딩과 실행을 담당하는 싱글톤.
    /// 단일 LuaEnv를 소유하며 각 스크립트를 격리된 샌드박스 _ENV에서 로드한다.
    /// Init 씬의 GameObject에 직접 부착해 사용한다.
    /// </summary>
    public class LuaManager : SingletonBehavior<LuaManager>
    {
        private LuaEnv m_luaEnv;
        private LuaTable m_safeGlobals;
        private UnityXOPSAPI m_api;
        private float m_gcTimer;

        private static bool s_envAlive;

        private const float GCInterval = 1.0f;

        /// <summary>
        /// LuaEnv가 아직 살아 있는지(Dispose 전인지) 여부. 종료 순서 경합 시 핸들 정리를 건너뛰는 데 쓴다.
        /// </summary>
        public static bool IsEnvAlive => s_envAlive;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }

            m_luaEnv = new LuaEnv();
            BuildSafeGlobals();
            m_api = new UnityXOPSAPI(m_luaEnv);
            s_envAlive = true;
        }

        private void Update()
        {
            if (m_luaEnv == null)
            {
                return;
            }

            m_gcTimer += Time.deltaTime;
            if (m_gcTimer >= GCInterval)
            {
                m_luaEnv.Tick();
                m_gcTimer = 0f;
            }
        }

        private void OnDestroy()
        {
            // 먼저 플래그를 내려, 뒤이어 파괴되는 LuaSceneController가 죽은 env에 Dispose를 시도하지 않게 한다.
            s_envAlive = false;
            m_safeGlobals?.Dispose();
            m_luaEnv?.Dispose();
        }

        /// <summary>
        /// 모드에 허용할 안전한 표준 전역만 골라 공유 베이스 테이블을 구성한다.
        /// os/io/require/load/debug/package/CS 등 위험한 전역은 의도적으로 제외된다.
        /// </summary>
        private void BuildSafeGlobals()
        {
            m_safeGlobals = m_luaEnv.NewTable();

            string[] whitelist =
            {
                "print", "pairs", "ipairs", "next", "select", "type",
                "tostring", "tonumber", "pcall", "xpcall", "error", "assert",
                "setmetatable", "getmetatable", "rawget", "rawset", "rawequal", "rawlen",
                "math", "string", "table",
            };

            LuaTable global = m_luaEnv.Global;
            foreach (string key in whitelist)
            {
                m_safeGlobals.Set(key, global.Get<object>(key));
            }
        }

        /// <summary>
        /// StreamingAssets 하위의 Lua 파일을 읽어 샌드박스 환경에서 로드한다.
        /// </summary>
        /// <param name="relativePath">StreamingAssets 기준 상대 경로</param>
        /// <param name="chunkName">에러 추적용 청크 이름</param>
        /// <returns>스크립트가 return한 LuaTable, 파일이 없거나 반환이 없으면 null</returns>
        public LuaTable LoadSandboxedFile(string relativePath, string chunkName)
        {
            string fullPath = SafePath.Combine(Application.streamingAssetsPath, relativePath);
            if (!File.Exists(fullPath))
            {
                Debugger.LogWarning($"[Lua] 스크립트를 찾을 수 없습니다: {fullPath}");
                return null;
            }

            string source = EncodingHelper.ReadAllText(fullPath);
            return LoadSandboxed(source, chunkName);
        }

        /// <summary>
        /// 주어진 Lua 소스를 격리된 샌드박스 환경에서 로드하고 반환 테이블을 받아온다.
        /// </summary>
        /// <param name="source">Lua 스크립트 소스 문자열</param>
        /// <param name="chunkName">에러 추적용 청크 이름</param>
        /// <returns>스크립트가 return한 LuaTable, 없으면 null</returns>
        public LuaTable LoadSandboxed(string source, string chunkName)
        {
            LuaTable env = m_luaEnv.NewTable();
            using (LuaTable meta = m_luaEnv.NewTable())
            {
                meta.Set("__index", m_safeGlobals);
                env.SetMetaTable(meta);
            }

            env.Set("XOPS", m_api);

            object[] result = m_luaEnv.DoString(source, chunkName, env);

            // 주의: env는 Dispose하지 않는다. 반환 테이블의 함수들이 _ENV로 env를 참조하므로
            // 호출 측이 반환 테이블의 수명을 쥐고 끝날 때 함께 정리한다.
            return result != null && result.Length > 0 ? result[0] as LuaTable : null;
        }
    }
}
