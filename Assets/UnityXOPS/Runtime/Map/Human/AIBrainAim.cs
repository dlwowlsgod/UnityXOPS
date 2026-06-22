using System.Collections.Generic;
using UnityEngine;

namespace UnityXOPS
{
    // AIBrain 조준/두리번/탐색/시야 partial (원본 TurnSeen ai.cpp:390 / ApplyTurn AIObjectDriver / SearchEnemy ai.cpp:1297).
    public partial class AIBrain
    {
        /// <summary>
        /// 원본 TurnSeen (ai.cpp:390). 경계/평상시 주위를 랜덤하게 둘러봄. 회전 플래그는 프레임 간 지속(m_scan*)되며
        /// ApplyTurn 관성 모델(가속+0.8 감쇠)이 부드러운 회전으로 만든다. 명시적 방향 타이머 없이 set/del 확률 중첩이 두리번거림.
        /// 소리 경계는 방향을 안 씀(랜덤). 피격 시(FaceCaution)에만 그 방향으로 정확히 회전.
        /// </summary>
        private void TurnSeen()
        {
            // 피격 방향 조준 (원본 ai.cpp:399-419) — 랜덤 두리번 대신 공격자 방향으로 ±2.5° 회전, 다 돌면 해제.
            if (m_mode == BattleMode.Caution && m_faceCaution)
            {
                float tr = Mathf.DeltaAngle(m_yaw, m_faceCautionYaw);
                if (tr > 2.5f) m_turnRight = true;
                if (tr < -2.5f) m_turnLeft = true;
                if (Mathf.Abs(tr) <= 2.5f) m_faceCaution = false;
                return;
            }

            int turnStart = (m_mode == BattleMode.Caution) ? 20 : 85; // 경계가 평상시보다 훨씬 자주 둘러봄 (원본 20 vs 85)
            int turnStop = (m_mode == BattleMode.Caution) ? 20 : 18;

            if (GetRand(turnStart) == 0) m_scanRight = true;
            if (GetRand(turnStart) == 0) m_scanLeft = true;
            if (GetRand(turnStop) == 0) m_scanRight = false;
            if (GetRand(turnStop) == 0) m_scanLeft = false;

            if (m_scanRight) m_turnRight = true;
            if (m_scanLeft) m_turnLeft = true;
        }

        /// <summary>
        /// 원본 ArmAngle (ai.cpp:1265). 무기/상태별 팔 pitch 유지. Unity controller.Pitch(양수=아래) = 원본 arm ry 부호 반전.
        /// 맨손=계속 내림 / 경계=정면 수평(0) 겨눔 / 평상시 무기=살짝 아래(armAngleInitial) 유지.
        /// </summary>
        private void ArmAngle()
        {
            // 좀비도 맨손이라 NORMAL/CAUTION(총알 스침·발소리·총성·피격 등 반응)에선 팔을 내림 — 원본 ArmAngle 그대로.
            // 좀비 팔을 드는 건 오직 ACTION(적 식별·교전, ZombieFight)뿐. (원본 object.cpp/ai.cpp: ArmAngle 맨손→TURNDOWN, 팔 -15°는 Action 에서만)
            bool noWeapon = IsNoneWeapon(m_self.CurrentWeapon);

            if (noWeapon)
            {
                m_turnDown = true; // 맨손 — 팔 계속 내림
            }
            else if (m_mode == BattleMode.Caution && m_cautionCnt > 0)
            {
                // 경계 — 정면 수평 겨눔 (목표 pitch 0, ±1° 데드밴드). 긴장한 조준 자세.
                if (m_pitch > 1f) m_turnUp = true;
                if (m_pitch < -1f) m_turnDown = true;
            }
            else
            {
                // 평상시 무기 소지 — pitch ≈ -armAngleInitial(살짝 아래) ±2° 유지. 원본 arm ry -28~-32.
                float rest = -GeneralData.armAngleInitial;
                if (m_pitch > rest + 2f) m_turnUp = true;
                if (m_pitch < rest - 2f) m_turnDown = true;
            }
        }

        // === 회전 적분 (원본 AIObjectDriver::ControlObject ai.cpp:2316) ============
        private void ApplyTurn()
        {
            float rate = GeneralData.aiTurnRateDeg;
            if (m_turnRight) m_addYaw += rate;
            if (m_turnLeft) m_addYaw -= rate;
            if (m_turnDown) m_addPitch += rate;
            if (m_turnUp) m_addPitch -= rate;

            m_yaw += m_addYaw;
            m_pitch += m_addPitch;

            float maxPitch = GeneralData.aiTurnMaxPitchDeg;
            m_pitch = Mathf.Clamp(m_pitch, -maxPitch, maxPitch);

            float damp = GeneralData.aiTurnDamping;
            m_addYaw *= damp;
            m_addPitch *= damp;

            float dz = GeneralData.aiTurnDeadzoneDeg;
            if (Mathf.Abs(m_addYaw) < dz) m_addYaw = 0f;
            if (Mathf.Abs(m_addPitch) < dz) m_addPitch = 0f;

            m_controller.SetYawPitch(m_yaw, m_pitch);
        }

