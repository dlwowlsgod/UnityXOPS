using UnityEngine;

namespace UnityXOPS
{
    // AIBrain 전투(조준/사격/회피) partial (원본 Action ai.cpp:559 / ActionCancel ai.cpp:841 / MoveRandom ai.cpp:274).
    public partial class AIBrain
    {
        // === 전투 종료 판정 (원본 ActionCancel ai.cpp:841) ========================
        private bool ActionCancel()
        {
            if (m_enemy == null || !m_enemy.Alive) return true;

            float sqr = (m_enemy.transform.position - m_self.transform.position).sqrMagnitude;
            float cancel = GeneralData.aiActionCancelDist;
            if (sqr > cancel * cancel) return true;

            // 시야 차단 확률 체크 (근거리 1/40, 원거리 1/30) + 강제 종료 (1/550, 1/450).
            int losChance = m_longAttack ? 30 : 40;
            if (GetRand(losChance) == 0 && !HasLineOfSight(EyePos(m_self), EyePos(m_enemy)))
                return true;

            int forced = m_longAttack ? 450 : 550;
            if (GetRand(forced) == 0) return true;

            return false;
        }

        // === 조준 / 사격 (원본 Action ai.cpp:559) =================================
        private void Action()
        {
            Weapon w = m_self.CurrentWeapon;

            Vector3 myEye = EyePos(m_self);
            Vector3 d = EyePos(m_enemy) - myEye;
            float dist = d.magnitude;
            if (dist < 1e-4f) return;

            m_longAttack = dist > GeneralData.aiShortAttackDist;

            // RUN2(우선적 달리기) 전투 — 원거리 정밀조준 모드로 안 감(항상 근접 유지, 원본 ai.cpp:828).
            bool run2 = m_nav.Mode == AIPathMode.Run2;
            if (run2) m_longAttack = false;

            float desiredYaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float atanx = Mathf.DeltaAngle(m_yaw, desiredYaw); // 좌우 오차
            float dz = GeneralData.aiTurnDeadzoneDeg;

            // 좀비 — 맨손이지만 항복 분기가 아니라 근접 돌격. (좀비 판정을 IsNoneWeapon 항복보다 먼저)
            if (IsZombie())
            {
                ZombieFight(d, dist, atanx, dz);
                return;
            }

            // 맨손 항복 — 무장한 적 앞이면 팔을 위로(손들기), 적도 맨손이면 아래로. 가끔 후퇴. 사격 없음.
            // 원본 GunFight ai.cpp:683-717. 팔 위 = m_pitch 음수(-70 클램프) → dynamicArm Euler +70(위). HaveWeapon 으로도 못 무장하면 이 상태 유지.
            if (IsNoneWeapon(w))
            {
                if (atanx > dz) m_turnRight = true;
                if (atanx < -dz) m_turnLeft = true;

                if (!IsNoneWeapon(m_enemy.CurrentWeapon)) m_turnUp = true; // 손들기
                else m_turnDown = true;

                if (GetRand(80) == 0) m_combatMove |= HumanMoveFlag.Back; // 1/80 후퇴 (원본 ai.cpp:709-717). CombatMoveCancel 이 해제.
                return;
            }

            HumanAIData ai = AiLevel();
            if (ai == null) return;

            float desiredPitch = -Mathf.Asin(Mathf.Clamp(d.y / dist, -1f, 1f)) * Mathf.Rad2Deg;
            float atany = Mathf.DeltaAngle(m_pitch, desiredPitch); // 상하 오차 (+ = 더 아래로 조준해야 함)

            // 조준 빈도 게이트 — aiming 이 0 이면 회전 안 함 (원본 randr=aiming). 보통 ≥1 이라 매 프레임 보정.
            if (ai.aiming != 0)
            {
                if (atanx > dz) m_turnRight = true;
                if (atanx < -dz) m_turnLeft = true;
                if (atany > dz) m_turnDown = true;
                if (atany < -dz) m_turnUp = true;
            }

            // 발사 판정 각도 = 스코프 기본각 ± limitsError 보정. 합산 오차가 이 안이면 발사 후보.
            HumanAIScopeData sc = ScopeAi(w);
            float shotAngle = m_longAttack ? sc.aiShotAngleLong : sc.aiShotAngle;
            shotAngle += (m_longAttack ? 0.2f : 0.5f) * ai.limitsError;
            if (run2) shotAngle *= k_run2ShotAngleScale; // RUN2 — 달리며 쏘므로 발사 허용각 완화 (원본 ai.cpp:797)
            if (shotAngle < 0f) shotAngle = 0f;

            if (Mathf.Abs(atanx) + Mathf.Abs(atany) < shotAngle)
            {
                int atk = ai.attack + (m_longAttack ? 1 : 0);
                if (GetRand(atk) == 0)
                    w.Shoot(m_self); // SpawnBullets 가 controller.Yaw/Pitch 방향으로 직사
            }
        }

        // === 전투 회피 이동 (원본 MoveRandom ai.cpp:274 / CancelMoveTurn ai.cpp:947) ====

        /// <summary>전투 이동 지속 플래그를 확률적으로 해제 (원본 CancelMoveTurn 의 MOVE 부분, ACTION). 근/원거리 다름.</summary>
        private void CombatMoveCancel()
        {
            int fwd = m_longAttack ? 5 : 6;
            int back = m_longAttack ? 4 : 6;
            int side = m_longAttack ? 5 : 7;
            if (GetRand(fwd) == 0) m_combatMove &= ~HumanMoveFlag.Forward;
            if (GetRand(back) == 0) m_combatMove &= ~HumanMoveFlag.Back;
            if (GetRand(side) == 0) m_combatMove &= ~HumanMoveFlag.Left;
            if (GetRand(side) == 0) m_combatMove &= ~HumanMoveFlag.Right;
        }

