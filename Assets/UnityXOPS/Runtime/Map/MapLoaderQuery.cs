using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 맵에 배치된 포인트(PD1 정적 데이터)와 스폰된 엔티티(런타임)를 조회하는 partial.
    /// 게임플레이 코드와 모드 API(XOPS.Map)가 공유한다. 맵 미로드 시 모든 조회는 null/0/-1을 돌려준다.
    /// </summary>
    public partial class MapLoader
    {
        /// <summary>
        /// 카테고리(param0)와 식별번호(param3=원본 P4)로 PD1 배치 포인트의 첫 매치를 조회한다.
        /// 카테고리: 1 HUMAN, 2 WEAPON, 3 AIPATH, 4 HUMANINFO, 5 SMALLOBJECT, 6 HUMAN2, 7 RAND_WEAPON, 8 RAND_AIPATH, 10~19 EVENT.
        /// </summary>
        /// <param name="category">포인트 카테고리(param0).</param>
        /// <param name="id">식별번호(param3).</param>
        /// <returns>첫 매치 RawPointData. 없으면 null.</returns>
        public static RawPointData GetPoint(int category, int id)
        {
            if (!Loaded) return null;
            var sorted = Instance.m_sortedRawPointData;
            if (sorted == null || category < 0 || category >= sorted.Count) return null;
            return sorted[category].TryGetValue(id, out var list) && list.Count > 0 ? list[0] : null;
        }

        /// <summary>스폰된 Human 수(m_humans). 맵 미로드 시 0.</summary>
        public static int HumanCount => Loaded && Instance.m_humans != null ? Instance.m_humans.Count : 0;

        /// <summary>
        /// 스폰 순서 인덱스로 Human을 조회한다.
        /// </summary>
        /// <param name="index">스폰 순서 인덱스(0-기반).</param>
        /// <returns>해당 Human. 범위 밖이거나 맵 미로드 시 null.</returns>
        public static Human GetHuman(int index)
        {
            if (!Loaded || Instance.m_humans == null || index < 0 || index >= Instance.m_humans.Count) return null;
            return Instance.m_humans[index];
        }

        /// <summary>m_humans 내 플레이어 인덱스. 플레이어가 없거나 맵 미로드 시 -1.</summary>
        public static int PlayerIndex
        {
            get
            {
                if (!Loaded || Instance.player == null || Instance.m_humans == null) return -1;
                return Instance.m_humans.IndexOf(Instance.player);
            }
        }

        /// <summary>
        /// 치트(F7 Player 교체)용 — 지정 Human 을 플레이어로 설정한다. Event/Path/스폰 데이터는 건드리지 않고
        /// Instance.player 만 교체 → AIController(매 프레임 MapLoader.Player 제외)와 PlayerController(재취득)가
        /// 다음 프레임에 조작/AI 주체를 자동으로 맞바꾼다. null 이나 맵 미로드 시 무시.
        /// </summary>
        /// <param name="human">새 플레이어로 지정할 Human.</param>
        public static void SetPlayer(Human human)
        {
            if (!Loaded || human == null) return;
            Instance.player = human;
        }

        /// <summary>
        /// 치트(F9 복제) — source Human 을 복제해 지정 위치/방향에 스폰한다. HP(최대)·비주얼·팀은 source 데이터로,
        /// 무기는 source 가 든 종류 + 기본 탄약(현재 HP/탄약은 복사 안 함). AI 는 aiMode(Follow=followTarget 추적 / Guard=제자리 경계).
        /// Event/Path 미연결. 맵 미로드/source null 이면 null.
        /// </summary>
        /// <param name="source">복제 원본 Human.</param>
        /// <param name="position">스폰 월드 위치.</param>
        /// <param name="yawDeg">스폰 방향(Y, deg).</param>
        /// <param name="aiMode">클론 AI 모드(Follow/Guard).</param>
        /// <param name="followTarget">Follow 모드에서 추적할 대상(Guard 면 null).</param>
        /// <returns>스폰된 클론 Human. 실패 시 null.</returns>
        public static Human SpawnHumanClone(Human source, Vector3 position, float yawDeg, Human.CloneAIMode aiMode, Human followTarget)
        {
            if (!Loaded || source == null || Instance.humanPrefab == null) return null;

            var obj = Instantiate(Instance.humanPrefab, Instance.humanRoot);
            obj.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDeg, 0f));
            var human = obj.GetComponent<Human>();

            human.CreateHuman(source.HumanParam, source.HumanDataParam); // HP(최대)/비주얼/팀 + 기본 무기·탄약
            human.CopyHeldWeaponsFrom(source);                           // 현재 든 무기 종류로 교체 (탄약 기본)
            human.SetCloneAI(aiMode, followTarget);                       // brain 생성 시 hold AI 적용 (m_humans 추가 전에 세팅)
            Instance.m_humans.Add(human);
            return human;
        }

        /// <summary>스폰된 Weapon 수(weaponRoot 자식 = 스폰 순서). 맵 미로드 시 0.</summary>
        public static int WeaponCount => Loaded && Instance.weaponRoot != null ? Instance.weaponRoot.childCount : 0;

        /// <summary>
        /// 스폰 순서 인덱스로 Weapon을 조회한다.
        /// </summary>
        /// <param name="index">스폰 순서 인덱스(0-기반).</param>
        /// <returns>해당 Weapon. 범위 밖이거나 맵 미로드 시 null.</returns>
        public static Weapon GetWeapon(int index)
        {
            if (!Loaded || Instance.weaponRoot == null || index < 0 || index >= Instance.weaponRoot.childCount) return null;
            return Instance.weaponRoot.GetChild(index).GetComponent<Weapon>();
        }

        /// <summary>스폰된 SmallObject 수(objectRoot 자식 = 스폰 순서). 맵 미로드 시 0.</summary>
        public static int SmallObjectCount => Loaded && Instance.objectRoot != null ? Instance.objectRoot.childCount : 0;

        /// <summary>
        /// 스폰 순서 인덱스로 SmallObject를 조회한다.
        /// </summary>
        /// <param name="index">스폰 순서 인덱스(0-기반).</param>
        /// <returns>해당 SmallObject. 범위 밖이거나 맵 미로드 시 null.</returns>
        public static SmallObject GetSmallObject(int index)
        {
            if (!Loaded || Instance.objectRoot == null || index < 0 || index >= Instance.objectRoot.childCount) return null;
            return Instance.objectRoot.GetChild(index).GetComponent<SmallObject>();
        }
    }
}
