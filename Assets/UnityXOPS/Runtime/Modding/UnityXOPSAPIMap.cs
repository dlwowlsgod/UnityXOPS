using UnityEngine;
using XLua;

namespace UnityXOPS.Modding
{
    public partial class UnityXOPSAPI
    {
        private MapAPI m_map;
        public MapAPI Map => m_map ??= new MapAPI(m_luaEnv);
    }

    /// <summary>
    /// 모드에 현재 맵의 포인트/엔티티 조회를 제공하는 API 그룹. Lua에서는 XOPS.Map 으로 접근한다.
    /// 위치는 모두 Unity 월드 좌표, ry는 Y축 회전(도)이다. 맵이 로드돼 있지 않으면 조회는 nil/0/-1을 돌려준다.
    /// </summary>
    [LuaCallCSharp]
    public class MapAPI
    {
        private readonly LuaEnv m_luaEnv;

        /// <summary>
        /// 맵 API 그룹을 생성한다.
        /// </summary>
        /// <param name="luaEnv">트랜스폼/정보 테이블 생성에 사용할 LuaEnv.</param>
        public MapAPI(LuaEnv luaEnv)
        {
            m_luaEnv = luaEnv;
        }

        /// <summary>
        /// 위치/회전을 {x,y,z,ry} Lua 테이블로 만든다.
        /// </summary>
        /// <param name="position">월드 좌표.</param>
        /// <param name="ry">Y축 회전(도).</param>
        /// <returns>{x,y,z,ry} 테이블.</returns>
        private LuaTable MakeTransform(Vector3 position, float ry)
        {
            LuaTable t = m_luaEnv.NewTable();
            t.Set("x", position.x);
            t.Set("y", position.y);
            t.Set("z", position.z);
            t.Set("ry", ry);
            return t;
        }

        /// <summary>
        /// 카테고리(param0)+식별번호(param3=P4)로 PD1 배치 포인트를 조회한다.
        /// 카테고리: 1 HUMAN, 2 WEAPON, 3 AIPATH, 4 HUMANINFO, 5 SMALLOBJECT, 6 HUMAN2, 7 RAND_WEAPON, 8 RAND_AIPATH, 10~19 EVENT.
        /// </summary>
        /// <param name="category">포인트 카테고리(param0).</param>
        /// <param name="id">식별번호(param3).</param>
        /// <returns>{x,y,z,ry,p0,p1,p2,p3} 테이블. 없으면 nil.</returns>
        public LuaTable GetPoint(int category, int id)
        {
            RawPointData p = MapLoader.GetPoint(category, id);
            if (p == null) return null;

            LuaTable t = MakeTransform(p.position, p.look);
            t.Set("p0", p.param0);
            t.Set("p1", p.param1);
            t.Set("p2", p.param2);
            t.Set("p3", p.param3);
            return t;
        }

        /// <summary>스폰된 Human 수를 반환한다.</summary>
        public int HumanCount() => MapLoader.HumanCount;

        /// <summary>스폰된 Weapon 수를 반환한다.</summary>
        public int WeaponCount() => MapLoader.WeaponCount;

        /// <summary>스폰된 SmallObject 수를 반환한다.</summary>
        public int ObjectCount() => MapLoader.SmallObjectCount;

        /// <summary>플레이어의 Human 인덱스를 반환한다. 없으면 -1.</summary>
        public int PlayerIndex() => MapLoader.PlayerIndex;

        /// <summary>로드된 메시지(.msg) 수를 반환한다.</summary>
        public int MessageCount() => MapLoader.MessageCount;

        /// <summary>
        /// 메시지 ID(.msg 파일의 0-기반 행번호)로 텍스트를 조회한다.
        /// </summary>
        /// <param name="id">메시지 ID(0-기반).</param>
        /// <returns>해당 메시지. 범위 밖이거나 맵 미로드 시 빈 문자열.</returns>
        public string GetMessage(int id) => MapLoader.GetMessageText(id);

        /// <summary>
        /// 스폰 순서 인덱스의 Human 현재 트랜스폼을 조회한다.
        /// </summary>
        /// <param name="index">스폰 순서 인덱스(0-기반).</param>
        /// <returns>{x,y,z,ry} 테이블. 없으면 nil.</returns>
        public LuaTable GetHumanTransform(int index)
        {
            Human h = MapLoader.GetHuman(index);
            return h != null ? MakeTransform(h.transform.position, h.transform.eulerAngles.y) : null;
        }

        /// <summary>
        /// 플레이어 Human의 현재 트랜스폼을 조회한다.
        /// </summary>
        /// <returns>{x,y,z,ry} 테이블. 플레이어가 없으면 nil.</returns>
        public LuaTable GetPlayerTransform()
        {
            return GetHumanTransform(MapLoader.PlayerIndex);
        }

        /// <summary>
        /// 스폰 순서 인덱스의 Weapon 현재 트랜스폼을 조회한다.
        /// </summary>
        /// <param name="index">스폰 순서 인덱스(0-기반).</param>
        /// <returns>{x,y,z,ry} 테이블. 없으면 nil.</returns>
        public LuaTable GetWeaponTransform(int index)
        {
            Weapon w = MapLoader.GetWeapon(index);
            return w != null ? MakeTransform(w.transform.position, w.transform.eulerAngles.y) : null;
        }

        /// <summary>
        /// 스폰 순서 인덱스의 SmallObject 현재 트랜스폼을 조회한다.
        /// </summary>
        /// <param name="index">스폰 순서 인덱스(0-기반).</param>
        /// <returns>{x,y,z,ry} 테이블. 없으면 nil.</returns>
        public LuaTable GetObjectTransform(int index)
        {
            SmallObject o = MapLoader.GetSmallObject(index);
            return o != null ? MakeTransform(o.transform.position, o.transform.eulerAngles.y) : null;
        }

        /// <summary>
        /// 스폰 순서 인덱스 Human의 부가 정보를 조회한다.
        /// </summary>
        /// <param name="index">스폰 순서 인덱스(0-기반).</param>
        /// <returns>{team, hp, alive, identifier} 테이블. 없으면 nil.</returns>
        public LuaTable GetHumanInfo(int index)
        {
            Human h = MapLoader.GetHuman(index);
            if (h == null) return null;

            LuaTable t = m_luaEnv.NewTable();
            t.Set("team", h.Team);
            t.Set("hp", h.HP);
            t.Set("alive", h.Alive);
            t.Set("identifier", h.Identifier);
            return t;
        }
    }
}
