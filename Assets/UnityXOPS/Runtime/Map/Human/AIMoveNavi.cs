using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// AI 경로 포인트 이동 모드. PD1 AIPATH(param0=3) 의 param1(=원본 p2) 값 0~7 에 직접 대응.
    /// 원본 OpenXOPS ai.cpp:2053-2063 MovePathNowState 매핑.
    /// </summary>
    public enum AIPathMode
    {
        Walk = 0,
        Run = 1,
        Wait = 2,
        Tracking = 3, // 특정 Human 추적 — 1차 미구현(제자리 대기로 폴백)
        WaitAlert = 4, // 경계 대기 — 대기 중 Wait 과 동일, 경계(Caution+) 진입 시 다음 경로로 진행 (NormalMain 처리)
        Stop5Sec = 5,
        Grenade = 6, // 지점 수류탄 투척 — 1차 미구현(즉시 다음으로)
        Run2 = 7, // 전투 스트레이핑 달리기 — 1차 Run 으로 폴백
        Random, // RAND_AIPATH(param0=8) 분기 노드 (위치 없음, 통과 전용)
        None, // 경로 없음/종료
    }

    /// <summary>
    /// Human 1명의 경로 추종 상태 (원본 OpenXOPS AIMoveNavi, ai.cpp). 현재 웨이포인트를 들고 다음으로 진행한다.
    /// 포인트 연결은 식별번호(param3=p4) 기반 — param2(=p3)가 가리키는 다음 포인트를 MapLoader 에서 조회.
    /// RAND_AIPATH(8) 분기 노드는 위치가 없으므로 즉시 50:50 선택해 다음 AIPATH(3)까지 통과한다.
    /// 순환/종료/분기는 전부 데이터로 결정 (마지막 param2 가 첫 포인트 param3 을 가리키면 루프).
    /// </summary>
    public class AIMoveNavi
    {
        private RawPointData m_current; // 현재 목표 웨이포인트 (항상 AIPATH type 3). null 이면 경로 없음.
        private Human m_target; // Tracking 모드 추적 대상. 원본처럼 한 번 잡으면 유지(죽어도 시체 추적).

        // 치트(F9 복제) hold 오버라이드 — 패스 순회를 무시하고 고정 목표(추적 대상/대기 위치)만 사용. 원본 AIMoveNavi hold 플래그.
        private bool m_hold;
        private AIPathMode m_holdMode; // Tracking(따라오기) 또는 Wait(제자리 경계)
        private Vector3 m_holdPos;     // Wait 고정 위치
        private float m_holdLook;      // Wait 선호 방향(deg)

        public bool Valid => m_hold
            ? (m_holdMode != AIPathMode.Tracking || m_target != null)
            : m_current != null;

        // 이동 모드는 캐시하지 않고 현재 웨이포인트의 param1 에서 라이브로 읽는다 (원본 MovePathNowState 와 동일).
        // 이벤트 ChangeToWalk(14) 가 param1 을 Walk(0) 로 바꾸면 Wait(2) 로 앉아있던 Human 도 즉시 풀려 다음 포인트로 진행.
        public AIPathMode Mode => m_hold
            ? m_holdMode
            : (m_current != null ? ModeOf(m_current.param1) : AIPathMode.None);
        public float TargetLook => m_hold ? m_holdLook : (m_current != null ? m_current.look : 0f);

        // Tracking 이면 대상 위치(라이브), Wait hold 면 고정 위치, 아니면 웨이포인트 위치.
        public Vector3 TargetPos
        {
            get
            {
                if (m_hold)
                {
                    if (m_holdMode == AIPathMode.Tracking && m_target != null) return m_target.transform.position;
                    return m_holdPos;
                }
                if (Mode == AIPathMode.Tracking && m_target != null) return m_target.transform.position;
                return m_current != null ? m_current.position : Vector3.zero;
            }
        }

        /// <summary>치트(F9) — 지정 Human 을 강제 추적(따라오기). 패스 무시. 원본 SetHoldTracking.</summary>
        /// <param name="target">추적할 Human.</param>
        public void SetHoldTracking(Human target)
        {
            m_hold = true;
            m_holdMode = AIPathMode.Tracking;
            m_target = target;
        }

        /// <summary>치트(F9) — 지정 위치/방향에서 제자리 경계(대기). 밀려나면 복귀. 패스 무시. 원본 SetHoldWait.</summary>
        /// <param name="pos">대기 고정 위치.</param>
        /// <param name="lookDeg">선호 방향(deg).</param>
        public void SetHoldWait(Vector3 pos, float lookDeg)
        {
            m_hold = true;
            m_holdMode = AIPathMode.Wait;
            m_holdPos = pos;
            m_holdLook = lookDeg;
            m_target = null;
        }

        /// <summary>HUMAN 포인트의 시작 식별번호로 첫 웨이포인트 확정. 원본 AIMoveNavi::Init + MovePathNextState.</summary>
        public void Init(int startId) => Resolve(startId);

        /// <summary>현재 웨이포인트의 다음(param2)으로 진행. 원본 MovePathNextState.</summary>
        public void Advance()
        {
            if (m_hold) return; // hold(F9 복제 클론) 는 패스 진행 없음 — 고정 목표 유지
            if (m_current == null) return;
            Resolve(m_current.param2);
        }

        /// <summary>식별번호 id 부터 RAND_AIPATH 분기를 따라가 최종 AIPATH(3) 웨이포인트를 확정. 못 찾으면 무효(정지).</summary>
        private void Resolve(int id)
        {
            for (int guard = 0; guard < 64; guard++) // 데이터 순환 분기 폭주 방지
            {
                if (!MapLoader.TryGetPathPoint(id, out RawPointData p))
                {
                    m_current = null;
                    return;
                }

                if (p.param0 == 8) // RAND_AIPATH: 50:50 분기, 위치 없음 → 통과
                {
                    id = (Random.Range(0, 2) == 0) ? p.param1 : p.param2;
                    continue;
                }

                m_current = p;                 // AIPATH (type 3)

                // Tracking — 추적 대상 Human 을 식별번호(param2=p3)로 1회 검색해 캐싱. 원본 SearchHuman(p4) 체인.
                if (ModeOf(p.param1) == AIPathMode.Tracking && m_target == null)
                    m_target = FindHumanByIdentifier(p.param2);
                return;
            }

            m_current = null;
        }

        private static AIPathMode ModeOf(int p2) => (p2 >= 0 && p2 <= 7) ? (AIPathMode)p2 : AIPathMode.Walk;

        /// <summary>식별번호(Human.Identifier = HUMAN 포인트 p4)로 추적 대상 Human 검색. 원본 SearchHuman. 첫 매치.</summary>
        private static Human FindHumanByIdentifier(int identifier)
        {
            var humans = MapLoader.Humans;
            if (humans == null) return null;
            for (int i = 0; i < humans.Count; i++)
                if (humans[i] != null && humans[i].Identifier == identifier) return humans[i];
            return null;
        }
    }
}
