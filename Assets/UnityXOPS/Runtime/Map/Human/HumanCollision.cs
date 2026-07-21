using System.Collections.Generic;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 독립 컴포넌트(별도 GameObject). 살아있는 Human 끼리 겹치면 서로 밀어내는 분리 처리.
    /// 원본 OpenXOPS ObjectManager::CollideHuman(objectmanager.cpp:641-669) + 매 프레임 호출 루프(objectmanager.cpp:2926-2937) 포팅.
    /// 플레이어/AI 구분 없이 모든 Human 이 대상이라 PlayerController/AIController 와 별개로 존재한다.
    ///
    /// 원본 거동: 두 원기둥(반경 HUMAN_HUMANCOLLISION_R)의 침투깊이 절반씩을 양쪽 move(속도)에 정반대로 가산. 수평(XZ)만, 시체(HP≤0)는 제외.
    /// SimClock 순서상 인간간충돌(O10)이 HumanController 이동/Tick보다 먼저라, 가산된 속도는 같은 틱의 Tick 에서 소비돼 벽 충돌과 함께 해소된다.
    /// </summary>
    public class HumanCollision : MonoBehaviour, ISimTickable
    {
        private readonly Dictionary<Human, HumanController> m_controllers = new Dictionary<Human, HumanController>();

        // 원본 인간간충돌(O10) — AI판단(P3)보다 먼저. 케이던스/게이트/누산은 SimClock 이 소유.
        public int SimOrder => 100;

        private void OnEnable() => SimClock.Register(this);
        private void OnDisable() => SimClock.Unregister(this);

        /// <summary>미션 재시작(맵 재로드) 시 컨트롤러 캐시 초기화 — 파괴된 Human 키의 stale 참조 제거.</summary>
        public void ResetState()
        {
            m_controllers.Clear();
        }

        public void SimTick()
        {
            IReadOnlyList<Human> humans = MapLoader.Humans;
            if (humans == null || humans.Count == 0)
            {
                if (m_controllers.Count > 0) m_controllers.Clear();
                return;
            }
            StepAll(humans);
        }

        /// <summary>모든 살아있는 Human 쌍(i&lt;j)을 1회씩 검사해 겹친 만큼 양쪽을 밀어낸다.</summary>
        private void StepAll(IReadOnlyList<Human> humans)
        {
            HumanGeneralData gen = DataManager.Instance.HumanParameterData.humanGeneralData;
            float R = gen.controllerRadiusControllerToController; // 0.25 (원본 HUMAN_HUMANCOLLISION_R 2.5)
            float H = gen.controllerHeight; // 수직 겹침 판정용 (원본 원기둥 높이)
            float minDist = R + R; // 두 반경 합 = 0.5
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

                    float dist = Mathf.Sqrt(distSqr);
                    float penetration = minDist - dist; // 겹친 깊이

                    // 분리 방향 (b → a). 거의 정확히 겹쳤으면 임의 방향(+X) — 원본 atan2(0,0)=0 과 동일.
                    Vector3 dir = dist > 1e-5f
                        ? new Vector3(dx / dist, 0f, dz / dist)
                        : Vector3.right;

                    // 각자 침투깊이의 절반만큼 정반대로 밀림(원본 length/2). 한 틱(SimClock.FrameTime)에 그만큼 변위하도록 속도 환산.
                    float push = penetration * 0.5f / SimClock.FrameTime;

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
