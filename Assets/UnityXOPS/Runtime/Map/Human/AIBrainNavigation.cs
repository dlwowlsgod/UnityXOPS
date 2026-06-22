using UnityEngine;

namespace UnityXOPS
{
    // AIBrain 경로 추종/순찰 partial (원본 MovePath ai.cpp:1489 + MoveTarget ai.cpp:133).
    public partial class AIBrain
    {
        /// <summary>현재 웨이포인트로 이동/도착 처리. 경로 없으면 제자리 두리번. 도착 시 모드별 행동(대기/5초정지/다음).</summary>
        private void MovePath()
        {
            if (!m_nav.Valid)
            {
                // 경로 미지정 — 제자리에서 두리번거림.
                TurnSeen();
                ArmAngle();
                return;
            }

            ArmAngle();

            if (!CheckArrived())
            {
                MoveTarget();
                return;
            }

            // 도착 — 모드별 행동.
            switch (m_nav.Mode)
            {
                case AIPathMode.Wait: // 무한 대기 — 제자리 두리번 (ChangeToWalk 이벤트로만 해제)
                case AIPathMode.WaitAlert: // 경계 대기 — 대기 중엔 Wait 과 동일, 단 경계(이상) 진입 시 다음 경로로 진행(NormalMain 에서 처리)
                case AIPathMode.Tracking: // 추적 대상에 도착(1.8 이내) → 제자리 두리번. 대상이 움직이면 다시 추격.
                    TurnSeen();
                    break;

                case AIPathMode.Stop5Sec:
                    // 포인트 방향(look)을 보며 5초 대기 후 다음.
                    if (m_waitCnt < GeneralData.aiStop5SecFrames)
                    {
                        if (StopSeen()) m_waitCnt++;
                    }
                    else
                    {
                        m_waitCnt = 0;
                        m_nav.Advance();
                    }
                    break;

                default: // Walk/Run/Run2/Grenade/Random → 즉시 다음 포인트
                    m_waitCnt = 0;
                    m_nav.Advance();
                    break;
            }
        }

        /// <summary>웨이포인트 도착 판정 — 수평 거리 < aiArrivalDistPath. 원본 CheckTargetPos (ai.cpp:100, AI_ARRIVALDIST_PATH).</summary>
        private bool CheckArrived()
        {
            Vector3 d = m_nav.TargetPos - m_self.transform.position;
            d.y = 0f;
            float arrive = (m_nav.Mode == AIPathMode.Tracking)
                ? GeneralData.aiArrivalDistTracking // 추적은 더 멀리서 "도착"(18→1.8)
                : GeneralData.aiArrivalDistPath;
            return d.sqrMagnitude < arrive * arrive;
        }

        /// <summary>목표 웨이포인트로 선회 + 전진. 모드별 전진 허용각(Walk 6°/Run 50°). 점프(1/16)·끼임 탈출(1/28). 원본 MoveTarget (ai.cpp:133).</summary>
        private void MoveTarget()
        {
            Vector3 to = m_nav.TargetPos - m_self.transform.position;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist < 1e-4f) return;

            float desiredYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            float atan = Mathf.DeltaAngle(m_yaw, desiredYaw);

            // 선회 (Action 조준과 동일 부호 규약: DeltaAngle>0 → 우회전)
            if (atan > k_turnTowardDeg) m_turnRight = true;
            if (atan < -k_turnTowardDeg) m_turnLeft = true;

            // 전진 — 허용각 안으로 정렬됐을 때만. 모드별 허용각/걷기·달리기 분기.
            if (IsZombie())
            {
                // 좀비 — 경로 추종 중엔 movemode 무시, 정면 ±20° 안에서 걷기만 (원본 ai.cpp:176-180).
                if (Mathf.Abs(atan) < k_zombiePathTol)
                    m_moveIntent = HumanMoveFlag.Forward | HumanMoveFlag.Walk;
            }
            else if (m_nav.Mode == AIPathMode.Tracking)
            {
                // 추적 — ±20° 안이면 전진. 걷기 전환 거리(2.4) 이내면 걷고, 멀면 달림 (원본 ai.cpp:196-205).
                if (Mathf.Abs(atan) < k_trackForwardTol)
                {
                    m_moveIntent = HumanMoveFlag.Forward;
                    if (dist < GeneralData.aiArrivalDistWalkTracking) m_moveIntent |= HumanMoveFlag.Walk;
                }
            }
            else
            {
                bool run = (m_nav.Mode == AIPathMode.Run || m_nav.Mode == AIPathMode.Run2);
                float tol = run ? k_forwardTolRun : k_forwardTolWalk;
                if (Mathf.Abs(atan) < tol)
                {
                    m_moveIntent = HumanMoveFlag.Forward;
                    if (!run) m_moveIntent |= HumanMoveFlag.Walk;
                }
            }

            // 점프 — 진행 방향 앞에 장애물이 있을 때만 (1/16 프레임).
            if (GetRand(k_jumpChance) == 0 && JumpBlocked()) m_jumpRequested = true;

            // 끼임 탈출 — 전진 의사가 있는데 직전 프레임에 거의 안 움직였으면 랜덤 선회 (1/28 프레임).
            if (GetRand(k_stuckChance) == 0 && m_moveIntent != HumanMoveFlag.None)
            {
                Vector3 moved = m_self.transform.position - m_lastFramePos;
                moved.y = 0f;
                if (moved.sqrMagnitude < k_stuckMoveSqr)
                {
                    if (Random.Range(0, 2) == 0) m_turnRight = true; else m_turnLeft = true;
                }
            }
        }

        /// <summary>STOP_5SEC 대기 시 포인트 look 방향으로 정렬. 정렬 완료면 true. 원본 StopSeen (ai.cpp:471).</summary>
        private bool StopSeen()
        {
            float tr = Mathf.DeltaAngle(m_yaw, m_nav.TargetLook);
            if (tr > 2.5f) { m_turnRight = true; return false; }
            if (tr < -2.5f) { m_turnLeft = true; return false; }
            return true;
        }

        /// <summary>진행 방향 앞 허리 높이에 블록이 있는지 — 점프 트리거. 원본 MoveJump (ai.cpp:504) 의 장애물 감지 단순화.</summary>
        private bool JumpBlocked()
        {
            HumanGeneralData gen = GeneralData;
            Vector3 fwd = Quaternion.Euler(0f, m_yaw, 0f) * Vector3.forward;
            Vector3 origin = m_self.transform.position + Vector3.up * (gen.controllerHeight * 0.5f);
            float dist = gen.aiJumpCheckDist + gen.controllerRadiusControllerToMap;
            return Physics.Raycast(origin, fwd, dist, MapLoader.BlockLayerMask);
        }
    }
}