        /// <summary>
        /// 전투 중 무작위 회피 이동 (원본 MoveRandom ai.cpp:274). 4방향 지속 플래그를 낮은 확률로 set —
        /// CombatMoveCancel(높은 확률 해제)과의 비대칭으로 짧은 스트레이핑이 나온다. 벽/낭떠러지 회피 + 적 근접 후퇴. 전부 달리기(Walk 미사용).
        /// </summary>
        private void MoveRandom()
        {
            int fwd, back, side;
            if (!m_longAttack) { fwd = 80; back = 90; side = 70; } // 근거리 — 자주 움직임
            else { fwd = 120; back = 150; side = 130; } // 원거리 — 덜 움직임

            if (GetRand(fwd) == 0) m_combatMove |= HumanMoveFlag.Forward;
            if (GetRand(back) == 0) m_combatMove |= HumanMoveFlag.Back;
            if (GetRand(side) == 0) m_combatMove |= HumanMoveFlag.Left;
            if (GetRand(side) == 0) m_combatMove |= HumanMoveFlag.Right;

            // 벽/낭떠러지 회피 — 1/3 확률 또는 이미 이동 중이면 한 축 검사 후 반대로.
            if (GetRand(3) == 0 || m_combatMove != HumanMoveFlag.None) WallCliffAvoid();

            // 적 근접 시 전진 강제 취소 + 1/70 후퇴.
            if (m_enemy != null)
            {
                float retreat = GeneralData.aiCombatRetreatDist;
                if ((m_enemy.transform.position - m_self.transform.position).sqrMagnitude < retreat * retreat)
                {
                    m_combatMove &= ~HumanMoveFlag.Forward;
                    if (GetRand(70) == 0) m_combatMove |= HumanMoveFlag.Back;
                }
            }
        }

        /// <summary>
        /// RUN2 전투 중 경로 목표점으로의 전방위 이동 (원본 MoveTarget2 ai.cpp:225-272). 몸은 적을 겨눈 채(선회는 Action 담당),
        /// 목표점이 놓인 방향에 따라 전진/후진/좌우 스트레이프를 골라 이동 의사(m_moveIntent)로 넣는다 — 등을 안 보이며 접근. 전부 달리기.
        /// </summary>
        private void MoveTarget2()
        {
            Vector3 to = m_nav.TargetPos - m_self.transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 1e-8f) return;

            float desiredYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            float atan = Mathf.DeltaAngle(m_yaw, desiredYaw); // + = 목표점이 몸 기준 오른쪽

            if (Mathf.Abs(atan) < k_run2ForwardTol) m_moveIntent |= HumanMoveFlag.Forward;
            if (Mathf.Abs(atan) > k_run2BackTol) m_moveIntent |= HumanMoveFlag.Back;
            if (atan > k_run2StrafeMin && atan < k_run2StrafeMax) m_moveIntent |= HumanMoveFlag.Right;
            if (atan < -k_run2StrafeMin && atan > -k_run2StrafeMax) m_moveIntent |= HumanMoveFlag.Left;

            // 점프 — 진행 방향 앞에 장애물이 있을 때만 (1/16 프레임). 원본 MoveTarget2 (ai.cpp:257).
            if (GetRand(k_jumpChance) == 0 && JumpBlocked()) m_jumpRequested = true;

            // 끼임 탈출 — 이동 의사가 있는데 직전 프레임에 거의 안 움직였으면 랜덤 선회 (1/28 프레임). 원본 ai.cpp:264-270.
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

        /// <summary>한 축(전후 또는 좌우)을 골라 진행 방향에 벽/낭떠러지가 있으면 반대 방향으로 전환. 원본 ai.cpp:312-366.</summary>
        private void WallCliffAvoid()
        {
            Quaternion yawQ = Quaternion.Euler(0f, m_yaw, 0f);
            Vector3 fwd = yawQ * Vector3.forward;
            Vector3 right = yawQ * Vector3.right;

            if (Random.Range(0, 2) == 0)
            {
                if (BlockedOrCliff(fwd)) { m_combatMove &= ~HumanMoveFlag.Forward; m_combatMove |= HumanMoveFlag.Back; }
                else if (BlockedOrCliff(-fwd)) { m_combatMove &= ~HumanMoveFlag.Back; m_combatMove |= HumanMoveFlag.Forward; }
            }
            else
            {
                if (BlockedOrCliff(right)) { m_combatMove &= ~HumanMoveFlag.Right; m_combatMove |= HumanMoveFlag.Left; }
                else if (BlockedOrCliff(-right)) { m_combatMove &= ~HumanMoveFlag.Left; m_combatMove |= HumanMoveFlag.Right; }
            }
        }

        /// <summary>방향 dir 로 허리높이 벽이 있거나(충돌) 앞쪽 발밑에 바닥이 없으면(낭떠러지) true. 원본 MoveRandom 의 벽/공허 레이.</summary>
        private bool BlockedOrCliff(Vector3 dir)
        {
            HumanGeneralData gen = GeneralData;
            Vector3 pos = m_self.transform.position;
            float probe = gen.controllerRadiusControllerToMap + k_combatProbeDist;

            Vector3 waist = pos + Vector3.up * (gen.controllerHeight * 0.5f);
            if (Physics.Raycast(waist, dir, probe, MapLoader.BlockLayerMask)) return true; // 벽

            Vector3 ahead = pos + dir * probe + Vector3.up * 0.1f;
            if (!Physics.Raycast(ahead, Vector3.down, k_cliffProbeDown, MapLoader.BlockLayerMask)) return true; // 낭떠러지

            return false;
        }
    }
}
