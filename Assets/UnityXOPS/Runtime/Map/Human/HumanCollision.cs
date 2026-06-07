using System.Collections.Generic;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 독립 컴포넌트(별도 GameObject). 살아있는 Human 끼리 겹치면 서로 밀어내는 분리 처리.
    /// 원본 OpenXOPS ObjectManager::CollideHuman(objectmanager.cpp:641-669) + 매 프레임 호출 루프(objectmanager.cpp:2926-2937) 포팅.
    /// 플레이어/AI 구분 없이 모든 Human 이 대상이라 PlayerController/AIController 와 별개로 존재한다.
    ///
    /// 원본 거동: 이동 처리 후 별도 패스에서 두 원기둥(반경 HUMAN_HUMANCOLLISION_R)의 침투깊이 절반씩을
    /// 양쪽 move(속도)에 정반대로 가산 → 다음 프레임 반영. 수평(XZ)만, 시체(HP≤0)는 제외.
    /// move 에 가산하므로(즉시 위치 이동 X) 다음 Tick 에서 벽 충돌이 정상적으로 재해소된다.
    /// </summary>
    public class HumanCollision : MonoBehaviour
    {
        // 원본 GAMEFPS = 33.333. 분리 패스도 AI 와 동일하게 프레임 락(속도 가산 케이던스 일치).
        private const float k_frameTime        = 1f / 33.3333f;
        private const int   k_maxCatchupFrames = 4;

        private readonly Dictionary<Human, HumanController> m_controllers = new Dictionary<Human, HumanController>();
        private float m_accum;

        private void FixedUpdate()
        {
            // Maingame/데모 공통 게이트 (HumanController 와 동일). Briefing 등에서는 정지.
            if (!HumanController.TickEnabled) return;

            IReadOnlyList<Human> humans = MapLoader.Humans;
            if (humans == null || humans.Count == 0)
            {
                if (m_controllers.Count > 0) m_controllers.Clear();
                m_accum = 0f;
                return;
            }

            m_accum += Time.fixedDeltaTime;
            int guard = 0;
            while (m_accum >= k_frameTime && guard++ < k_maxCatchupFrames)
            {
                m_accum -= k_frameTime;
                StepAll(humans);
            }
            if (m_accum > k_frameTime) m_accum = 0f;
        }

        /// <summary>모든 살아있는 Human 쌍(i&lt;j)을 1회씩 검사해 겹친 만큼 양쪽을 밀어낸다.</summary>
        private void StepAll(IReadOnlyList<Human> humans)
        {
            HumanGeneralData gen = DataManager.Instance.HumanParameterData.humanGeneralData;
            float R          = gen.controllerRadiusControllerToController; // 0.25 (원본 HUMAN_HUMANCOLLISION_R 2.5)
            float H          = gen.controllerHeight;                       // 수직 겹침 판정용 (원본 원기둥 높이)
            float minDist    = R + R;                                      // 두 반경 합 = 0.5
            float minDistSqr = minDist * minDist;

            int n = humans.Count;
            for (int i = 0; i < n; i++)
            {
                Human a = humans[i];
                if (a == null || !a.Alive || a.HP <= 0f) continue; // 원본: enableflag && HP>0
                Vector3 pa = a.transform.position;

                for (int j = i + 1; j < n; j++)
                {
                    Human b = humans[j];
                    if (b == null || !b.Alive || b.HP <= 0f) continue;
                    Vector3 pb = b.transform.position;

                    // 수직(Y) 겹침 — 원본 CollideCylinder 높이 조건 |Δy| < H. 위/아래로 떨어졌으면 충돌 아님.
                    if (Mathf.Abs(pa.y - pb.y) >= H) continue;

                    // 수평(XZ) 거리 (b → a 방향)
                    float dx = pa.x - pb.x;
                    float dz = pa.z - pb.z;
                    float distSqr = dx*dx + dz*dz;
                    if (distSqr >= minDistSqr) continue;

                    float dist        = Mathf.Sqrt(distSqr);
                    float penetration = minDist - dist; // 겹친 깊이

                    // 분리 방향 (b → a). 거의 정확히 겹쳤으면 임의 방향(+X) — 원본 atan2(0,0)=0 과 동일.
                    Vector3 dir = dist > 1e-5f
                        ? new Vector3(dx / dist, 0f, dz / dist)
                        : Vector3.right;

                    // 각자 침투깊이의 절반만큼 정반대로 밀림(원본 length/2). 한 프레임(1/33.333s)에 그만큼 변위하도록 속도 환산.
                    float push = penetration * 0.5f / k_frameTime;

                    HumanController ca = GetController(a);
                    HumanController cb = GetController(b);
                    if (ca != null) ca.AddKnockbackVector( dir, push);
                    if (cb != null) cb.AddKnockbackVector(-dir, push);
                }
            }
        }

        private HumanController GetController(Human h)
        {
            if (!m_controllers.TryGetValue(h, out HumanController c))
            {
                c = h.GetComponent<HumanController>();
                m_controllers[h] = c;
            }
            return c;
        }
    }
}