        // === 타겟 탐색 (원본 SearchEnemy ai.cpp:1297) =============================
        /// <summary>랜덤 샘플링으로 적을 찾는다. 발견 시 m_enemy 설정. 0=없음 / 1=근거리 / 2=원거리.</summary>
        private int SearchEnemy()
        {
            IReadOnlyList<Human> humans = MapLoader.Humans;
            int n = humans != null ? humans.Count : 0;
            if (n == 0) return 0;

            HumanAIData ai = AiLevel();
            if (ai == null) return 0;

            HumanGeneralData gen = GeneralData;
            bool caution = m_mode == BattleMode.Caution;
            HumanAIScopeData sc = ScopeAi(m_self.CurrentWeapon);

            int loops = ai.search * gen.aiSearchLoopScale;
            if (caution) loops += gen.aiSearchLoopScale; // 경계 시 탐색 강화

            float baseDist = caution ? gen.aiSearchDistBaseCaution : gen.aiSearchDistBaseNormal;
            float coeff = caution ? gen.aiSearchDistCoeffCaution : gen.aiSearchDistCoeffNormal;
            float addDist = caution ? sc.aiAddSearchDistCaution : sc.aiAddSearchDistNormal;
            float maxDist = baseDist + coeff * (ai.search - 2) + addDist;

            float aH = caution ? gen.aiSearchFovHNearCaution : gen.aiSearchFovHNear;
            float aV = caution ? gen.aiSearchFovVNearCaution : gen.aiSearchFovVNear;
            float bH = caution ? gen.aiSearchFovHLongCaution : gen.aiSearchFovHLong;
            float bV = caution ? gen.aiSearchFovVLongCaution : gen.aiSearchFovVLong;

            float nearDist = gen.aiShortAttackDist;

            // 고정 풀(원본 MAX_HUMAN)에서 무작위 1명 샘플 — 인덱스가 실제 인원을 넘으면 빈 슬롯=miss.
            // 인원이 적어도 특정 적을 뽑을 확률이 1/pool 로 낮아 "발견에 확률적 지연"(원본 동작) 이 생긴다.
            // 이게 없으면(dense 리스트 샘플) 적이 보이는 즉시 1프레임 만에 발견해 반응이 너무 빨라짐.
            int pool = Mathf.Max(n, gen.aiSearchPoolSize);

            for (int i = 0; i < loops; i++)
            {
                int idx = GetRand(pool);
                if (idx >= n) continue; // 빈 슬롯 — 검사 안 함
                Human t = humans[idx];

                if (CheckLookEnemy(t, aH, aV, nearDist))
                {
                    m_enemy = t;
                    m_longAttack = false;
                    return 1;
                }
                if (CheckLookEnemy(t, bH, bV, maxDist))
                {
                    m_enemy = t;
                    m_longAttack = GetRand(4) == 0;
                    return 2;
                }
            }
            return 0;
        }

        /// <summary>원본 CheckLookEnemy (ai.cpp:1398). 팀·시야각·가림막(LOS) 판정.</summary>
        private bool CheckLookEnemy(Human t, float fovH, float fovV, float maxDist)
        {
            if (t == null || t == m_self || !t.Alive) return false;
            if (t.Team == m_self.Team) return false; // 같은 팀 = 아군

            Vector3 myEye = EyePos(m_self);
            Vector3 d = EyePos(t) - myEye;
            float dist = d.magnitude;
            if (dist > maxDist || dist < 1e-4f) return false;

            float tYaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float tPitch = -Mathf.Asin(Mathf.Clamp(d.y / dist, -1f, 1f)) * Mathf.Rad2Deg;

            // 수평 FOV 중심 = 몸 yaw. 세로 FOV 중심 = 항상 수평(0), 조준 pitch(m_pitch) 와 무관 — 원본 CheckLookEnemy(ai.cpp:1422)
            // 가 CheckTargetAngle 의 기준 ry 에 0.0f 를 하드코딩. armrotation_y(팔/발사 pitch)는 탐색 세로 시야에 안 쓰임.
            if (Mathf.Abs(Mathf.DeltaAngle(m_yaw, tYaw)) > fovH * 0.5f) return false;
            if (Mathf.Abs(tPitch) > fovV * 0.5f) return false;

            return HasLineOfSight(myEye, EyePos(t));
        }

        /// <summary>두 지점 사이를 블록이 가리는지. 원본 CheckALLBlockIntersectRay 대응 (블록 콜라이더 직접 레이 검사).</summary>
        private bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            float dist = dir.magnitude;
            if (dist < 1e-4f) return true;
            dir /= dist;

            IReadOnlyList<Block> blocks = MapLoader.BlockColliders;
            if (blocks == null) return true;
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] != null && blocks[i].IntersectRay(from, dir, dist, out _, out _))
                    return false;
            }
            return true;
        }
    }
}
