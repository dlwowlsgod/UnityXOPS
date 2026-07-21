using System.Collections.Generic;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// Human 1명의 AI 두뇌 (원본 OpenXOPS AIcontrol — HumanAI[] 배열의 한 슬롯). 상태머신/탐색/조준/사격 판단.
    /// MonoBehaviour 가 아니라 AIController(씬 매니저)가 비플레이어 Human 마다 하나씩 생성·보유한다.
    /// 실제 이동·회전 적용은 대상 Human 의 HumanController(= 원본 AIObjectDriver) API 로 주입.
    ///
    /// 구현 범위: 상태머신(NORMAL/CAUTION/ACTION/DEAD) + 타겟 탐색 + 조준 보간 + 사격 + 경로 이동 + 회피 + 좀비 근접 + 지점 수류탄 투척.
    /// Tick() 한 번 = 원본 1프레임(33.333fps). 프레임 락 누산은 AIController 가 담당.
    /// 관심사별 partial 분리: 본 파일(코어/상태머신) + AIBrainNavigation/AIBrainCombat/AIBrainAim/AIBrainZombie/AIBrainWeapon.
    /// </summary>
    public partial class AIBrain
    {
        private enum BattleMode { Dead, Action, Caution, Normal }

        private readonly Human m_self;
        private readonly HumanController m_controller;

        private BattleMode m_mode = BattleMode.Normal;
        private Human m_enemy;
        private int m_cautionCnt;
        private int m_actionCnt;
        private bool m_longAttack;

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
        private bool m_faceCaution;
        private float m_faceCautionYaw;

        // 경로 추종 (기본 순찰). 결정은 33fps AIFrame, 이동 플래그 적용은 매 FixedUpdate(ApplyMovement) — 케이던스 보정.
        private readonly AIMoveNavi m_nav = new AIMoveNavi();
        private HumanMoveFlag m_moveIntent; // 지속 이동 의사 (Forward/Walk). 매 AIFrame 재계산, 매 FixedUpdate 적용.
        private bool m_jumpRequested; // 점프 1회 요청 (one-shot).
        private int m_waitCnt; // STOP_5SEC 대기 카운터.
        private Vector3 m_lastFramePos; // 끼임 탈출용 — 직전 AIFrame 위치.

        // MoveTarget 전진 허용 각도 (원본 ai.cpp:176-205). 이 안으로 정렬돼야 전진.
        private const float k_forwardTolWalk = 6f;
        private const float k_forwardTolRun = 50f;
        private const float k_turnTowardDeg = 0.5f; // 목표 방향 선회 임계 (원본 ai.cpp:168-173)
        private const int k_jumpChance = 16; // 1/16 프레임 점프 시도 (원본 ai.cpp:208)
        private const int k_stuckChance = 28; // 1/28 끼임 탈출 시도 (원본 ai.cpp:213)
        private const float k_stuckMoveSqr = 0.0001f; // 직전 프레임 이동 < 0.01 이면 끼임 (원본 0.1 units ×0.1)
        private const float k_trackForwardTol = 20f; // Tracking 전진 허용각 (원본 ai.cpp:196-205)
        private const float k_grenadeThrowTol = 1.5f; // 수류탄 투척 조준 수렴 임계 — 좌우·상하 각각 이 안이면 투척 (원본 ai.cpp:1247)

        // RUN2(우선적 달리기) 전투 중 전방위 이동(원본 MoveTarget2 ai.cpp:225-272). 목표점 방향에 따라 전진/후진/스트레이프.
        private const float k_run2ForwardTol = 56f; // 목표점이 이 각도 안이면 전진
        private const float k_run2BackTol = 123.5f; // 이 각도 밖(뒤쪽)이면 후진
        private const float k_run2StrafeMin = 33f; // 스트레이프 시작각
        private const float k_run2StrafeMax = 146f; // 스트레이프 종료각
        private const float k_run2ShotAngleScale = 1.5f; // RUN2 발사 허용각 완화 배수 (원본 ai.cpp:797)
        private const float k_combatProbeDist = 0.3f; // 회피 벽/낭떠러지 전방 거리 (원본 HUMAN_MAPCOLLISION_R 2.8 ×0.1)
        private const float k_cliffProbeDown = 0.5f; // 낭떠러지 판정 하향 레이 길이

        // === 좀비 근접 공격 (원본 ai.cpp:559-838 / objectmanager.cpp:2405-2528) — 거리 ×0.1 ===
        private const float k_zombiePathTol = 20f; // 경로 추종 전진 허용각 (원본 ai.cpp:178)
        private const float k_zombieApproachTol = 25f; // 전투 접근(걷기) 허용각 (원본 ai.cpp:724)
        private const float k_zombieChargeTol = 15f; // 돌격(달리기) 허용각 (원본 ai.cpp:730)
        private const float k_zombieChargeDist = 2.4f; // 돌격 거리 임계 (원본 24.0)
        private const int k_zombieAttackPeriod = 50; // 공격 판정/돌격 주기 프레임 (원본 actioncnt%50)
        private const int k_zombieChargeWindow = 20; // actioncnt%50 > 20 일 때만 돌격 (원본 ai.cpp:733)
        private const float k_zombieAttackOffset = 0.2f; // 공격 지점 정면 오프셋 (원본 2.0)
        private const float k_zombieAttackRadius = 0.33f; // 공격 수평 반경 (원본 3.3)
        private const float k_zombieGrabDist = 0.9f; // 끌어당김 거리 (원본 9.0)
        private const float k_zombieGrabHeight = 1.0f; // 끌어당김 높이차 한계 (원본 10.0)
        private const float k_zombiePullSpeed = 1.6667f; // 끌어당김 속도 m/s (원본 0.5/frame ×0.1×33.333)
        private const float k_zombieViewShake = 2f; // 피해자 시점 흔들기 ±deg/frame (원본 ±2°)
        private const float k_zombieHitReaction = 10f; // 피격 조준 오차 (원본 ReactionGunsightErrorRange 10)
        private const float k_zombieAttackVolume = 1f; // 공격음 볼륨

        public Human Self => m_self;

        public AIBrain(Human self)
        {
            m_self = self;
            m_controller = self.GetComponent<HumanController>();
            if (m_controller != null)
            {
                m_yaw = m_controller.Yaw;
                m_pitch = m_controller.Pitch;
            }
            m_lastFramePos = self.transform.position;
            m_nav.Init(self.PathStartId);

            // 치트(F9) 복제 클론 — 패스 순찰 대신 따라오기/제자리 경계로 오버라이드.
            switch (self.CloneAI)
            {
                case Human.CloneAIMode.Follow:
                    m_nav.SetHoldTracking(self.CloneFollowTarget);
                    break;
                case Human.CloneAIMode.Guard:
                    m_nav.SetHoldWait(self.transform.position, self.transform.eulerAngles.y);
                    break;
            }
        }

        /// <summary>원본 AIcontrol::Process (ai.cpp:1934) 1프레임. 상태 전이 → 상태별 처리 → 회전 적분 적용.</summary>
        public void Tick()
        {
            if (m_self == null || m_controller == null) return;

            if (!m_self.Alive)
            {
                m_mode = BattleMode.Dead;
                m_enemy = null;
                return;
            }
            if (m_mode == BattleMode.Dead) m_mode = BattleMode.Normal; // 부활 복귀

            // 외부(발사 반동 AddViewRecoil 등)가 시점을 바꿨을 수 있으니 컨트롤러 값과 재동기화.
            m_yaw = m_controller.Yaw;
            m_pitch = m_controller.Pitch;

            m_turnLeft = m_turnRight = m_turnUp = m_turnDown = false;
            m_moveIntent = HumanMoveFlag.None; // 이동 의사는 매 틱 재계산 — MovePath 만 set. ACTION/CAUTION 은 제자리.

            // 위협 소리(총성/총알 통과/폭발)·피격 — 상태와 무관하게 매 틱 소비(ACTION 에서도 비워 잔존 방지). NORMAL/CAUTION 만 반응.
            bool heard = m_self.ConsumeThreatHeard();
            bool hit = m_self.ConsumeHit(out float faceYaw);
            if (hit) m_faceCautionYaw = faceYaw; // 피격 방향은 항상 갱신, FaceCaution 발동은 NORMAL/CAUTION 에서만

            // 무기 들기/교체 (원본 HaveWeapon — ACTION/CAUTION 만, ai.cpp:1964). 빈손/소진 슬롯이면 탄 있는 다른 슬롯으로.
            if (m_mode == BattleMode.Action || m_mode == BattleMode.Caution) HaveWeapon();

            switch (m_mode)
            {
                case BattleMode.Action: ActionMain(); break;
                case BattleMode.Caution: CautionMain(heard, hit); break;
                default: NormalMain(heard, hit); break;
            }

            ApplyTurn();
            ControlWeapon(); // 탄창 빔 → 재장전/전환/버림 (원본 ControlWeapon, 매 프레임, ai.cpp:1984)

            // 비무장 팔 동적화 — ACTION 포즈(좀비 공격/항복)일 때만 dynamicArmRoot 로. 평상시/경계는 fixed(데이터대로).
            m_self.SetUnarmedArmDynamic(m_mode == BattleMode.Action && IsNoneWeapon(m_self.CurrentWeapon));

            m_lastFramePos = m_self.transform.position; // 끼임 탈출 판정용
        }

        /// <summary>
        /// 매 FixedUpdate(50fps) 호출 — 33fps AIFrame 에서 결정한 이동 의사를 물리 스텝마다 적용. AIController 가 호출.
        /// 회전(yaw/pitch)은 절대값이라 프레임 간 유지되지만, 이동 플래그는 HumanController 가 매 스텝 소비/클리어하므로 재적용 필요.
        /// </summary>
        public void ApplyMovement()
        {
            if (m_self == null || m_controller == null || !m_self.Alive) return;

            // 경로 이동(per-frame)과 전투 회피(persistent)는 상태 배타적이라 OR 로 합쳐 적용.
            HumanMoveFlag move = m_moveIntent | m_combatMove;
            if (m_jumpRequested)
            {
                move |= HumanMoveFlag.Jump;
                m_jumpRequested = false;
            }
            if (move == HumanMoveFlag.None) return;

            // 조준은 33fps Tick(ApplyTurn)이 소유 — 여기선 현재 컨트롤러 조준값(발사 반동 포함)을 그대로 실어 이동만 OR 추가한다.
            m_controller.SetInput(new HumanInput { moveFlag = move, yaw = m_controller.Yaw, pitch = m_controller.Pitch });
        }

        // === 상태별 메인 =========================================================

        /// <summary>원본 NormalMain (ai.cpp:1673). 적 발견·소리·피격 시 경계 진입(피격이면 그 방향 조준). 그 외엔 경로(MovePath) 순찰.</summary>
        private void NormalMain(bool heard, bool hit)
        {
            m_combatMove = HumanMoveFlag.None; // 비전투 — 전투 회피 플래그 잔존 방지

            // 수류탄 투척 경로점 — 위협 감지(CAUTION)보다 먼저 독립 처리(원본 ai.cpp:1703). 교전 상황이어도 그 프레임엔
            // 조준/투척을 우선한다. 걷지 않고 그 지점을 조준: 보유+수렴 시 던지고 다음, 미보유면 방향만 향하고 다음, 조준 중이면 유지.
            if (m_nav.Mode == AIPathMode.Grenade)
            {
                if (ThrowGrenade()) m_nav.Advance();
                return;
            }

            // RUN2(우선적 달리기) — 경계(CAUTION)를 건너뜀. 피격/소리/시체 자극은 전부 무시하고, 실제로 적을
            // 봤을 때만 곧바로 전투(ACTION)로 직행한다(원본 NormalMain AI_RUN2 분기 ai.cpp:1714). 그 외엔 경로를 계속 달림.
            if (m_nav.Mode == AIPathMode.Run2)
            {
                if (SearchEnemy() != 0) EnterAction();
                else MovePath();
                return;
            }

            if (SearchEnemy() != 0 || heard || hit)
            {
                // 경계 대기(WaitAlert): 그 지점에 도착해 대기 중일 때만, 경계(이상) 진입 순간 다음 경로로 진행.
                // (도착 전 이동 중 경계 들어가면 advance 안 함 → 교전 후 그 WaitAlert 지점까지 가서 대기)
                if (m_nav.Mode == AIPathMode.WaitAlert && CheckArrived()) m_nav.Advance();

                m_mode = BattleMode.Caution;
                m_cautionCnt = GeneralData.aiCautionFrames;
                if (hit) m_faceCaution = true; // 피격만 방향 조준 (소리/시야 발견은 방향 없음, 원본 ai.cpp:1727-1745)
                return;
            }

            MovePath();
        }

        /// <summary>원본 CautionMain (ai.cpp:1587). 적 확정 시 전투 진입, 소리 들으면 경계 연장, 카운트다운 종료 시 평상시 복귀. 매 프레임 두리번(TurnSeen)+팔 각도(ArmAngle).</summary>
        private void CautionMain(bool heard, bool hit)
        {
            m_combatMove = HumanMoveFlag.None; // 비전투 — 전투 회피 플래그 잔존 방지

            if (m_enemy != null || SearchEnemy() != 0)
            {
                EnterAction();
                return;
            }

            if (hit)
            {
                m_cautionCnt = GeneralData.aiCautionFrames; // 피격 → 경계 재시작 + 그 방향 조준
                m_faceCaution = true;
            }
            else if (heard)
            {
                m_cautionCnt = GeneralData.aiCautionFrames; // 원본 soundlists>0 → cautioncnt=160 경계 재시작 (방향 없음)
            }
            else if (m_cautionCnt <= 0)
            {
                m_mode = BattleMode.Normal;
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

        /// <summary>원본 ActionMain (ai.cpp:1527). 조준·사격 + 회피 이동. 종료 조건이면 경계로 복귀.</summary>
        private void ActionMain()
        {
            bool run2 = m_nav.Mode == AIPathMode.Run2;

            if (m_enemy == null || ActionCancel())
            {
                m_enemy = null;
                m_combatMove = HumanMoveFlag.None; // 전투 종료 — 회피 이동 정리
                if (run2)
                {
                    m_mode = BattleMode.Normal; // RUN2 — 경계 없이 즉시 경로 순찰 재개 (원본 ActionMain GetRun2 ai.cpp:1566)
                }
                else
                {
                    m_mode = BattleMode.Caution;
                    m_cautionCnt = GeneralData.aiCautionFrames;
                }
                return;
            }

            if (run2)
            {
                // RUN2 — 사격(Action)하면서 경로 목표점으로 전방위 이동(MoveTarget2). 도착하면 전투 중에도 다음
                // 포인트로 진행(원본 ActionMain GetRun2 분기 ai.cpp:1540-1550). 회피 스트레이핑(MoveRandom) 대신 경로 추종.
                m_combatMove = HumanMoveFlag.None; // 이동은 MoveTarget2(per-frame m_moveIntent)가 전담
                Action();
                if (CheckArrived()) m_nav.Advance();
                else MoveTarget2();
                m_actionCnt++;
                return;
            }

            // 원본 Process 순서: CancelMoveTurn(이전 프레임 이동 플래그 확률적 해제) → Action(조준/사격) → MoveRandom(이동 set).
            CombatMoveCancel();
            Action();
            // 비좀비 무장 AI 만 회피 스트레이핑. 좀비(돌격)·맨손(항복+후퇴는 Action 내 처리)은 제외.
            if (!IsZombie() && !IsNoneWeapon(m_self.CurrentWeapon)) MoveRandom();
            m_actionCnt++;
        }

        /// <summary>전투(ACTION) 진입 공통 처리 — 카운터·스캔 플래그 초기화. 원본 newbattlemode=AI_ACTION 진입부.</summary>
        private void EnterAction()
        {
            m_mode = BattleMode.Action;
            m_actionCnt = 0;
            m_scanLeft = m_scanRight = false;
        }

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
