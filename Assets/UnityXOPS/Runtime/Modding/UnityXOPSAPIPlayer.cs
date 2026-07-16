using UnityEngine;
using XLua;

namespace UnityXOPS.Modding
{
    public partial class UnityXOPSAPI
    {
        private PlayerAPI m_player;
        public PlayerAPI Player => m_player ??= new PlayerAPI(m_luaEnv);
    }

    /// <summary>
    /// 모드에 로컬 플레이어의 HUD 관련 상태(체력/탄약/무기/조준/스코프)를 제공하는 API 그룹.
    /// Lua에서는 XOPS.Player 로 접근한다. 플레이어가 없으면(스폰 전/사망 후) 0/빈문자열/false/nil 을 돌려준다.
    /// 스코프 on/off 상태는 엔진(Human)이 소유한다 — 무기교체·재장전·사망 시 자동 해제되므로 조회만 하고 기억하지 않는다.
    /// </summary>
    [LuaCallCSharp]
    public class PlayerAPI
    {
        private readonly LuaEnv m_luaEnv;
        private PlayerController m_controller;

        /// <summary>
        /// 플레이어 API 그룹을 생성한다.
        /// </summary>
        /// <param name="luaEnv">스코프 정보 테이블 생성에 사용할 LuaEnv.</param>
        public PlayerAPI(LuaEnv luaEnv)
        {
            m_luaEnv = luaEnv;
        }

        // 현재 플레이어 Human(스폰/리스폰/사망으로 매 프레임 바뀔 수 있음). 없으면 null.
        private Human Player => MapLoader.Player;

        // 현재 플레이어가 든 무기(현재 슬롯). 플레이어가 없으면 null.
        private Weapon Current
        {
            get
            {
                Human p = Player;
                return p != null ? p.CurrentWeapon : null;
            }
        }

        // 시점 조회용 컨트롤러. 맵 재로드에는 살아남지만 씬 전환 시 파괴되므로, 사라졌으면 다시 찾는다.
        private PlayerController Controller
        {
            get
            {
                if (m_controller == null)
                {
                    m_controller = Object.FindFirstObjectByType<PlayerController>();
                }
                return m_controller;
            }
        }

        /// <summary>플레이어가 현재 존재하는지(스폰됨) 반환한다.</summary>
        public bool Exists() => Player != null;

        /// <summary>플레이어가 살아 있는지 반환한다. 없으면 false.</summary>
        public bool IsAlive()
        {
            Human p = Player;
            return p != null && p.Alive;
        }

        /// <summary>플레이어 체력(HP)을 반환한다. 없으면 0.</summary>
        public float GetHP()
        {
            Human p = Player;
            return p != null ? p.HP : 0f;
        }

        /// <summary>현재 무기의 장전된 탄약(탄창) 수를 반환한다. 없으면 0.</summary>
        public int GetMagazine()
        {
            Weapon w = Current;
            return w != null ? w.CurrentMagazine : 0;
        }

        /// <summary>현재 무기의 예비 탄약 수를 반환한다. 없으면 0.</summary>
        public int GetReserveAmmo()
        {
            Weapon w = Current;
            return w != null ? w.ReserveAmmo : 0;
        }

        /// <summary>현재 무기의 표시 이름을 반환한다. 없으면 빈 문자열.</summary>
        public string GetWeaponName()
        {
            Weapon w = Current;
            return (w != null && w.WeaponData != null) ? w.WeaponData.name : "";
        }

        /// <summary>플레이어가 재장전 중인지 반환한다. 없으면 false.</summary>
        public bool IsReloading()
        {
            Human p = Player;
            return p != null && p.IsReloading;
        }

        /// <summary>플레이어가 무기 교체 중인지 반환한다. 없으면 false.</summary>
        public bool IsSwitchingWeapon()
        {
            Human p = Player;
            return p != null && p.IsSwitchingWeapon;
        }

        /// <summary>
        /// 플레이어가 맞았는지 확인하고 그 표시를 지운다(한 번 확인하면 사라진다).
        /// 체력이 닳지 않는 피격(방어 등)도 true 다. 피격 연출은 이 값을 소비하는 쪽이 유일해야 하므로
        /// 한 프레임에 한 곳에서만 호출한다.
        /// </summary>
        /// <returns>지난 확인 이후 맞았으면 true. 플레이어가 없으면 false.</returns>
        public bool ConsumeHit()
        {
            Human p = Player;
            return p != null && p.ConsumeHit(out _);
        }

