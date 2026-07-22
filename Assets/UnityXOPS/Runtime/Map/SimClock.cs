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

        // 등록 목록은 static — 종료(quit) 시 드라이버 인스턴스보다 tickable 이 늦게 파괴돼도 Register/Unregister 가 인스턴스에 접근하지 않게 한다.
        // SingletonBehavior.Instance 는 종료 중 null 을 반환하지만 Loaded(=m_instance!=null)는 아직 true 라, 인스턴스 필드를 참조하면 NRE 가 난다(종료 시 간헐 발생).
        private static readonly List<ISimTickable> s_tickables = new List<ISimTickable>();
        private float m_accum;

        // 도메인 리로드 off(에디터 Enter Play Mode 옵션) 시 static 목록이 이전 세션의 파괴된 엔트리를 물고 있지 않도록 플레이 시작마다 비운다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_tickables.Clear();

        /// <summary>
        /// 시뮬레이션 틱 대상을 등록한다 (보통 OnEnable). SimOrder 오름차순 정렬 삽입 — 낮은 값이 먼저 호출된다.
        /// 목록은 static 이라 인스턴스 없이 안전하며, 드라이버가 없으면 여기서 생성한다(종료 중이면 Instance 가 null 반환→생성 안 함). 중복 등록은 무시.
        /// </summary>
        /// <param name="tickable">등록할 대상.</param>
        public static void Register(ISimTickable tickable)
        {
            if (tickable == null || s_tickables.Contains(tickable)) return;

            int i = 0;
            while (i < s_tickables.Count && s_tickables[i].SimOrder <= tickable.SimOrder) i++;
            s_tickables.Insert(i, tickable);

            // 드라이버 인스턴스 보장. Instance getter 는 종료 중이면 null 을 반환(생성 안 함)하므로 접근만 하고 역참조하지 않는다.
            if (!Loaded) { _ = Instance; }
        }

        /// <summary>
        /// 시뮬레이션 틱 대상을 해제한다 (보통 OnDisable). static 목록만 건드리므로 종료(quit) 중 인스턴스 파괴 경합과 무관하게 안전.
        /// </summary>
        /// <param name="tickable">해제할 대상.</param>
        public static void Unregister(ISimTickable tickable)
        {
            if (tickable == null) return;
            s_tickables.Remove(tickable);
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
            for (int i = 0; i < s_tickables.Count; i++)
            {
                ISimTickable t = s_tickables[i];
                // 도메인 리로드 off 등으로 파괴된 오브젝트가 목록에 남았으면 방어적으로 건너뜀(Unity fake-null).
                if (t is UnityEngine.Object o && o == null) continue;
                t.SimTick();
            }
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
