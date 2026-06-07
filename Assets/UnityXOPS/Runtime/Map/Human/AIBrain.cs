using System.Collections.Generic;
using JJLUtility;
using JJLUtility.IO;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// Human 1명의 AI 두뇌 (원본 OpenXOPS AIcontrol — HumanAI[] 배열의 한 슬롯). 상태머신/탐색/조준/사격 판단.
    /// MonoBehaviour 가 아니라 AIController(씬 매니저)가 비플레이어 Human 마다 하나씩 생성·보유한다.
    /// 실제 이동·회전 적용은 대상 Human 의 HumanController(= 원본 AIObjectDriver) API 로 주입.
    ///
    /// 구현 범위: 상태머신(NORMAL/CAUTION/ACTION/DEAD) + 타겟 탐색 + 조준 보간 + 사격 + 경로 이동 + 회피 + 좀비 근접.
    /// 수류탄 궤도 조준·Run2 스트레이핑 돌격은 다음 단계.
    /// Tick() 한 번 = 원본 1프레임(33.333fps). 프레임 락 누산은 AIController 가 담당.
    /// </summary>
    public class AIBrain
    {
        private enum BattleMode { Dead, Action, Caution, Normal }

        private readonly Human           m_self;
        private readonly HumanController m_controller;

        private BattleMode m_mode = BattleMode.Normal;
        private Human      m_enemy;
        private int        m_cautionCnt;
        private int        m_actionCnt;
        private bool       m_longAttack;

        // 조준 회전 적분 상태 (원본 AIObjectDriver: rx/ry + addrx/addry). deg 단위.
        private float m_yaw, m_pitch;
        private float m_addYaw, m_addPitch;

        // 이번 프레임의 회전 의사(플래그). ApplyTurn 에서 각속도로 변환. 매 틱 reset 후 상태별 로직이 set.
        private bool m_turnLeft, m_turnRight, m_turnUp, m_turnDown;

        // TurnSeen 두리번거림 지속 플래그 (원본 moveturn_mode 비트처럼 프레임 간 유지). 매 틱 m_turn* 으로 먹임.
        private bool m_scanLeft, m_scanRight;

        // 전투 회피 이동 — 지속 플래그(원본 moveturn_mode). MoveRandom 이 낮은 확률로 set, CombatMoveCancel 이 높은 확률로 del → 짧은 스트레이핑.
        // ACTION 에서만 유효, 비전투 진입 시 None 으로 클리어. (per-frame 인 m_moveIntent[경로]와 별개)
        private HumanMoveFlag m_combatMove;

        // 피격 방향 조준 (원본 FaceCaution). 피격 시에만 set, TurnSeen 이 그 방향으로 ±2.5° 회전 후 해제.
        private bool  m_faceCaution;
        private float m_faceCautionYaw;

        // 경로 추종 (기본 순찰). 결정은 33fps AIFrame, 이동 플래그 적용은 매 FixedUpdate(ApplyMovement) — 케이던스 보정.
        private readonly AIMoveNavi m_nav = new AIMoveNavi();
        private HumanMoveFlag       m_moveIntent;     // 지속 이동 의사 (Forward/Walk). 매 AIFrame 재계산, 매 FixedUpdate 적용.
        private bool                m_jumpRequested;  // 점프 1회 요청 (one-shot).
        private int                 m_waitCnt;        // STOP_5SEC 대기 카운터.
        private Vector3             m_lastFramePos;   // 끼임 탈출용 — 직전 AIFrame 위치.

        // MoveTarget 전진 허용 각도 (원본 ai.cpp:176-205). 이 안으로 정렬돼야 전진.
        private const float k_forwardTolWalk = 6f;
        private const float k_forwardTolRun  = 50f;
        private const float k_turnTowardDeg  = 0.5f;   // 목표 방향 선회 임계 (원본 ai.cpp:168-173)
        private const int   k_jumpChance     = 16;     // 1/16 프레임 점프 시도 (원본 ai.cpp:208)
        private const int   k_stuckChance    = 28;     // 1/28 끼임 탈출 시도 (원본 ai.cpp:213)
        private const float k_stuckMoveSqr   = 0.0001f; // 직전 프레임 이동 < 0.01 이면 끼임 (원본 0.1 units ×0.1)
        private const float k_trackForwardTol = 20f;   // Tracking 전진 허용각 (원본 ai.cpp:196-205)
        private const float k_combatProbeDist = 0.3f;  // 회피 벽/낭떠러지 전방 거리 (원본 HUMAN_MAPCOLLISION_R 2.8 ×0.1)
        private const float k_cliffProbeDown  = 0.5f;  // 낭떠러지 판정 하향 레이 길이

        // === 좀비 근접 공격 (원본 ai.cpp:559-838 / objectmanager.cpp:2405-2528) — 거리 ×0.1 ===
        private const float k_zombiePathTol      = 20f;     // 경로 추종 전진 허용각 (원본 ai.cpp:178)
        private const float k_zombieApproachTol  = 25f;     // 전투 접근(걷기) 허용각 (원본 ai.cpp:724)
        private const float k_zombieChargeTol    = 15f;     // 돌격(달리기) 허용각 (원본 ai.cpp:730)
        private const float k_zombieChargeDist   = 2.4f;    // 돌격 거리 임계 (원본 24.0)
        private const int   k_zombieAttackPeriod = 50;      // 공격 판정/돌격 주기 프레임 (원본 actioncnt%50)
        private const int   k_zombieChargeWindow = 20;      // actioncnt%50 > 20 일 때만 돌격 (원본 ai.cpp:733)
        private const float k_zombieAttackOffset = 0.2f;    // 공격 지점 정면 오프셋 (원본 2.0)
        private const float k_zombieAttackRadius = 0.33f;   // 공격 수평 반경 (원본 3.3)
        private const float k_zombieGrabDist     = 0.9f;    // 끌어당김 거리 (원본 9.0)
        private const float k_zombieGrabHeight   = 1.0f;    // 끌어당김 높이차 한계 (원본 10.0)
        private const float k_zombiePullSpeed    = 1.6667f; // 끌어당김 속도 m/s (원본 0.5/frame ×0.1×33.333)
        private const float k_zombieViewShake    = 2f;      // 피해자 시점 흔들기 ±deg/frame (원본 ±2°)
        private const float k_zombieHitReaction  = 10f;     // 피격 조준 오차 (원본 ReactionGunsightErrorRange 10)
        private const float k_zombieAttackVolume = 1f;      // 공격음 볼륨

        public Human Self => m_self;

        public AIBrain(Human self)
        {
            m_self       = self;
            m_controller = self.GetComponent<HumanController>();
            if (m_controller != null)
            {
                m_yaw   = m_controller.Yaw;
                m_pitch = m_controller.Pitch;
            }
            m_lastFramePos = self.transform.position;
            m_nav.Init(self.PathStartId);
        }

        /// <summary>원본 AIcontrol::Process (ai.cpp:1934) 1프레임. 상태 전이 → 상태별 처리 → 회전 적분 적용.</summary>
        public void Tick()
        {
            if (m_self == null || m_controller == null) return;

            if (!m_self.Alive)
            {
                m_mode  = BattleMode.Dead;
                m_enemy = null;
                return;
            }
            if (m_mode == BattleMode.Dead) m_mode = BattleMode.Normal; // 부활 복귀

            // 외부(발사 반동 AddViewRecoil 등)가 시점을 바꿨을 수 있으니 컨트롤러 값과 재동기화.
            m_yaw   = m_controller.Yaw;
            m_pitch = m_controller.Pitch;

            m_turnLeft = m_turnRight = m_turnUp = m_turnDown = false;
            m_moveIntent = HumanMoveFlag.None; // 이동 의사는 매 틱 재계산 — MovePath 만 set. ACTION/CAUTION 은 제자리.

            // 위협 소리(총성/총알 통과/폭발)·피격 — 상태와 무관하게 매 틱 소비(ACTION 에서도 비워 잔존 방지). NORMAL/CAUTION 만 반응.
            bool heard = m_self.ConsumeThreatHeard();
            bool hit   = m_self.ConsumeHit(out float faceYaw);
            if (hit) m_faceCautionYaw = faceYaw; // 피격 방향은 항상 갱신, FaceCaution 발동은 NORMAL/CAUTION 에서만

            // 무기 들기/교체 (원본 HaveWeapon — ACTION/CAUTION 만, ai.cpp:1964). 빈손/소진 슬롯이면 탄 있는 다른 슬롯으로.
            if (m_mode == BattleMode.Action || m_mode == BattleMode.Caution) HaveWeapon();

            switch (m_mode)
            {
                case BattleMode.Action:  ActionMain();            break;
                case BattleMode.Caution: CautionMain(heard, hit); break;
                default:                 NormalMain(heard, hit);  break;
            }

            ApplyTurn();
            ControlWeapon(); // 탄창 빔 → 재장전/전환/버림 (원본 ControlWeapon, 매 프레임, ai.cpp:1984)

            // 비무장 팔 동적화 — ACTION 포즈(좀비 공격/항복)일 때만 dynamicArmRoot 로. 평상시/경계는 fixed(데이터대로).
            m_self.SetUnarmedArmDynamic(m_mode == BattleMode.Action && IsNoneWeapon(m_self.CurrentWeapon));

            m_lastFramePos = m_self.transform.position; // 끼임 탈출 판정용
        }

        /// <summary>
        /// 매 FixedUpdate(50fps) 호출 — 33fps AIFrame 에서 결정한 이동 의사를 물리 스텝마다 적용. AIController 가 호출.
        /// 회전(yaw/pitch)은 SetYawPitch 절대값이라 프레임 간 유지되지만, 이동 플래그는 HumanController 가 매 스텝 소비/클리어하므로 재적용 필요.
        /// </summary>
        public void ApplyMovement()
        {
            if (m_self == null || m_controller == null || !m_self.Alive) return;

            // 경로 이동(per-frame)과 전투 회피(persistent)는 상태 배타적이라 OR 로 합쳐 적용.
            HumanMoveFlag move = m_moveIntent | m_combatMove;
            if (move != HumanMoveFlag.None) m_controller.SetMoveFlag(move);

            if (m_jumpRequested)
            {
                m_controller.SetMoveFlag(HumanMoveFlag.Jump);
                m_jumpRequested = false;
            }
        }

        // === 상태별 메인 =========================================================

        /// <summary>원본 NormalMain (ai.cpp:1673). 적 발견·소리·피격 시 경계 진입(피격이면 그 방향 조준). 그 외엔 경로(MovePath) 순찰.</summary>
        private void NormalMain(bool heard, bool hit)
        {
            m_combatMove = HumanMoveFlag.None; // 비전투 — 전투 회피 플래그 잔존 방지

            if (SearchEnemy() != 0 || heard || hit)
            {
                // 경계 대기(WaitAlert): 그 지점에 도착해 대기 중일 때만, 경계(이상) 진입 순간 다음 경로로 진행.
                // (도착 전 이동 중 경계 들어가면 advance 안 함 → 교전 후 그 WaitAlert 지점까지 가서 대기)
                if (m_nav.Mode == AIPathMode.WaitAlert && CheckArrived()) m_nav.Advance();

                m_mode        = BattleMode.Caution;
                m_cautionCnt  = GeneralData.aiCautionFrames;
                if (hit) m_faceCaution = true; // 피격만 방향 조준 (소리/시야 발견은 방향 없음, 원본 ai.cpp:1727-1745)
                return;
            }

            MovePath();
        }

        // === 경로 추종 (원본 MovePath ai.cpp:1489 + MoveTarget ai.cpp:133) =========

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
                case AIPathMode.Wait:      // 무한 대기 — 제자리 두리번 (ChangeToWalk 이벤트로만 해제)
                case AIPathMode.WaitAlert: // 경계 대기 — 대기 중엔 Wait 과 동일, 단 경계(이상) 진입 시 다음 경로로 진행(NormalMain 에서 처리)
                case AIPathMode.Tracking:  // 추적 대상에 도착(1.8 이내) → 제자리 두리번. 대상이 움직이면 다시 추격.
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
                ? GeneralData.aiArrivalDistTracking   // 추적은 더 멀리서 "도착"(18→1.8)
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
            float atan       = Mathf.DeltaAngle(m_yaw, desiredYaw);

            // 선회 (Action 조준과 동일 부호 규약: DeltaAngle>0 → 우회전)
            if (atan >  k_turnTowardDeg) m_turnRight = true;
            if (atan < -k_turnTowardDeg) m_turnLeft  = true;

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
            if (tr >  2.5f) { m_turnRight = true; return false; }
            if (tr < -2.5f) { m_turnLeft  = true; return false; }
            return true;
        }

        /// <summary>진행 방향 앞 허리 높이에 블록이 있는지 — 점프 트리거. 원본 MoveJump (ai.cpp:504) 의 장애물 감지 단순화.</summary>
        private bool JumpBlocked()
        {
            HumanGeneralData gen = GeneralData;
            Vector3 fwd    = Quaternion.Euler(0f, m_yaw, 0f) * Vector3.forward;
            Vector3 origin = m_self.transform.position + Vector3.up * (gen.controllerHeight * 0.5f);
            float   dist   = gen.aiJumpCheckDist + gen.controllerRadiusControllerToMap;
            return Physics.Raycast(origin, fwd, dist, MapLoader.BlockLayerMask);
        }

        /// <summary>원본 CautionMain (ai.cpp:1587). 적 확정 시 전투 진입, 소리 들으면 경계 연장, 카운트다운 종료 시 평상시 복귀. 매 프레임 두리번(TurnSeen)+팔 각도(ArmAngle).</summary>
        private void CautionMain(bool heard, bool hit)
        {
            m_combatMove = HumanMoveFlag.None; // 비전투 — 전투 회피 플래그 잔존 방지

            if (m_enemy != null || SearchEnemy() != 0)
            {
                m_mode      = BattleMode.Action;
                m_actionCnt = 0;
                m_scanLeft  = m_scanRight = false; // 전투 진입 — 스캔 플래그 정리
                return;
            }

            if (hit)
            {
                m_cautionCnt   = GeneralData.aiCautionFrames; // 피격 → 경계 재시작 + 그 방향 조준
                m_faceCaution  = true;
            }
            else if (heard)
            {
                m_cautionCnt = GeneralData.aiCautionFrames; // 원본 soundlists>0 → cautioncnt=160 경계 재시작 (방향 없음)
            }
            else if (m_cautionCnt <= 0)
            {
                m_mode        = BattleMode.Normal;
                m_faceCaution = false; // 경계 종료 — 방향 조준 해제
                return;
            }
            else
            {
                m_cautionCnt--;
            }

            TurnSeen();
            ArmAngle();
        }

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
                if (tr >  2.5f) m_turnRight = true;
                if (tr < -2.5f) m_turnLeft  = true;
                if (Mathf.Abs(tr) <= 2.5f) m_faceCaution = false;
                return;
            }

            int turnStart = (m_mode == BattleMode.Caution) ? 20 : 85; // 경계가 평상시보다 훨씬 자주 둘러봄 (원본 20 vs 85)
            int turnStop  = (m_mode == BattleMode.Caution) ? 20 : 18;

            if (GetRand(turnStart) == 0) m_scanRight = true;
            if (GetRand(turnStart) == 0) m_scanLeft  = true;
            if (GetRand(turnStop)  == 0) m_scanRight = false;
            if (GetRand(turnStop)  == 0) m_scanLeft  = false;

            if (m_scanRight) m_turnRight = true;
            if (m_scanLeft)  m_turnLeft  = true;
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
                if (m_pitch >  1f) m_turnUp   = true;
                if (m_pitch < -1f) m_turnDown = true;
            }
            else
            {
                // 평상시 무기 소지 — pitch ≈ -armAngleInitial(살짝 아래) ±2° 유지. 원본 arm ry -28~-32.
                float rest = -GeneralData.armAngleInitial;
                if (m_pitch > rest + 2f) m_turnUp   = true;
                if (m_pitch < rest - 2f) m_turnDown = true;
            }
        }

        /// <summary>원본 ActionMain (ai.cpp:1527). 조준·사격 + 회피 이동. 종료 조건이면 경계로 복귀.</summary>
        private void ActionMain()
        {
            if (m_enemy == null || ActionCancel())
            {
                m_enemy      = null;
                m_mode       = BattleMode.Caution;
                m_cautionCnt = GeneralData.aiCautionFrames;
                m_combatMove = HumanMoveFlag.None; // 전투 종료 — 회피 이동 정리
                return;
            }

            // 원본 Process 순서: CancelMoveTurn(이전 프레임 이동 플래그 확률적 해제) → Action(조준/사격) → MoveRandom(이동 set).
            CombatMoveCancel();
            Action();
            // 비좀비 무장 AI 만 회피 스트레이핑. 좀비(돌격)·맨손(항복+후퇴는 Action 내 처리)은 제외.
            if (!IsZombie() && !IsNoneWeapon(m_self.CurrentWeapon)) MoveRandom();
            m_actionCnt++;
        }

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
            Vector3 d     = EyePos(m_enemy) - myEye;
            float   dist  = d.magnitude;
            if (dist < 1e-4f) return;

            m_longAttack = dist > GeneralData.aiShortAttackDist;

            float desiredYaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float atanx      = Mathf.DeltaAngle(m_yaw, desiredYaw); // 좌우 오차
            float dz         = GeneralData.aiTurnDeadzoneDeg;

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
                if (atanx >  dz) m_turnRight = true;
                if (atanx < -dz) m_turnLeft  = true;

                if (!IsNoneWeapon(m_enemy.CurrentWeapon)) m_turnUp   = true; // 손들기
                else                                      m_turnDown = true;

                if (GetRand(80) == 0) m_combatMove |= HumanMoveFlag.Back;    // 1/80 후퇴 (원본 ai.cpp:709-717). CombatMoveCancel 이 해제.
                return;
            }

            HumanAIData ai = AiLevel();
            if (ai == null) return;

            float desiredPitch = -Mathf.Asin(Mathf.Clamp(d.y / dist, -1f, 1f)) * Mathf.Rad2Deg;
            float atany        = Mathf.DeltaAngle(m_pitch, desiredPitch); // 상하 오차 (+ = 더 아래로 조준해야 함)

            // 조준 빈도 게이트 — aiming 이 0 이면 회전 안 함 (원본 randr=aiming). 보통 ≥1 이라 매 프레임 보정.
            if (ai.aiming != 0)
            {
                if (atanx >  dz) m_turnRight = true;
                if (atanx < -dz) m_turnLeft  = true;
                if (atany >  dz) m_turnDown  = true;
                if (atany < -dz) m_turnUp    = true;
            }

            // 발사 판정 각도 = 스코프 기본각 ± limitsError 보정. 합산 오차가 이 안이면 발사 후보.
            HumanAIScopeData sc = ScopeAi(w);
            float shotAngle = m_longAttack ? sc.aiShotAngleLong : sc.aiShotAngle;
            shotAngle += (m_longAttack ? 0.2f : 0.5f) * ai.limitsError;
            if (shotAngle < 0f) shotAngle = 0f;

            if (Mathf.Abs(atanx) + Mathf.Abs(atany) < shotAngle)
            {
                int atk = ai.attack + (m_longAttack ? 1 : 0);
                if (GetRand(atk) == 0)
                    w.Shoot(m_self); // AimPoint 미설정 → SpawnBullets 가 controller.Yaw/Pitch 방향으로 발사
            }
        }

        // === 전투 회피 이동 (원본 MoveRandom ai.cpp:274 / CancelMoveTurn ai.cpp:947) ====

        /// <summary>전투 이동 지속 플래그를 확률적으로 해제 (원본 CancelMoveTurn 의 MOVE 부분, ACTION). 근/원거리 다름.</summary>
        private void CombatMoveCancel()
        {
            int fwd  = m_longAttack ? 5 : 6;
            int back = m_longAttack ? 4 : 6;
            int side = m_longAttack ? 5 : 7;
            if (GetRand(fwd)  == 0) m_combatMove &= ~HumanMoveFlag.Forward;
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
            if (!m_longAttack) { fwd =  80; back =  90; side =  70; } // 근거리 — 자주 움직임
            else               { fwd = 120; back = 150; side = 130; } // 원거리 — 덜 움직임

            if (GetRand(fwd)  == 0) m_combatMove |= HumanMoveFlag.Forward;
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

        /// <summary>한 축(전후 또는 좌우)을 골라 진행 방향에 벽/낭떠러지가 있으면 반대 방향으로 전환. 원본 ai.cpp:312-366.</summary>
        private void WallCliffAvoid()
        {
            Quaternion yawQ  = Quaternion.Euler(0f, m_yaw, 0f);
            Vector3    fwd   = yawQ * Vector3.forward;
            Vector3    right = yawQ * Vector3.right;

            if (Random.Range(0, 2) == 0)
            {
                if      (BlockedOrCliff(fwd))  { m_combatMove &= ~HumanMoveFlag.Forward; m_combatMove |= HumanMoveFlag.Back;    }
                else if (BlockedOrCliff(-fwd)) { m_combatMove &= ~HumanMoveFlag.Back;    m_combatMove |= HumanMoveFlag.Forward; }
            }
            else
            {
                if      (BlockedOrCliff(right))  { m_combatMove &= ~HumanMoveFlag.Right; m_combatMove |= HumanMoveFlag.Left;  }
                else if (BlockedOrCliff(-right)) { m_combatMove &= ~HumanMoveFlag.Left;  m_combatMove |= HumanMoveFlag.Right; }
            }
        }

        /// <summary>방향 dir 로 허리높이 벽이 있거나(충돌) 앞쪽 발밑에 바닥이 없으면(낭떠러지) true. 원본 MoveRandom 의 벽/공허 레이.</summary>
        private bool BlockedOrCliff(Vector3 dir)
        {
            HumanGeneralData gen = GeneralData;
            Vector3 pos   = m_self.transform.position;
            float   probe = gen.controllerRadiusControllerToMap + k_combatProbeDist;

            Vector3 waist = pos + Vector3.up * (gen.controllerHeight * 0.5f);
            if (Physics.Raycast(waist, dir, probe, MapLoader.BlockLayerMask)) return true; // 벽

            Vector3 ahead = pos + dir * probe + Vector3.up * 0.1f;
            if (!Physics.Raycast(ahead, Vector3.down, k_cliffProbeDown, MapLoader.BlockLayerMask)) return true; // 낭떠러지

            return false;
        }

        private bool IsZombie()
        {
            HumanTypeData t = m_self.HumanTypeData;
            return t != null && t.zombie;
        }

        // 좀비 할퀴기 팔 목표 pitch (m_pitch space). 데이터 aiZombieArmAngle 는 원본 space(음수=아래, armAngleInitial 과 동일 규약).
        // 부호 반전해서 m_pitch 로. 값을 키울수록(원본 space) 팔이 더 위로 올라감.
        private float ZombieArmPitch => -GeneralData.aiZombieArmAngle;

        // === 좀비 근접 전투 (원본 Action 좀비 분기 ai.cpp:559-838) =====================

        /// <summary>
        /// 좀비 전투 — 적을 향해 선회, 할퀴기 팔 자세, 근접 끌어당김, 정면 돌격, 50프레임 주기 근접 공격.
        /// 총 발사 없음(맨손). 회피 스트레이핑(MoveRandom)도 안 함.
        /// </summary>
        private void ZombieFight(Vector3 d, float dist, float atanx, float dz)
        {
            // 적 방향으로 선회
            if (atanx >  dz) m_turnRight = true;
            if (atanx < -dz) m_turnLeft  = true;

            // 팔(조준) 각도 — 할퀴기 자세로 수렴 (데이터 aiZombieArmAngle, 원본 AI_ZOMBIEATTACK_ARMRY -15°). ±1° 데드밴드.
            float armTarget = ZombieArmPitch;
            if (m_pitch > armTarget + 1f) m_turnUp   = true;
            if (m_pitch < armTarget - 1f) m_turnDown = true;

            // 끌어당김(引き付け) — 근접 시 적을 끌고 + 적 시점 흔들기.
            ZombieGrab(d, dist);

            // 돌격 — 정면 ±25° 안에서 접근(걷기). ±15° + 근접 + 주기조건이면 달려듦(걷기 플래그 제거 = 달리기).
            if (Mathf.Abs(atanx) < k_zombieApproachTol)
            {
                m_moveIntent |= HumanMoveFlag.Forward;
                bool charge = Mathf.Abs(atanx) < k_zombieChargeTol
                           && dist < k_zombieChargeDist
                           && (m_actionCnt % k_zombieAttackPeriod) > k_zombieChargeWindow;
                if (!charge) m_moveIntent |= HumanMoveFlag.Walk;
            }

            // 공격 판정 — 50프레임마다 (원본 actioncnt%50==0).
            if (m_actionCnt % k_zombieAttackPeriod == 0) ZombieMeleeAttack();
        }

        /// <summary>근접(거리/높이차 임계) 시 적을 좀비 쪽으로 끌어당기고 적 시점을 ±2°/프레임 흔든다. 원본 ai.cpp:748-769.</summary>
        private void ZombieGrab(Vector3 d, float dist)
        {
            if (m_enemy == null) return;
            if (dist >= k_zombieGrabDist) return;
            if (Mathf.Abs(d.y) >= k_zombieGrabHeight) return;

            // 적을 좀비 쪽으로 끌어당김 (원본 AddPosOrder 0.5/frame). 수평만. 무기 넉백과 동일 속도 경로.
            Vector3 pull = m_self.transform.position - m_enemy.transform.position;
            pull.y = 0f;
            if (pull.sqrMagnitude > 1e-6f)
            {
                HumanController vc = m_enemy.GetComponent<HumanController>();
                if (vc != null) vc.AddKnockbackVector(pull.normalized, k_zombiePullSpeed);
            }

            // 적 시점 흔들기 — 무기 반동과 동일 경로(AddViewRecoil)라 플레이어/AI 피해자 모두 적용됨.
            m_enemy.AddViewRecoil(Random.Range(-k_zombieViewShake, k_zombieViewShake),
                                  Random.Range(-k_zombieViewShake, k_zombieViewShake));
        }

        /// <summary>정면 공격 지점(반경 0.33) 안의 적에게 근접 데미지. 원본 ObjectManager::CheckZombieAttack (objectmanager.cpp:2416).</summary>
        private void ZombieMeleeAttack()
        {
            HumanGeneralData gen = GeneralData;
            float eyeH  = gen.cameraAttachPosition; // 원본 VIEW_HEIGHT 19 → 눈높이
            float bandH = gen.controllerHeight;     // 원본 HUMAN_HEIGHT 20 → 수직 밴드 폭

            Vector3 self = m_self.transform.position;
            Vector3 fwd  = Quaternion.Euler(0f, m_yaw, 0f) * Vector3.forward;
            // 공격 지점 = 정면 0.2 앞 (수평), 높이 = 발 + 2·눈높이 - 0.05 (원본 (feet+VIEW)+VIEW-0.5).
            Vector3 ap  = self + fwd * k_zombieAttackOffset;
            float   apY = self.y + eyeH + eyeH - 0.05f;
            float   r2  = k_zombieAttackRadius * k_zombieAttackRadius;

            var humans = MapLoader.Humans;
            if (humans == null) return;
            for (int i = 0; i < humans.Count; i++)
            {
                Human t = humans[i];
                if (t == null || t == m_self || !t.Alive) continue;
                if (t.Team == m_self.Team) continue;

                Vector3 tp = t.transform.position;
                float ax = ap.x - tp.x;
                float az = ap.z - tp.z;
                if (ax * ax + az * az >= r2) continue;     // 수평 반경 밖

                float ty = tp.y + eyeH;                      // 피해자 눈높이
                if (apY < ty || apY > ty + bandH) continue;  // 수직 범위 밖

                ZombieHit(t);
            }
        }

        /// <summary>좀비 근접 명중 — 데미지 + 피격방향 + 조준오차 + 피 이펙트 + 공격음. 원본 HitZombieAttack (objectmanager.cpp:2451).</summary>
        private void ZombieHit(Human victim)
        {
            HumanTypeData atkType = m_self.HumanTypeData;
            IntRange dmg = atkType.zombieMeleeDamageRange;
            int dealt = Random.Range(dmg.min, dmg.max); // 원본 15+GetRand(5) = 15~19 (max exclusive)
            victim.ApplyDamage(dealt);

            // 피격 방향 (좀비→피해자, 총알 진행방향 규약) — 사망 쓰러짐 방향용.
            Vector3 dir = victim.transform.position - m_self.transform.position;
            victim.SetHitYaw(Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg);

            // 피격 조준 오차 (원본 ReactionGunsightErrorRange = 10).
            victim.SetHitReaction(k_zombieHitReaction);

            // 피 이펙트 (피해자 타입 blood, 데이터) — 좀비 피격 flowing=true 라 데미지 비례 분사 포함(원본 objectmanager.cpp:2515).
            HumanTypeData vt = victim.HumanTypeData;
            if (vt != null)
                EffectManager.Instance.Play(vt.bloodEffectIndex,
                    victim.transform.position + Vector3.up * GeneralData.cameraAttachPosition, dealt);

            // 공격음 (원본은 일반 피탄음 재사용). 데이터 경로.
            if (!string.IsNullOrEmpty(atkType.zombieAttackSound))
            {
                AudioClip clip = SoundLoader.LoadAudio(
                    SafePath.Combine(Application.streamingAssetsPath, atkType.zombieAttackSound));
                if (clip != null) SoundManager.Instance.PlayAt(clip, victim.transform.position, k_zombieAttackVolume);
            }

            // 좀비 타격음 AI 인지 (원본 HIT_HUMAN_ZOMBIE, 팀 무관 → enemyDist==allyDist). 근처 AI 경계 트리거.
            float zd = GeneralData.aiHearHitHumanZombie;
            WorldSound.EmitPointSound(victim.transform.position, m_self.Team, zd, zd);
        }

        // === 회전 적분 (원본 AIObjectDriver::ControlObject ai.cpp:2316) ============
        private void ApplyTurn()
        {
            float rate = GeneralData.aiTurnRateDeg;
            if (m_turnRight) m_addYaw   += rate;
            if (m_turnLeft)  m_addYaw   -= rate;
            if (m_turnDown)  m_addPitch += rate;
            if (m_turnUp)    m_addPitch -= rate;

            m_yaw   += m_addYaw;
            m_pitch += m_addPitch;

            float maxPitch = GeneralData.aiTurnMaxPitchDeg;
            m_pitch = Mathf.Clamp(m_pitch, -maxPitch, maxPitch);

            float damp = GeneralData.aiTurnDamping;
            m_addYaw   *= damp;
            m_addPitch *= damp;

            float dz = GeneralData.aiTurnDeadzoneDeg;
            if (Mathf.Abs(m_addYaw)   < dz) m_addYaw   = 0f;
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

            HumanGeneralData gen     = GeneralData;
            bool             caution = m_mode == BattleMode.Caution;
            HumanAIScopeData sc      = ScopeAi(m_self.CurrentWeapon);

            int loops = ai.search * gen.aiSearchLoopScale;
            if (caution) loops += gen.aiSearchLoopScale; // 경계 시 탐색 강화

            float baseDist = caution ? gen.aiSearchDistBaseCaution  : gen.aiSearchDistBaseNormal;
            float coeff    = caution ? gen.aiSearchDistCoeffCaution  : gen.aiSearchDistCoeffNormal;
            float addDist  = caution ? sc.aiAddSearchDistCaution     : sc.aiAddSearchDistNormal;
            float maxDist  = baseDist + coeff * (ai.search - 2) + addDist;

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
                    m_enemy      = t;
                    m_longAttack = false;
                    return 1;
                }
                if (CheckLookEnemy(t, bH, bV, maxDist))
                {
                    m_enemy      = t;
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
            if (t.Team == m_self.Team)               return false; // 같은 팀 = 아군

            Vector3 myEye = EyePos(m_self);
            Vector3 d     = EyePos(t) - myEye;
            float   dist  = d.magnitude;
            if (dist > maxDist || dist < 1e-4f) return false;

            float tYaw   =  Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float tPitch = -Mathf.Asin(Mathf.Clamp(d.y / dist, -1f, 1f)) * Mathf.Rad2Deg;

            // 수평 FOV 중심 = 몸 yaw. 세로 FOV 중심 = 항상 수평(0), 조준 pitch(m_pitch) 와 무관 — 원본 CheckLookEnemy(ai.cpp:1422)
            // 가 CheckTargetAngle 의 기준 ry 에 0.0f 를 하드코딩. armrotation_y(팔/발사 pitch)는 탐색 세로 시야에 안 쓰임.
            if (Mathf.Abs(Mathf.DeltaAngle(m_yaw, tYaw)) > fovH * 0.5f) return false;
            if (Mathf.Abs(tPitch)                        > fovV * 0.5f) return false;

            return HasLineOfSight(myEye, EyePos(t));
        }

        /// <summary>두 지점 사이를 블록이 가리는지. 원본 CheckALLBlockIntersectRay 대응 (블록 콜라이더 직접 레이 검사).</summary>
        private bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            Vector3 dir  = to - from;
            float   dist = dir.magnitude;
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

        // === 무기 운용 (원본 HaveWeapon ai.cpp:899 / ControlWeapon ai.cpp:1033) ====

        /// <summary>
        /// 현재 슬롯이 맨손이거나 총탄 소진이면, 탄 있는 총이 든 다른 슬롯으로 전환. ACTION/CAUTION 에서만 호출.
        /// 원본 HaveWeapon — 두 슬롯 다 비면 아무것도 못 함(맨손 유지). SetSelectWeapon 이 전환 쿨다운(IsChanging) 가드.
        /// </summary>
        private void HaveWeapon()
        {
            Weapon cur = m_self.CurrentWeapon;
            if (!IsNoneWeapon(cur) && TotalAmmo(cur) > 0) return; // 멀쩡한 총 들고 있음

            int    next  = 1 - m_self.SelectWeapon;
            Weapon nextW = m_self.GetWeapon(next);
            if (!IsNoneWeapon(nextW) && TotalAmmo(nextW) > 0)
                m_self.SetSelectWeapon(next);
        }

        /// <summary>
        /// 현재 탄창이 비면(CurrentMagazine==0) 상태별 확률로 재장전 / 다른 슬롯 전환 / (예비탄도 0이면) 버림.
        /// 원본 ControlWeapon — 2단계 확률 게이트. 매 프레임 호출. 맨손/전환·재장전 중이면 no-op.
        /// </summary>
        private void ControlWeapon()
        {
            Weapon cur = m_self.CurrentWeapon;
            if (IsNoneWeapon(cur) || cur.IsFalling) return; // 맨손/낙하 무기는 처리 안 함 (원본 weapon NULL → return)
            if (IsCaseWeapon(cur)) return;                  // 케이스(서류가방 등 임무 아이템)는 탄약 0이어도 재장전/전환/버림 안 함
            if (m_self.IsChanging) return;                  // 전환/재장전 쿨다운 중 (원본 selectweaponcnt/weaponreloadcnt 가드)
            if (cur.CurrentMagazine > 0) return;            // 탄창에 탄 있음 (원본 lnbs>0)

            // 1단계: 행동 시도 게이트 (NORMAL 1/1, CAUTION 1/10, ACTION 1/8).
            int gate = (m_mode == BattleMode.Normal) ? 1 : (m_mode == BattleMode.Caution) ? 10 : 8;
            if (GetRand(gate) != 0) return;

            // 예비탄도 0 → 빈 총 버림 (원본 nbs==0 → DumpWeapon). 다음 프레임 HaveWeapon 이 다른 슬롯 픽업.
            if (cur.ReserveAmmo == 0)
            {
                m_self.DropCurrentWeapon();
                return;
            }

            // 2단계: 재장전 vs 전환 확률. under/ways 로 리로드 확률 = (under+1)/ways.
            int ways, under;
            if (m_mode == BattleMode.Normal)       { ways = 1; under = 0; } // 항상 재장전
            else if (m_mode == BattleMode.Caution) { ways = 5; under = 3; } // 4/5 재장전
            else if (!m_longAttack)                { ways = 4; under = 2; } // ACTION 근거리 3/4 재장전
            else                                   { ways = 3; under = 1; } // ACTION 원거리 2/3 재장전

            if (GetRand(ways) <= under) m_self.ReloadCurrentWeapon();
            else                        m_self.SetSelectWeapon(1 - m_self.SelectWeapon);
        }

        /// <summary>맨손(noneWeapon) 슬롯 판정 — WeaponIndex 가 noneWeaponIndex 이거나 무기/데이터 없음.</summary>
        private static bool IsNoneWeapon(Weapon w)
        {
            if (w == null || w.WeaponData == null) return true;
            int noneIdx = DataManager.Instance.WeaponParameterData.weaponGeneralData.noneWeaponIndex;
            return w.WeaponIndex == noneIdx;
        }

        /// <summary>케이스(서류가방 등) 무기 판정 — WeaponGeneralData.caseWeaponIndex 목록에 포함. 탄약 운용/버림 대상에서 제외.</summary>
        private static bool IsCaseWeapon(Weapon w)
        {
            if (w == null || w.WeaponData == null) return false;
            List<int> caseIndices = DataManager.Instance.WeaponParameterData.weaponGeneralData.caseWeaponIndex;
            return caseIndices != null && caseIndices.Contains(w.WeaponIndex);
        }

        /// <summary>무기의 총 보유 탄수 (탄창 + 예비). 원본 nbs.</summary>
        private static int TotalAmmo(Weapon w) => w == null ? 0 : w.CurrentMagazine + w.ReserveAmmo;

        // === 헬퍼 ===============================================================
        private static HumanGeneralData GeneralData => DataManager.Instance.HumanParameterData.humanGeneralData;

        private static Vector3 EyePos(Human h) => h.transform.position + Vector3.up * GeneralData.cameraAttachPosition;

        private HumanAIData AiLevel()
        {
            List<HumanAIData> list = DataManager.Instance.HumanParameterData.humanAIData;
            if (list == null || list.Count == 0) return null;
            return list[Mathf.Clamp(m_self.AILevel, 0, list.Count - 1)];
        }

        /// <summary>무기 스코프 → aiScopeData 인덱스. 비스코프=0, 스코프 무기는 scopeIndex+1 (원본 scopemode 정렬).</summary>
        private static HumanAIScopeData ScopeAi(Weapon w)
        {
            List<HumanAIScopeData> list = GeneralData.aiScopeData;
            int idx = 0;
            if (w != null && w.WeaponData != null && w.WeaponData.scope)
                idx = w.WeaponData.scopeIndex + 1;
            return list[Mathf.Clamp(idx, 0, list.Count - 1)];
        }

        /// <summary>원본 GetRand(window.cpp:363) — rand()%num, num≤0 이면 0.</summary>
        private static int GetRand(int num) => num <= 0 ? 0 : Random.Range(0, num);
    }
}
