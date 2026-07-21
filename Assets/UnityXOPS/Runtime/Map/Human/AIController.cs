using System.Collections.Generic;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 독립 컴포넌트(별도 GameObject). PlayerController 와 대칭 — 수동 조작 주체(PlayerController)가 소유한 Human 을
    /// 제외한 모든 Human 의 AI 를 한 곳에서 구동한다. 원본 OpenXOPS gamemain.cpp:2546-2555 의 전체 AI Process 루프 대응
    /// (HumanAI[] 배열을 순회하되 PlayerID 는 스킵). Maingame 은 PlayerController 가 있어 Player 를 제외하지만,
    /// Mainmenu/Opening 데모는 PlayerController 가 없어 Player Human 까지 전부 AI 가 구동한다(원본 데모 동작).
    ///
    /// 프레임 락: 원본 AI 는 33.333fps 정수 카운트/확률 모델(GetRand, 회전 ×0.8/frame 감쇠, cautioncnt 160프레임).
    /// Unity 가변 스텝에 dt 를 곱하면 동작이 깨지므로, 누산기로 30ms 마다 전체 brain 을 정확히 한 번씩 Tick 한다.
    /// 각 Human 의 AI 상태는 AIBrain 인스턴스가 보유 (Human 1명 = brain 1개).
    /// </summary>
    public class AIController : MonoBehaviour, ISimTickable
    {
        private readonly Dictionary<Human, AIBrain> m_brains = new Dictionary<Human, AIBrain>();

        // 원본 AI판단(P3) — 인간간충돌(O10) 뒤, 이벤트(P5) 앞. 케이던스/게이트/누산은 SimClock 이 소유.
        public int SimOrder => 200;

        private void OnEnable() => SimClock.Register(this);
        private void OnDisable() => SimClock.Unregister(this);

        /// <summary>미션 재시작(맵 재로드) 시 brain 캐시 초기화 — 파괴된 Human 키의 stale brain 제거.</summary>
        public void ResetState()
        {
            m_brains.Clear();
        }

        // 수동 조작 주체(PlayerController). 씬에 존재하면 MapLoader.Player 를 AI 에서 제외하고, 없으면(Mainmenu/Opening 데모)
        // 플레이어 Human 도 AI 가 구동한다. per-scene MonoBehaviour 이므로 씬 로드 시점에 1회만 resolve.
        private PlayerController m_playerController;
        private bool m_playerControllerResolved;

        // SimClock 33.333fps 틱 — AI 판단(원본 P3). 결정한 이동 의사는 FixedUpdate 의 ApplyMovementAll 이 50fps 로 재적용/소비.
        public void SimTick()
        {
            IReadOnlyList<Human> humans = MapLoader.Humans;
            if (humans == null || humans.Count == 0)
            {
                // 맵 언로드/리로드로 Human 이 사라지면 stale brain 정리 (파괴된 Human 키 누적 방지).
                if (m_brains.Count > 0) m_brains.Clear();
                return;
            }
            StepAll(humans);
        }

        // 50Hz 브릿지 — HumanController 가 이동 플래그를 매 FixedUpdate(50fps) 소비/클리어하므로, 33.333fps 로 결정한 이동
        // 의사를 물리 스텝마다 재적용한다. AIFrame 이 안 도는 스텝에도 직전 결정의 이동 의사를 유지해야 속도/부드러움이 맞는다.
        // (Phase 1-B step 4 에서 이동이 SimClock 으로 합쳐지면 이 브릿지는 SimTick 으로 흡수돼 사라진다.)
        private void FixedUpdate()
        {
            if (!HumanController.TickEnabled) return;
            IReadOnlyList<Human> humans = MapLoader.Humans;
            if (humans == null || humans.Count == 0) return;
            ApplyMovementAll(humans);
        }

        /// <summary>
        /// AI 에서 제외할 Human (수동 조작 주체가 소유). PlayerController 가 씬에 있으면 MapLoader.Player,
        /// 없으면(Mainmenu/Opening 데모) null → 플레이어 Human 도 AI 가 구동. PlayerController 는 씬 로드 시점에 1회만 resolve.
        /// </summary>
        private Human GetManualHuman()
        {
            if (!m_playerControllerResolved)
            {
                m_playerController         = FindFirstObjectByType<PlayerController>();
                m_playerControllerResolved = true;
            }
            return m_playerController != null ? MapLoader.Player : null;
        }

        /// <summary>모든 AI Human brain 의 이동 의사를 컨트롤러에 적용 (매 FixedUpdate).</summary>
        private void ApplyMovementAll(IReadOnlyList<Human> humans)
        {
            Human manual = GetManualHuman();
            for (int i = 0; i < humans.Count; i++)
            {
                Human h = humans[i];
                if (h == null || h == manual) continue;
                if (m_brains.TryGetValue(h, out AIBrain brain)) brain.ApplyMovement();
            }
        }

        /// <summary>수동 조작 Human 을 제외한 모든 Human 의 brain 을 1프레임씩 진행. 신규 Human 은 brain 을 lazy 생성.</summary>
        private void StepAll(IReadOnlyList<Human> humans)
        {
            Human manual = GetManualHuman();
            for (int i = 0; i < humans.Count; i++)
            {
                Human h = humans[i];
                if (h == null || h == manual) continue; // 수동 조작 주체가 소유한 Human 만 스킵 (원본 PlayerAI 스킵)

                if (!m_brains.TryGetValue(h, out AIBrain brain))
                {
                    brain = new AIBrain(h);
                    m_brains[h] = brain;
                }
                brain.Tick();
            }
        }
    }
}
