using XLua;

namespace UnityXOPS.Modding
{
    public partial class UnityXOPSAPI
    {
        private DataAPI m_data;
        public DataAPI Data => m_data ??= new DataAPI(m_luaEnv);
    }

    /// <summary>
    /// 모드에 전역 데이터(GlobalData — 제품/회사/라이선스/버전)와 미션 목록 조회를 제공하는 API 그룹.
    /// Lua에서는 XOPS.Data 로 접근한다. 데이터가 없으면 빈 문자열/빈 테이블/0을 돌려준다.
    /// </summary>
    [LuaCallCSharp]
    public class DataAPI
    {
        private readonly LuaEnv m_luaEnv;

        /// <summary>
        /// 데이터 API 그룹을 생성한다.
        /// </summary>
        /// <param name="luaEnv">배열 결과 테이블 생성에 사용할 LuaEnv.</param>
        public DataAPI(LuaEnv luaEnv)
        {
            m_luaEnv = luaEnv;
        }

        // DataManager가 로드돼 있을 때만 데이터를 돌려준다(미로드 시 자동 생성 방지).
        private GlobalData Global => DataManager.Loaded ? DataManager.Instance.GlobalData : null;
        private MissionData Missions => DataManager.Loaded ? DataManager.Instance.MissionData : null;

        /// <summary>표시용 버전 문자열(major.minor.patch)을 반환한다.</summary>
        public string GetVersion() => Global != null ? Global.Version : "";

        /// <summary>제품명을 반환한다.</summary>
        public string GetProductName() => Global != null ? Global.productName : "";

        /// <summary>회사/제작자명을 반환한다.</summary>
        public string GetCompanyName() => Global != null ? Global.companyName : "";

        /// <summary>라이선스 종류를 반환한다.</summary>
        public string GetLicenseType() => Global != null ? Global.licenseType : "";

        /// <summary>라이선스 표기를 반환한다.</summary>
        public string GetLicenseName() => Global != null ? Global.licenseName : "";

        /// <summary>
        /// 라이선스 본문 줄들을 Lua 배열 테이블(1-기반)로 반환한다.
        /// </summary>
        /// <returns>각 줄 문자열이 담긴 LuaTable. 없으면 빈 테이블.</returns>
        public LuaTable GetLicenseLines()
        {
            LuaTable table = m_luaEnv.NewTable();
            string[] lines = Global != null ? Global.licenseLines : null;
            if (lines != null)
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    table.Set(i + 1, lines[i]);
                }
            }
            return table;
        }

        /// <summary>공식 미션의 개수를 반환한다.</summary>
        /// <returns>공식 미션 수. 데이터가 없으면 0.</returns>
        public int GetOfficialMissionCount()
        {
            MissionData m = Missions;
            return m != null && m.officialMissions != null ? m.officialMissions.Count : 0;
        }

        /// <summary>
        /// 공식 미션의 이름(리스트 표시명)을 반환한다.
        /// </summary>
        /// <param name="index">공식 미션 인덱스(0-기반).</param>
        /// <returns>미션 이름. 범위 밖이거나 데이터가 없으면 빈 문자열.</returns>
        public string GetOfficialMissionName(int index)
        {
            MissionData m = Missions;
            if (m == null || m.officialMissions == null) return "";
            return (index >= 0 && index < m.officialMissions.Count) ? m.officialMissions[index].Name : "";
        }

        /// <summary>
        /// 애드온 미션 페이지 수를 반환한다. 기본 1(페이지 0 = addon 폴더 자동 로드분). 모더가 페이지를 늘리면 증가한다.
        /// </summary>
        /// <returns>애드온 페이지 수. 데이터가 없으면 0.</returns>
        public int GetAddonPageCount()
        {
            MissionData m = Missions;
            return m != null && m.addonMissions != null ? m.addonMissions.Count : 0;
        }

        /// <summary>
        /// 지정 애드온 페이지의 이름(탭 표시명)을 반환한다. addon.json의 addonName에서 오며, 지정되지 않았으면 빈 문자열.
        /// </summary>
        /// <param name="page">애드온 페이지(0-기반).</param>
        /// <returns>페이지 이름. 범위 밖이거나 데이터가 없으면 빈 문자열.</returns>
        public string GetAddonPageName(int page)
        {
            MissionData m = Missions;
            if (m == null || m.addonPageNames == null || page < 0 || page >= m.addonPageNames.Count) return "";
            return m.addonPageNames[page] ?? "";
        }

        /// <summary>
        /// 지정 애드온 페이지의 미션 개수를 반환한다.
        /// </summary>
        /// <param name="page">애드온 페이지(0-기반).</param>
        /// <returns>그 페이지의 미션 수. 범위 밖이거나 데이터가 없으면 0.</returns>
        public int GetAddonMissionCount(int page)
        {
            MissionData m = Missions;
            if (m == null || m.addonMissions == null || page < 0 || page >= m.addonMissions.Count) return 0;
            var list = m.addonMissions[page];
            return list != null ? list.Count : 0;
        }

        /// <summary>
        /// 지정 애드온 페이지·인덱스 미션의 이름(리스트 표시명)을 반환한다.
        /// </summary>
        /// <param name="page">애드온 페이지(0-기반).</param>
        /// <param name="index">그 페이지 내 미션 인덱스(0-기반).</param>
        /// <returns>미션 이름. 범위 밖이거나 데이터가 없으면 빈 문자열.</returns>
        public string GetAddonMissionName(int page, int index)
        {
            MissionData m = Missions;
            if (m == null || m.addonMissions == null || page < 0 || page >= m.addonMissions.Count) return "";
            var list = m.addonMissions[page];
            return (list != null && index >= 0 && index < list.Count) ? list[index].Name : "";
        }
    }
}