        /// <summary>현재 시점이 1인칭인지 반환한다. 컨트롤러가 없으면 false(3인칭 취급).</summary>
        public bool IsFirstPerson()
        {
            PlayerController c = Controller;
            return c != null && c.FirstPerson;
        }

        /// <summary>
        /// 1인칭 ↔ 3인칭 시점을 전환한다. 죽었거나 플레이어가 없으면 무시된다(사망 카메라를 그대로 둔다).
        /// </summary>
        public void ToggleViewMode()
        {
            Human p = Player;
            PlayerController c = Controller;
            if (p == null || !p.Alive || c == null)
            {
                return;
            }

            c.ToggleViewMode();
        }

        /// <summary>
        /// 현재 무기가 크로스헤어를 쓰는 무기인지 반환한다(죽었거나 무기가 없으면 false).
        /// 스코프로 인한 숨김은 별개다 — GetActiveScope().hideCrosshair 로 판단한다.
        /// </summary>
        public bool ShowsCrosshair()
        {
            Human p = Player;
            if (p == null || !p.Alive) return false;

            Weapon w = p.CurrentWeapon;
            return w != null && w.WeaponData != null && w.WeaponData.crosshair;
        }

        /// <summary>
        /// 현재 조준 오차를 반환한다. 이동/점프/저체력 상태분 + 발사 반동 누적분의 합이며,
        /// 동적 크로스헤어 간격에 픽셀 1:1로 대응한다. 플레이어가 없으면 0.
        /// </summary>
        public float GetErrorRange()
        {
            Human p = Player;
            return p != null ? p.GunsightErrorRange : 0f;
        }

        /// <summary>플레이어가 스코프를 보고 있는지 반환한다. 없으면 false.</summary>
        public bool IsScoping()
        {
            Human p = Player;
            return p != null && p.IsScoping;
        }

        /// <summary>
        /// 스코프를 켜고 끈다(토글). 스코프가 없는 무기이거나 교체/재장전 중이거나 죽었으면 무시된다.
        /// </summary>
        public void ToggleScope()
        {
            Human p = Player;
            if (p != null) p.ToggleScope();
        }

        /// <summary>
        /// 현재 무기가 쓰는 스코프 종류 번호를 반환한다. 스코프 정보를 다시 읽어야 하는지 판단하는 데 쓴다.
        /// </summary>
        /// <returns>스코프 종류 번호. 무기/플레이어가 없으면 -1.</returns>
        public int GetScopeIndex()
        {
            Weapon w = Current;
            return (w != null && w.WeaponData != null) ? w.WeaponData.scopeIndex : -1;
        }

        /// <summary>
        /// 지금 보고 있는 스코프의 표시 정보를 Lua 테이블로 반환한다. 스코프를 안 보고 있으면 nil.
        /// aspect 는 스코프 그림의 가로세로 비(예: 1.333=4:3) — 화면비와 무관하게 이 비율로 그리라는 뜻이다.
        /// lines 는 조준선 목록이며, 좌표는 화면 중앙이 원점이고 화면 높이가 480인 기준이다
        /// (가로도 같은 배율 — 화면비나 스코프 그림 크기와 무관한 고정 좌표).
        /// </summary>
        /// <returns>{ fov, aspect, texturePath, hideCrosshair, lines={{x1,y1,x2,y2,r,g,b,a,width},...} }. 스코프 미사용 시 null.</returns>
        public LuaTable GetActiveScope()
        {
            Human p = Player;
            ScopeData s = p != null ? p.ActiveScope : null;
            if (s == null) return null;

            LuaTable t = m_luaEnv.NewTable();
            t.Set("fov", s.fovDegrees);
            t.Set("aspect", s.textureAspect);
            t.Set("texturePath", s.texturePath ?? "");
            t.Set("hideCrosshair", s.hideCrosshair);

            LuaTable lines = m_luaEnv.NewTable();
            if (s.lines != null)
            {
                for (int i = 0; i < s.lines.Count; i++)
                {
                    ScopeLine line = s.lines[i];
                    LuaTable e = m_luaEnv.NewTable();
                    e.Set("x1", line.start.x);
                    e.Set("y1", line.start.y);
                    e.Set("x2", line.end.x);
                    e.Set("y2", line.end.y);
                    e.Set("r", line.color.r);
                    e.Set("g", line.color.g);
                    e.Set("b", line.color.b);
                    e.Set("a", line.color.a);
                    e.Set("width", line.width);
                    lines.Set(i + 1, e);
                }
            }
            t.Set("lines", lines);
            return t;
        }
    }
}
