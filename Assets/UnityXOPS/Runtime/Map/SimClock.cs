using System.Collections.Generic;
using JJLUtility;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 게임플레이 33.333fps 단일 틱을 발행하는 시뮬레이션 시계. 원본 OpenXOPS 는 프레임당 1회 고정 케이던스로 전체 로직을 돈다.
    /// 등록된 ISimTickable 을 SimOrder 오름차순으로 한 틱에 1회씩 호출한다. 렌더레이트/50Hz 와 분리해 프레임레이트 독립성을 확보한다.
    /// DontDestroyOnLoad 싱글턴이며 HumanController.TickEnabled 인 씬(Maingame/데모)에서만 틱을 발행한다.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class SimClock : SingletonBehavior<SimClock>
    {
        // 원본 GAMEFPS. 게임플레이 프레임 단위 환산의 단일 소스 — 하드코딩 33.333 대신 이 값을 참조한다.
        public const float FrameRate = 33.3333f;
        // 한 틱 시간(초) = 1/FrameRate. 침투깊이→속도 환산 등 dt 기반 계산이 참조.
        public const float FrameTime = 1f / FrameRate;

        private const int k_maxCatchup = 4; // 프레임 폭주 방지 (긴 프레임에 누산이 몰려도 최대 4틱만 따라잡음)

        private readonly List<ISimTickable> m_tickables = new List<ISimTickable>();
        private float m_accum;

        /// <summary>
        /// 시뮬레이션 틱 대상을 등록한다 (보통 OnEnable). SimOrder 오름차순 정렬 삽입 — 낮은 값이 먼저 호출된다.
        /// 드라이버 인스턴스가 없으면 접근 시 자동 생성된다. 중복 등록은 무시.
        /// </summary>
        /// <param name="tickable">등록할 대상.</param>
        public static void Register(ISimTickable tickable)
        {
            if (tickable == null) return;
            SimClock clock = Instance;
            if (clock == null) return;

            List<ISimTickable> list = clock.m_tickables;
            if (list.Contains(tickable)) return;

            int i = 0;
            while (i < list.Count && list[i].SimOrder <= tickable.SimOrder) i++;
            list.Insert(i, tickable);
        }

        /// <summary>
        /// 시뮬레이션 틱 대상을 해제한다 (보통 OnDisable). 드라이버가 이미 파괴됐으면 무시.
        /// </summary>
        /// <param name="tickable">해제할 대상.</param>
        public static void Unregister(ISimTickable tickable)
        {
            if (tickable == null || !Loaded) return;
            Instance.m_tickables.Remove(tickable);
        }

        private void FixedUpdate()
        {
            // Maingame/데모 공통 게이트. Briefing 등 정지 씬에서는 누산조차 안 함(재개 시 버스트 방지).
            if (!HumanController.TickEnabled) { m_accum = 0f; return; }

            m_accum += Time.fixedDeltaTime;
            int guard = 0;
            while (m_accum >= FrameTime && guard++ < k_maxCatchup)
            {
                m_accum -= FrameTime;
                Step();
            }
            if (m_accum > FrameTime) m_accum = 0f;
        }

        /// <summary>한 시뮬레이션 틱 — 등록된 모든 대상을 SimOrder 순으로 1회씩 진행.</summary>
        private void Step()
        {
            for (int i = 0; i < m_tickables.Count; i++)
                m_tickables[i].SimTick();
        }
    }

    /// <summary>
    /// SimClock 이 33.333fps 로 호출하는 시뮬레이션 대상. SimOrder 로 한 틱 안의 실행 순서를 정한다(원본 프레임 루프 순서 재현).
    /// 원본 순서: 이동/맵충돌 → 무기/총알 → 인간간충돌 → AI판단 → 미션판정/이벤트.
    /// </summary>
    public interface ISimTickable
    {
        /// <summary>틱 실행 순서 (오름차순, 낮을수록 먼저).</summary>
        int SimOrder { get; }
        /// <summary>시뮬레이션 1틱 진행 (원본 33.333fps 1프레임 분량).</summary>
        void SimTick();
    }
}
