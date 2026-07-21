using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityXOPS
{
    [Flags]
    public enum HumanMoveFlag
    {
        None = 0,
        Forward = 1 << 0,
        Back = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,
        Walk = 1 << 4,
        Jump = 1 << 5,
    }

    /// <summary>
    /// 이번 틱 무기 액션 의도. 직접 호출 대신 플래그로 표현해 Human 이 한 곳에서 소비한다.
    /// 소비 순서는 원본 입력 처리 순서(슬롯선택 → 버림 → 무기ID전환 → 재장전 → 발사)를 따른다.
    /// </summary>
    [Flags]
    public enum HumanWeaponAction
    {
        None = 0,
        SelectFirst = 1 << 0, // 슬롯 0(보조) 선택
        SelectSecond = 1 << 1, // 슬롯 1(주) 선택
        Drop = 1 << 2, // 현재 무기 버리기
        SwitchPrevious = 1 << 3, // 슬롯 내 이전 무기 ID 로 전환
        SwitchNext = 1 << 4, // 슬롯 내 다음 무기 ID 로 전환
        Reload = 1 << 5, // 재장전
        Fire = 1 << 6, // 발사
    }

    /// <summary>
    /// 한 틱 분량의 캐릭터 입력. PlayerController(사람)·AIBrain(AI)이 채우고, 이동/조준은 HumanController 가,
    /// 무기 액션은 Human 이 소비하는 단일 입력 표면. 조준은 절대각(yaw/pitch, deg), 이동은 방향 플래그.
    /// 직렬화·리플레이 가능한 대상으로 만들기 위해 값 타입.
    /// </summary>
    public struct HumanInput
    {
        public HumanMoveFlag moveFlag;
        public float yaw;
        public float pitch;
        public HumanWeaponAction weapon;
    }

    /// <summary>
    /// OpenXOPS human::CollisionMap 포팅. 원본 알고리즘을 그대로 Unity 좌표로 재구현한다.
    /// Player는 PlayerController, AI는 AIController가 이 컨트롤러의 API로 입력을 주입.
    /// </summary>
    [RequireComponent(typeof(Human))]
    public class HumanController : MonoBehaviour
    {
        // 실제 게임플레이 씬(Maingame)에서만 Tick 허용. Briefing/Mainmenu 데모에서는 중력/AI 모두 정지.
        public static bool TickEnabled;

        // 원본 OpenXOPS object.h 상수 × 0.1 (Unity scale)
        private const float k_climbHeight = 0.32f; // HUMAN_MAPCOLLISION_CLIMBHEIGHT
        private const float k_climbForwardDist = 0.2f; // 원본 dir*2.0f (Step climb 전방 체크)
        private const float k_groundHeight = -0.05f; // HUMAN_MAPCOLLISION_GROUND_HEIGHT
        private const float k_groundR1 = 0.015f; // 플레이어 접지 반경 1
        private const float k_groundR2 = 0.05f; // 플레이어 접지 반경 2
        private const float k_groundR3 = 0.03f; // NPC 접지 반경
        private const float k_collisionAddSize = 0.001f; // COLLISION_ADDSIZE
        private const int k_moveYUpperCooldown = 8; // 경사 slide 후 점프/climb 금지 프레임

        // 브로드페이즈 반경 — 원본 HUMAN_MAPCOLLISION_CHECK_MAXDIST(12.0) × 0.1. 이 반경 밖 블록은 이번 Tick 충돌 검사에서 제외.
        private const float k_broadphaseRadius = 1.2f;

        // 원본 AddCollisionFlag 추가 허리 체크 높이 (SCHOOL 맵 좁은 통로 대응, 항상 적용)
        private const float k_addHeightA = 0.9f;
        private const float k_addHeightB = 1.3f;

        // Step climb 최소 이동 속도 (원본: |move| > 0.2/frame = 0.666 m/s)
        private const float k_climbMinSpeed = 0.666f;

        // 경사 미끄러짐 예측 시간 (원본: move*3f @ 33fps = 90ms)
        private const float k_slidePredictionTime = 0.09f;

        // 치트(F5) 강제 상승 속도 — 원본 object.cpp:2013-2017 pos_y += 5.0/frame × 0.1 scale × 33.333fps.
        private const float k_cheatRiseSpeed = 16.6667f;

        // 사망 상태머신 상수 (원본 33.33fps 기준 → 시간 기반 변환).
        // OpenXOPS HUMAN_DEADADDRY = 0.75°/frame²: 매 프레임 회전속도가 0.75°씩 증가.
        // 시간 단위 변환: 0.75 × 33.33² ≈ 833 deg/s² 각가속도.
        private const float k_deadFps = 33.3333f;
        private const float k_deadRotationAccel = 0.75f * k_deadFps * k_deadFps;
        private const float k_deadFlatLayPitch = 90f; // 평지 누움 각 (원본 deadstate 1→3 진입 후 이 각에서 안착)
        private const float k_deadFreeFallEntryPitch = 135f; // 머리가 빈 공간 → HeadStuck (자유낙하) 진입각 (원본 ±135°)
        private const float k_deadPopupHeight = 0.1f; // 사망 진입 시 시체 함몰 방지 (원본 pos_y += 1.0f × 0.1)

        private Human m_human;
        private HumanVisual m_humanVisual;

        // 이번 Tick 에서 충돌 검사할 근처 블록만 담는 재사용 리스트 (원본 CheckBlockID[] 브로드페이즈 대응). 매 Tick 갱신, GC 미발생.
        private readonly List<Block> m_nearBlocks = new List<Block>(32);

        private float m_rotationX;
        private float m_armRotationY;

        private Vector3 m_moveVelocity;
        private HumanMoveFlag m_moveFlag;
        private HumanMoveFlag m_moveFlagLt;
        private int m_moveYUpper;

        // 치트(F5+Enter 홀드) 강제 상승 플래그 — 플레이어가 매 프레임 갱신, Tick 이 소비. hold 방식이라 별도 해제 로직 불필요.
        private bool m_cheatRise;

        // 접지 여부 — 원본 OpenXOPS move_y_flag 의 반전 (접지=true). 정확도 airborne 페널티에 사용.
        // 스폰 직후 첫 FixedUpdate 전까지는 접지로 간주 (대부분 지면에서 시작).
        private bool          m_grounded = true;

        // 사망 상태머신 누적값
        private float m_deadAddRy; // 사망 회전 각속도 (deg/s), 부호 있음. Falling/LegSliding 단계 누적
        private float m_deadPitchAngle; // 사망 회전 각도 (deg), 부호 있음. +면 앞으로 엎어짐, -면 뒤로 자빠짐
        private float m_deadDirection; // +1 (앞으로 엎어짐) / -1 (뒤로 자빠짐). 사망 진입 시 1회 결정 후 고정
        private int m_settlingFrames; // Settling 단계 FixedUpdate 카운터

        public float Yaw => m_rotationX;
        public float Pitch => m_armRotationY;
        public Vector3 MoveVelocity => m_moveVelocity;
        public HumanMoveFlag MoveFlag => m_moveFlag;
        public HumanMoveFlag MoveFlagLt => m_moveFlagLt;
        public bool Grounded => m_grounded;

        private void Awake()
        {
            m_human = GetComponent<Human>();
            m_humanVisual = m_human.HumanVisual;
        }

        private void Start()
        {
            m_rotationX = transform.eulerAngles.y;

            // 팔 pitch 초기값 — 원본 armrotation_y init -30°(아래). arm-space(음수=아래) → 카메라 pitch space(양수=아래) 부호 반전.
            // 무기 든 평상시 팔이 수평(0)이 아니라 살짝 아래로 쉬게 함. 플레이어는 조작권 획득 시 0으로 덮어쓰고, AI 는 조준 시 갱신.
            m_armRotationY = -DataManager.Instance.HumanParameterData.humanGeneralData.armAngleInitial;
        }

        /// <summary>
        /// 이번 틱 입력을 주입한다. 조준각(yaw/pitch)은 덮어쓰고, 이동 플래그는 FixedUpdate 소비 전까지 OR 누적한다.
        /// 사람은 렌더 프레임마다, AI는 조준(33fps)과 이동(50fps) 케이던스가 달라 각자 호출 지점에서 채운다.
        /// 값을 클램프/정규화하지 않고 그대로 저장한다(그 책임은 호출측) — AIBrain.ApplyMovement 가 현재 조준값을
        /// 되싣어 이동만 추가하는 no-op 재읽기가 이 무변형 전제에 의존하므로 여기서 값을 변형하면 안 된다.
        /// </summary>
        /// <param name="input">이번 틱 조준·이동 입력.</param>
        public void SetInput(in HumanInput input)
        {
            m_rotationX = input.yaw;
            m_armRotationY = input.pitch;
            m_moveFlag |= input.moveFlag;
        }

        /// <summary>
        /// 치트(F5+Enter 홀드) 강제 상승 여부를 설정한다. true 면 다음 Tick 에서 수직 관통 상승한다.
        /// </summary>
        /// <param name="active">이번 프레임 상승 여부 (홀드 상태 그대로).</param>
        public void SetCheatRise(bool active) => m_cheatRise = active;

        public void AddYawPitch(float deltaYaw, float deltaPitch)
        {
            m_rotationX += deltaYaw;
            m_armRotationY += deltaPitch;
        }

        /// <summary>
        /// 외부 (Bullet 등) 가 호출하는 knockback. 원본 OpenXOPS human::AddPosOrder (object.cpp:1025-1030) 대응.
        /// (yaw, pitch) 방향으로 speed (m/s) 만큼 m_moveVelocity 에 가산. 다음 Tick 에서 attenuation 으로 자동 감쇠.
        /// </summary>
        /// <param name="yawDeg">Unity 월드 yaw (Y축 회전, deg). 0=+Z, 90=+X.</param>
        /// <param name="pitchDeg">pitch (X축 회전, deg). 0=수평, 음수=위로.</param>
        /// <param name="speedMps">초기 속도 (m/s). 원본 1.0 unit/frame ≈ 3.333 m/s.</param>
        public void AddKnockback(float yawDeg, float pitchDeg, float speedMps)
        {
            Vector3 dir = Quaternion.Euler(pitchDeg, yawDeg, 0f) * Vector3.forward;
            m_moveVelocity += dir * speedMps;
        }

        /// <summary>
        /// 폭발 등 임의 월드 방향으로 knockback. dir 은 정규화돼 있어야 함.
        /// </summary>
        public void AddKnockbackVector(Vector3 worldDir, float speedMps)
        {
            m_moveVelocity += worldDir * speedMps;
        }

        private void FixedUpdate()
        {
            if (!TickEnabled) return;

            // 사망 시 입력 플래그를 모두 클리어 (Jump 같은 잔여 입력이 step climb 등에 영향 주는 것 방지).
            // Tick 자체는 계속 돌려야 중력/지면/deadlineY 클램프가 시체에 적용됨.
            if (!m_human.Alive)
            {
                m_moveFlag = HumanMoveFlag.None;
                m_moveFlagLt = HumanMoveFlag.None;
            }

            Tick();

            // 사망 상태머신 진행 (alive면 즉시 return). 회전/Settling/Done 전이는 Tick 이후에 처리.
            TickDeadState();

            m_moveFlagLt = m_moveFlag;
            m_moveFlag = HumanMoveFlag.None;

            EmitFootstep(); // 달리기 발소리 월드사운드 — 근처 적 AI 경계 트리거 (원본 ObjectManager::Process 足音)

            // 원본 human::ProcessObject 말미의 MotionCtrl->ProcessObject 호출 대응.
            // MoveFlag_lt (= 이번 프레임 입력)와 현재 body yaw로 다리 애니메이션/회전 갱신.
            if (m_humanVisual != null)
                m_humanVisual.TickLeg(Time.fixedDeltaTime, m_moveFlagLt, m_rotationX, m_human.Alive);
        }

        /// <summary>
        /// 달리기 발소리를 월드사운드로 방출 — 청취 범위 내 "적(다른 팀)" AI 만 경계 전환. 원본 OpenXOPS objectmanager.cpp:2774-2785
        /// + soundmanager.cpp:348-365. 걷기(Walk)·정지·점프는 인식 안 됨(달리기만). 방향별 거리: 전진/좌우/후진. 방향 정보는 안 씀.
        /// </summary>
        private void EmitFootstep()
        {
            if (!m_human.Alive) return;

            HumanMoveFlag f = m_moveFlagLt;
            bool moving = (f & (HumanMoveFlag.Forward | HumanMoveFlag.Back |
                                HumanMoveFlag.Left | HumanMoveFlag.Right)) != 0;
            if (!moving || (f & HumanMoveFlag.Walk) != 0) return; // 정지/걷기 = 발소리 인식 안 됨, 달리기만

            HumanGeneralData gen = DataManager.Instance.HumanParameterData.humanGeneralData;
            float dist = (f & HumanMoveFlag.Forward) != 0 ? gen.aiHearFootstepForward
                       : (f & HumanMoveFlag.Back) != 0 ? gen.aiHearFootstepBack
                       : gen.aiHearFootstepSide;

            // 음원=발 위치(transform.position), 적(다른 팀)만 들음(allyDist=0 → 같은 팀 무시). 방향은 안 씀 — CAUTION 트리거만.
            WorldSound.EmitPointSound(transform.position, m_human.Team, dist, 0f);
        }

        private void Tick()
        {
            HumanTypeData type = m_human.HumanTypeData;
            if (type == null) return;

            HumanGeneralData gen = DataManager.Instance.HumanParameterData.humanGeneralData;
            float dt = Time.fixedDeltaTime;

            ApplyAcceleration(type, dt);

            Vector3 pos2 = transform.position;
            Vector3 pos = pos2;

            // 0. 치트(F5) 강제 상승 — 원본 object.cpp:2013-2017. CollisionMap 백업(pos2) 전에 상승분을 반영해
            // 수직(천장/블록) 충돌이 되돌리지 못하게 함(수직 관통). pos2 도 함께 올려 수평 취소 분기가 높이를 유지.
            // move_y=0 으로 중력 누적 차단. 수평 벽 충돌은 아래 로직에서 그대로 작동.
            if (m_cheatRise)
            {
                m_moveVelocity.y = 0f;
                float rise = k_cheatRiseSpeed * dt;
                pos.y += rise;
                pos2.y += rise;
            }

            // 1. XZ 이동 반영 (원본: pos_x += move_x; pos_z += move_z;)
            pos.x += m_moveVelocity.x * dt;
            pos.z += m_moveVelocity.z * dt;

            // 2. XZ 감쇠 (원본: move_x *= 0.5; 프레임당 50%, Unity는 continuous decay)
            float decay = Mathf.Exp(-type.attenuation * dt);
            m_moveVelocity.x *= decay;
            m_moveVelocity.z *= decay;

            // 3. 이동 벡터 정규화
            float dx = pos.x - pos2.x;
            float dz = pos.z - pos2.z;
            float speed = Mathf.Sqrt(dx*dx + dz*dz);
            float dirX = 0f;
            float dirZ = 0f;
            if (speed > 1e-6f) { dirX = dx / speed; dirZ = dz / speed; }

            float R = gen.controllerRadiusControllerToMap;
            float H = gen.controllerHeight;
            float waistY = H * 0.5f; // 원본 HUMAN_MAPCOLLISION_HEIGHT=10.0 → 허리
            float slopeLimit = gen.controllerSlopeLimit * Mathf.Deg2Rad;

            // 브로드페이즈: 이번 Tick 의 모든 충돌 패스가 155개 전체가 아닌 근처 블록만 검사하도록 후보를 추린다.
            // 이동 스윕(velocity*dt)과 키 높이(H)를 반경에 포함해 프레임 내 어떤 체크포인트도 놓치지 않게 함.
            BuildNearBlocks(pos, H, m_moveVelocity * dt);
            IReadOnlyList<Block> blocks = m_nearBlocks;

            if (speed > 0f || m_moveVelocity.y != 0f)
            {
                // 5a. 머리 (원본: pos_y + HEIGHT-0.22 → Unity: H - 0.022)
                for (int i = 0; i < blocks.Count; i++)
                {
                    Vector3 head = new Vector3(pos.x, pos.y + H - 0.022f, pos.z);
                    if (CollisionBlockScratch(blocks[i], ref pos, pos2, head, 0x01))
                    {
                        if (m_moveVelocity.y > 0f) m_moveVelocity.y = 0f;
                    }
                }

                // 5b. 발밑
                for (int i = 0; i < blocks.Count; i++)
                {
                    Vector3 foot = new Vector3(pos.x, pos.y, pos.z);
                    CollisionBlockScratch(blocks[i], ref pos, pos2, foot, 0x00);
                }

                // 5c. 허리 3점 (원본: 전방 + 회전된 성분 2개)
                for (int i = 0; i < blocks.Count; i++)
                {
                    CollisionBlockScratch(blocks[i], ref pos, pos2,
                        new Vector3(pos.x + dirX*R, pos.y + waistY, pos.z + dirZ*R), 0x02);
                    CollisionBlockScratch(blocks[i], ref pos, pos2,
                        new Vector3(pos.x + dirZ*R, pos.y + waistY, pos.z + dirX*R), 0x02);
                    CollisionBlockScratch(blocks[i], ref pos, pos2,
                        new Vector3(pos.x - dirZ*R, pos.y + waistY, pos.z - dirX*R), 0x02);
                }

                // 5c'. 추가 허리 체크 (원본 AddCollisionFlag, 발 위 수직 2점)
                for (int i = 0; i < blocks.Count; i++)
                {
                    CollisionBlockScratch(blocks[i], ref pos, pos2,
                        new Vector3(pos.x, pos.y + k_addHeightA, pos.z), 0x02);
                    CollisionBlockScratch(blocks[i], ref pos, pos2,
                        new Vector3(pos.x, pos.y + k_addHeightB, pos.z), 0x02);
                }

                // 5d. Step climb (원본 object.cpp:1607-1644)
                float absVx = Mathf.Abs(m_moveVelocity.x);
                float absVz = Mathf.Abs(m_moveVelocity.z);
                if ((absVx > k_climbMinSpeed || absVz > k_climbMinSpeed) && m_moveYUpper == 0)
                {
                    bool flag = false;
                    for (int i = 0; i < blocks.Count; i++)
                    {
                        // 원본은 블록 AABB에 COLLISION_ADDSIZE 여유가 포함돼 있어 y=경계도 내부로 판정.
                        // Unity에서는 foot.y에 동일한 마진을 더해 등가 효과.
                        Vector3 foot = new Vector3(pos.x + dirX*k_climbForwardDist, pos.y + k_collisionAddSize, pos.z + dirZ*k_climbForwardDist);
                        Vector3 top  = new Vector3(foot.x, foot.y + k_climbHeight, foot.z);
                        if (blocks[i].Contains(foot) && !blocks[i].Contains(top))
                        {
                            flag = true;

                            // 발 아래 면의 각도 체크 (원본: 1.2 단위 → 0.12 Unity)
                            if (blocks[i].IntersectRay(
                                new Vector3(pos.x, pos.y, pos.z), Vector3.down, 0.12f,
                                out int face, out _))
                            {
                                float ny = Mathf.Clamp(blocks[i].faceNormals[face].y, -1f, 1f);
                                if (Mathf.Acos(ny) > slopeLimit)
                                {
                                    flag = false;
                                    break;
                                }
                            }
                        }
                    }

                    if (flag)
                    {
                        // 원본: pos_y += CLIMBADDY(0.04/frame). Unity continuous: stepClimbSpeed (m/s)
                        pos.y           += gen.controllerStepClimbSpeed * dt;
                        m_moveVelocity.y *= 0.2f;
                    }
                }

                // 5e. Sanity 체크 (원본 1647-1668): 블록에 몸이 깊이 박혔는지 최종 검사
                //  - 어깨 높이에서 아래로 레이 → 맞으면 XZ 이동 취소
                //  - 어깨 근처 점이 블록 내부 && 예측 위치도 내부 → 전부 취소
                float shoulderY    = pos.y + H - 0.02f;
                float shoulderRayY = pos.y + H - 0.2f;
                float shoulderMax  = H - 0.4f;
                for (int i = 0; i < blocks.Count; i++)
                {
                    if (blocks[i].IntersectRay(
                        new Vector3(pos.x, shoulderRayY, pos.z), Vector3.down, shoulderMax,
                        out _, out _))
                    {
                        pos.x = pos2.x;
                        pos.z = pos2.z;
                    }

                    if (blocks[i].Contains(new Vector3(pos.x, shoulderY, pos.z)))
                    {
                        Vector3 pred = new Vector3(
                            pos.x + m_moveVelocity.x * 0.33f,
                            shoulderY,
                            pos.z + m_moveVelocity.z * 0.33f);
                        // 예측 지점(*0.33)은 고속 시 브로드페이즈 반경을 벗어날 수 있어, 원본 CheckALLBlockInside 처럼
                        // 근처 리스트가 아닌 전체 블록으로 검사한다. 매몰 상태에서만 진입하므로 비용은 무시할 수준.
                        IReadOnlyList<Block> allBlocks = MapLoader.BlockColliders;
                        bool predInAny = false;
                        for (int j = 0; j < allBlocks.Count; j++)
                        {
                            if (allBlocks[j].Contains(pred)) { predInAny = true; break; }
                        }
                        if (predInAny)
                        {
                            pos = pos2;
                            if (m_moveVelocity.y > 0f) m_moveVelocity.y = 0f;
                        }
                    }
                }
            }

            if (m_moveYUpper > 0) m_moveYUpper--;

            // 6. 3 서브스텝 낙하 및 접지 체크
            bool fallFlag = false;
            bool isAlive  = m_human.Alive;
            for (int ycnt = 0; ycnt < 3; ycnt++)
            {
                float ang = Mathf.Atan2(m_moveVelocity.z, m_moveVelocity.x);

                // 낙하
                pos.y += m_moveVelocity.y * dt * (1f / 3f);

                float gy = pos.y + k_groundHeight;

                if (isAlive)
                {
                    // Alive: 플레이어 8점 접지 체크 (NPC 분기는 추후). 4 R1 또는 4 R2 모두 블록 내부면 접지.
                    int cnt = 0;
                    if (AnyBlockContains(blocks, pos.x + Mathf.Cos(ang)*k_groundR1, gy, pos.z + Mathf.Sin(ang)*k_groundR1)) cnt++;
                    if (AnyBlockContains(blocks, pos.x - Mathf.Cos(ang)*k_groundR1, gy, pos.z - Mathf.Sin(ang)*k_groundR1)) cnt++;
                    if (AnyBlockContains(blocks, pos.x + Mathf.Cos(ang + Mathf.PI*0.5f)*k_groundR1, gy, pos.z + Mathf.Sin(ang + Mathf.PI*0.5f)*k_groundR1)) cnt++;
                    if (AnyBlockContains(blocks, pos.x + Mathf.Cos(ang - Mathf.PI*0.5f)*k_groundR1, gy, pos.z + Mathf.Sin(ang - Mathf.PI*0.5f)*k_groundR1)) cnt++;
                    if (cnt == 4) { fallFlag = true; break; }

                    cnt = 0;
                    if (AnyBlockContains(blocks, pos.x + Mathf.Cos(ang)*k_groundR2, gy, pos.z + Mathf.Sin(ang)*k_groundR2)) cnt++;
                    if (AnyBlockContains(blocks, pos.x - Mathf.Cos(ang)*k_groundR2, gy, pos.z - Mathf.Sin(ang)*k_groundR2)) cnt++;
                    if (AnyBlockContains(blocks, pos.x + Mathf.Cos(ang + Mathf.PI*0.5f)*k_groundR2, gy, pos.z + Mathf.Sin(ang + Mathf.PI*0.5f)*k_groundR2)) cnt++;
                    if (AnyBlockContains(blocks, pos.x + Mathf.Cos(ang - Mathf.PI*0.5f)*k_groundR2, gy, pos.z + Mathf.Sin(ang - Mathf.PI*0.5f)*k_groundR2)) cnt++;
                    if (cnt == 4) { fallFlag = true; break; }
                }
                else
                {
                    // 시체: 단일 발 점 체크 (원본 OpenXOPS deadstate 2 object.cpp:1320-1326 대응).
                    // 절벽 모서리에서 8점이 모두 인사이드라 부유하던 문제 해소.
                    if (AnyBlockContains(blocks, pos.x, gy, pos.z))
                    {
                        fallFlag = true;
                        break;
                    }
                }

                // 중력 1 서브스텝분
                m_moveVelocity.y -= gen.gravityAcceleration * dt * (1f / 3f);
                if (m_moveVelocity.y < gen.fallMaxSpeed) m_moveVelocity.y = gen.fallMaxSpeed;
            }

            // 접지 상태 갱신 — fallFlag=true 는 발밑 블록 감지(접지)를 의미. 정확도 airborne 페널티에 사용.
            m_grounded = fallFlag;

            // 7. 접지 처리 및 경사 미끄러짐
            if (fallFlag)
            {
                // 낙하 데미지 — 임계 속도(fallMinSpeed) 보다 빠르게 착지한 프레임 1회. 원본 object.cpp:1792-1797.
                //   damage = floor(fallDamageMax / |fallMaxSpeed - fallMinSpeed| × |v - fallMinSpeed|) + Random(0..fallDamageRandomMax)
                // 종단속도(fallMaxSpeed) 착지 시 fallDamageMax + rand → HP 100 즉사. 임계점 착지면 rand 만(0~5).
                if (m_human.Alive && m_moveVelocity.y < gen.fallMinSpeed)
                {
                    float scale  = gen.fallDamageMax / Mathf.Abs(gen.fallMaxSpeed - gen.fallMinSpeed);
                    float damage = Mathf.Floor(scale * Mathf.Abs(m_moveVelocity.y - gen.fallMinSpeed))
                                 + UnityEngine.Random.Range(0, gen.fallDamageRandomMax);
                    m_human.ApplyDamage(damage);
                }

                m_moveVelocity.y = 0f;

                // 점프 (원본: 이전 프레임 점프 입력 + 쿨다운 없음)
                if ((m_moveFlagLt & HumanMoveFlag.Jump) != 0 && m_moveYUpper == 0)
                {
                    m_moveVelocity.y = type.jumpSpeed;
                }

                // 발 아래 면을 찾아 경사각 체크
                for (int i = 0; i < blocks.Count; i++)
                {
                    // 원본: pos_y + 2.5 → 0.25, maxDist 3.5 → 0.35
                    if (blocks[i].IntersectRay(
                        new Vector3(pos.x, pos.y + 0.25f, pos.z), Vector3.down, 0.35f,
                        out int face, out _))
                    {
                        Vector3 n       = blocks[i].faceNormals[face];
                        float   nYClamp = Mathf.Clamp(n.y, -1f, 1f);

                        if (Mathf.Acos(nYClamp) > slopeLimit)
                        {
                            // 원본: move_x = nx*1.2, move_y = ny*-0.5, move_z = nz*1.2 (프레임당)
                            // Unity: × 33.33 fps × 0.1 scale = ×3.333
                            m_moveVelocity.x = n.x * 4.0f;
                            m_moveVelocity.y = n.y * -1.667f;
                            m_moveVelocity.z = n.z * 4.0f;

                            // 다음 예상 위치 클램프 (원본: pos + move*3.0f = 3프레임 후 ≈ 90ms)
                            Vector3 pred = pos + m_moveVelocity * k_slidePredictionTime;
                            for (int j = 0; j < blocks.Count; j++)
                            {
                                if (blocks[j].Contains(pred))
                                {
                                    m_moveVelocity.y = 0f;
                                    if (blocks[j].Contains(new Vector3(pred.x, pos.y, pred.z)))
                                    {
                                        m_moveVelocity.x = 0f;
                                        m_moveVelocity.z = 0f;
                                        break;
                                    }
                                }
                            }

                            m_moveYUpper = k_moveYUpperCooldown;
                        }
                        break;
                    }
                }
            }

            // deadlineY 클램프: 원본 OpenXOPS object.cpp:2083-2088 hp=0 즉사 + 위치 클램프(주석처리)에 대응.
            // 클램프 + 사망 진입 트리거를 함께 적용. Alive가 이미 false면 트리거 없이 클램프만.
            bool clamped = false;
            if (pos.y < gen.deadlineY)
            {
                pos.y = gen.deadlineY;
                m_moveVelocity.y = 0f;
                clamped = true;
            }

            // 사망 진입 조건: Alive 상태에서 HP ≤ 0 또는 deadlineY 도달.
            if (m_human.Alive && (m_human.HP <= 0f || clamped))
                EnterDeadState(ref pos);

            transform.position = pos;
        }

        /// <summary>
        /// Alive → Falling 사망 진입. 회전 누적값 초기화 + 시체 함몰 방지 popup + 두 슬롯 무기 흩뿌리기.
        /// 스코프 해제 / AI 정지 등은 추후 단계.
        /// </summary>
        private void EnterDeadState(ref Vector3 pos)
        {
            m_human.SetDeadState(HumanDeadState.Falling);

            // 모드 이벤트 — 사망 진입 1회. (식별번호, 팀, 위치). 게임 로직은 그대로, Emit 한 줄만.
            UnityXOPS.Modding.XOPSEventBus.Emit("humanDied", m_human.Identifier, m_human.Team, pos.x, pos.y, pos.z);

            // 원본 object.cpp:1213-1222 — Hit_rx 와 본인 yaw 차이로 앞/뒤 분기.
            // |Δyaw| < 90° (등 뒤에서 맞음) → 앞으로 엎어짐 (+pitch),
            // 그 외 (앞/옆에서 맞음) → 뒤로 자빠짐 (-pitch). 정확히 ±90° 는 else (뒤로) 분기.
            float deltaYaw = Mathf.DeltaAngle(m_human.HitYaw, m_rotationX);
            m_deadDirection = (Mathf.Abs(deltaYaw) < 90f) ? +1f : -1f;

            m_deadAddRy = 0f;
            m_deadPitchAngle = 0f;
            m_settlingFrames = 0;
            pos.y += k_deadPopupHeight;

            // 원본 OpenXOPS object.cpp:1228-1237 사망 진입 루프 — noneWeapon 이외 슬롯의 무기를 무작위로 흩뿌림.
            m_human.DropAllWeaponsOnDeath();

            // 사망 이펙트 — HumanTypeData.deathEffectIndex 가 가리키는 이펙트(예: 로봇=RobotDeathSmoke). -1 이면 Play 가 무시.
            // 원본 OpenXOPS DeadEffect (objectmanager.cpp:1234) — type==1(로봇)만 연기. UnityXOPS 는 타입별 인덱스 데이터로 일반화.
            HumanTypeData type = m_human.HumanTypeData;
            if (type != null) EffectManager.Instance.Play(type.deathEffectIndex, m_human.transform.position);
        }

        /// <summary>
        /// 사망 상태머신 진행. Alive면 no-op. 원본 OpenXOPS object.cpp:1265-1377 deadstate 1/2/3 대응.
        /// Falling: 회전 누적 → 머리 위치 예측해서 평지 안착(Settling) / 절벽 추락(HeadStuck) 분기.
        /// HeadStuck: 회전 정지, alive Tick의 fall section이 중력+단일 발 점 체크로 자유낙하 처리.
        /// LegSliding: 골격만 (즉시 Settling 폴백, 추후 분기).
        /// </summary>
        private void TickDeadState()
        {
            if (m_human.Alive) return;

            float dt = Time.fixedDeltaTime;
            HumanGeneralData gen = DataManager.Instance.HumanParameterData.humanGeneralData;
            float deadlineY = gen.deadlineY;
            Vector3 curPos = transform.position;

            switch (m_human.DeadState)
            {
                case HumanDeadState.Falling:
                {
                    // m_deadDirection 부호에 따라 회전 방향이 결정되므로 검사는 abs 비교.
                    // (A) deadlineY 근처(원본 +10.0 → +1.0 Unity)에서 이미 90° 이상이면 즉시 Settling.
                    //     원본 OpenXOPS object.cpp:1273-1279 — 공중/경계 사망은 HeadStuck 거치지 않고 평지 누움.
                    if (curPos.y <= deadlineY + 1.0f && Mathf.Abs(m_deadPitchAngle) >= k_deadFlatLayPitch)
                    {
                        m_deadPitchAngle = k_deadFlatLayPitch * m_deadDirection;
                        m_deadAddRy = 0f;
                        m_human.SetDeadState(HumanDeadState.Settling);
                        break;
                    }

                    // 1. 현재 pitch가 이미 135° 이상이면 HeadStuck (이전 프레임 누적).
                    if (Mathf.Abs(m_deadPitchAngle) >= k_deadFreeFallEntryPitch)
                    {
                        m_human.SetDeadState(HumanDeadState.HeadStuck);
                        m_moveVelocity.y = 0f;
                        break;
                    }

                    // 2. 가속 후 다음 프레임 pitch 예측 (원본은 add_ry += DEADADDRY 후 ry+add_ry 사용).
                    //    부호는 m_deadDirection 으로 결정. 가속도 자체에 부호 곱.
                    m_deadAddRy += k_deadRotationAccel * dt * m_deadDirection;
                    float thisDeltaPitch = m_deadAddRy * dt;
                    float predictedPitch = m_deadPitchAngle + thisDeltaPitch;

                    // (B) deadlineY 이하(원본 object.cpp:1289-1291)에서는 머리 충돌 체크 없이 회전만 누적.
                    //     아래 (A) 분기가 다음 프레임에 90° 이상에서 잡아주므로 HeadStuck/LegSliding 진입 차단.
                    if (curPos.y <= deadlineY)
                    {
                        m_deadPitchAngle = predictedPitch;
                        break;
                    }

                    // 3. 예측 pitch 절대값이 135° 이상이면 HeadStuck (이번 프레임에 한계 도달).
                    if (Mathf.Abs(predictedPitch) >= k_deadFreeFallEntryPitch)
                    {
                        m_deadPitchAngle = predictedPitch;
                        m_human.SetDeadState(HumanDeadState.HeadStuck);
                        m_moveVelocity.y = 0f;
                        break;
                    }

                    // 4. 예측 pitch에서 머리 위치 충돌 체크. 박혔으면 회전 미커밋 + 분기 전이.
                    if (IsHeadInsideBlock(predictedPitch))
                    {
                        if (Mathf.Abs(predictedPitch) > k_deadFlatLayPitch)
                        {
                            // 지면 박힘 (90° 통과) → Settling 직행, 90° 클램프 (부호 보존)
                            m_deadPitchAngle = k_deadFlatLayPitch * m_deadDirection;
                            m_deadAddRy = 0f;
                            m_human.SetDeadState(HumanDeadState.Settling);
                        }
                        else
                        {
                            // 벽 박힘 (90° 미만) → LegSliding 진입. 현재 각도 보존, m_deadAddRy 유지.
                            m_human.SetDeadState(HumanDeadState.LegSliding);
                        }
                        break;
                    }

                    // 5. 안전 → 회전 커밋
                    m_deadPitchAngle = predictedPitch;
                    break;
                }

                case HumanDeadState.HeadStuck:
                    // 원본 OpenXOPS object.cpp:1310-1313: HeadStuck 도중에도 deadlineY 도달 시 즉시 Settling.
                    if (curPos.y <= deadlineY)
                    {
                        m_human.SetDeadState(HumanDeadState.Settling);
                        break;
                    }

                    // 회전 멈춤. 중력은 alive Tick의 fall section에서 적용. 단일 발 점 grounded → velocity.y = 0.
                    // velocity.y == 0 이면 발이 닿았다는 신호 → Settling.
                    if (m_moveVelocity.y == 0f)
                        m_human.SetDeadState(HumanDeadState.Settling);
                    break;

                case HumanDeadState.LegSliding:
                {
                    // 원본 OpenXOPS deadstate 3 (object.cpp:1334-1377) 포팅.
                    // 회전을 계속 진행하면서 발을 -forward 방향으로 sin(thisDelta)*H 만큼 슬라이드 →
                    // 머리가 벽 안으로 더 들어가지 않게 발 위치 자체를 backwards로 빼냄.
                    if (Mathf.Abs(m_deadPitchAngle) >= k_deadFlatLayPitch)
                    {
                        m_deadPitchAngle = k_deadFlatLayPitch * m_deadDirection;
                        m_deadAddRy = 0f;
                        m_human.SetDeadState(HumanDeadState.Settling);
                        break;
                    }

                    m_deadAddRy += k_deadRotationAccel * dt * m_deadDirection;
                    float thisDeltaPitch = m_deadAddRy * dt;
                    float thisDeltaRad = thisDeltaPitch * Mathf.Deg2Rad;
                    float predictedPitch = m_deadPitchAngle + thisDeltaPitch;
                    float predictedPitchRad = predictedPitch * Mathf.Deg2Rad;

                    float H = gen.controllerHeight;

                    // Body fall direction (forward) / slide direction (-forward = back).
                    Quaternion yawQ = Quaternion.Euler(0, m_rotationX, 0);
                    Vector3 slideDir = yawQ * Vector3.back;

                    // 이번 프레임 슬라이드 (원본 pos -= sin(add_ry)*H 와 동일 패턴).
                    Vector3 thisFrameSlide = slideDir * (Mathf.Sin(thisDeltaRad) * H);
                    Vector3 nextPos = transform.position + thisFrameSlide;

                    // 슬라이드 후 발/머리 예측. body local up 이 pitch 회전 후 (0, cos, sin)Z.
                    Vector3 nextHeadOffset = yawQ * new Vector3(0f, H * Mathf.Cos(predictedPitchRad), H * Mathf.Sin(predictedPitchRad));
                    Vector3 nextFootCheck  = nextPos + Vector3.up * 0.1f;
                    Vector3 nextHeadCheck  = nextPos + nextHeadOffset;

                    // 발 또는 머리가 블록 안 → state 4 (Settling) 정지.
                    IReadOnlyList<Block> blocks = MapLoader.BlockColliders;
                    bool blocked = false;
                    for (int i = 0; i < blocks.Count; i++)
                    {
                        if (blocks[i].Contains(nextFootCheck) || blocks[i].Contains(nextHeadCheck))
                        {
                            blocked = true;
                            break;
                        }
                    }

                    if (blocked)
                    {
                        m_deadAddRy = 0f;
                        m_human.SetDeadState(HumanDeadState.Settling);
                        break;
                    }

                    // 슬라이드 + 회전 커밋
                    transform.position += thisFrameSlide;
                    m_deadPitchAngle = predictedPitch;
                    break;
                }

                case HumanDeadState.Settling:
                    m_settlingFrames++;
                    if (m_settlingFrames >= 1)
                        m_human.SetDeadState(HumanDeadState.Done);
                    break;

                case HumanDeadState.Done:
                    break;
            }
        }

        /// <summary>
        /// 원본 human::CollisionBlockScratch 포팅. 체크 포인트(inV)가 블록 내부에 들어가 있으면
        /// 면 법선 기반 슬라이드로 pos를 업데이트.
        /// </summary>
        /// <param name="mode">0x00: 통상, 0x01: Y 상승 금지, 0x02: Y 고정.</param>
        /// <returns>레이가 블록 면에 맞아 처리가 실행됐으면 true.</returns>
        private bool CollisionBlockScratch(Block block, ref Vector3 pos, Vector3 posOld, Vector3 inV, int mode)
        {
            if (block == null || !block.collider) return false;

            // 발밑(0x00)은 바닥 이음매 걸림 방지용 보정
            if (mode == 0x00) inV.y += k_collisionAddSize;

            Vector3 posBackup = pos;

            Vector3 v = pos - posOld;
            float dist = v.magnitude;
            if (dist < 1e-6f) return false;
            v /= dist;

            // 시작점: inV - v*dist (프레임 시작 시의 체크 포인트)
            // rayStart를 살짝 뒤로 밀고 maxDist를 그만큼 늘려, 경계 위에서 시작하는 경우도 안전하게 면 감지.
            const float k_rayStartMargin = 1e-4f;
            Vector3 rayStart = inV - v * (dist + k_rayStartMargin);

            if (!block.IntersectRay(rayStart, v, dist + k_rayStartMargin, out int face, out _))
                return false;

            // 면과 이동 벡터의 각도 = acos(dot(v, n)). dot이 음수여야 정면 충돌.
            Vector3 n = block.faceNormals[face];
            float dot = Vector3.Dot(v, n);
            if (dot >= 0f) return false;
            float faceAngle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f));

            // face_angle_per = PI / face_angle - 1
            // 정면(PI) → 0 (완전 정지) / 수직(PI/2) → 1 (원래 이동 유지)
            float per = (faceAngle > 1e-6f) ? (Mathf.PI / faceAngle - 1f) : 0f;

            // v + n 정규화 후 per로 블렌드
            Vector3 v2 = v + n;
            if (v2.sqrMagnitude > 1e-8f) v2.Normalize();

            Vector3 vBlend = v2 * (1f - per) + v * per;
            if (vBlend.sqrMagnitude > 1e-8f) vBlend.Normalize();

            // 수평 성분 전부 0이면 법선 사용
            if (Mathf.Abs(vBlend.x) < 1e-6f && Mathf.Abs(vBlend.z) < 1e-6f)
                vBlend = n;

            float temp = per * dist;
            Vector3 newPos = vBlend * temp + posOld;

            // 최종 위치가 여전히 블록 내부면 롤백
            if (block.Contains(newPos)) newPos = posOld;

            // 모드별 Y 보정
            if (mode == 0x01 && newPos.y > posBackup.y) newPos.y = posBackup.y;
            if (mode == 0x02) newPos.y = posBackup.y;

            pos = newPos;
            return true;
        }

        /// <summary>
        /// 원본 human::CollisionMap 의 CheckBlockID[] 프리필터 대응. 전체 블록 중 이번 Tick 검사 대상인 근처 블록만
        /// m_nearBlocks 에 추린다. human 위치 기준 박스(반경 k_broadphaseRadius, 상단은 키 height 만큼 추가 + 이동 스윕)에
        /// AABB 가 겹치는 블록만 통과. 이후 모든 충돌 패스는 이 리스트만 순회한다.
        /// </summary>
        /// <param name="pos">이번 Tick 의 현재 human 위치(발 기준).</param>
        /// <param name="height">human 키(controllerHeight) — 머리/어깨 체크포인트를 반경에 포함하기 위한 상단 확장.</param>
        /// <param name="moveDelta">이번 Tick 예상 이동량(velocity×dt) — 스윕 방향으로 박스를 늘려 이동 후 위치까지 커버.</param>
        private void BuildNearBlocks(Vector3 pos, float height, Vector3 moveDelta)
        {
            m_nearBlocks.Clear();

            float r = k_broadphaseRadius;
            Vector3 lo = new Vector3(
                pos.x - r + Mathf.Min(0f, moveDelta.x),
                pos.y - r + Mathf.Min(0f, moveDelta.y),
                pos.z - r + Mathf.Min(0f, moveDelta.z));
            Vector3 hi = new Vector3(
                pos.x + r + Mathf.Max(0f, moveDelta.x),
                pos.y + height + r + Mathf.Max(0f, moveDelta.y),
                pos.z + r + Mathf.Max(0f, moveDelta.z));

            IReadOnlyList<Block> all = MapLoader.BlockColliders;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].OverlapsAABB(lo, hi)) m_nearBlocks.Add(all[i]);
            }
        }

        private static bool AnyBlockContains(IReadOnlyList<Block> blocks, float x, float y, float z)
        {
            Vector3 p = new Vector3(x, y, z);
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].Contains(p)) return true;
            }
            return false;
        }

        /// <summary>
        /// 사망 회전 pitchDeg에서 머리가 블록 내부에 있는지 검사. 원본 OpenXOPS deadstate 1 머리 위치 예측 (object.cpp:1294-1296) 대응.
        /// 평지 위 사망 → 90° 통과 시 머리가 지면 블록 안 → 안착 트리거.
        /// 절벽 끝 사망 → 머리가 빈 공간 유지 → 135°까지 회전 후 자유낙하 진입.
        /// 벽 앞 사망 → 회전 도중 머리가 벽 안 → LegSliding 진입.
        /// </summary>
        private bool IsHeadInsideBlock(float pitchDeg)
        {
            HumanGeneralData gen = DataManager.Instance.HumanParameterData.humanGeneralData;
            Quaternion deathRot = Quaternion.Euler(0, m_rotationX, 0)
                                * Quaternion.Euler(pitchDeg, 0, 0);
            Vector3 headOffset = deathRot * new Vector3(0f, gen.controllerHeight, 0f);
            Vector3 headPos = transform.position + headOffset;

            IReadOnlyList<Block> blocks = MapLoader.BlockColliders;
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].Contains(headPos)) return true;
            }
            return false;
        }

        private void ApplyAcceleration(HumanTypeData type, float dt)
        {
            HumanMoveFlag moveMask = m_moveFlag & (
                HumanMoveFlag.Forward | HumanMoveFlag.Back |
                HumanMoveFlag.Left | HumanMoveFlag.Right);
            bool walk = (m_moveFlag & HumanMoveFlag.Walk) != 0;

            Vector3 localDir = Vector3.zero;
            float accel = 0f;

            if (walk)
            {
                localDir = Vector3.forward;
                accel = type.progressWalkAcceleration;
            }
            else
            {
                const float k_invSqrt2 = 0.7071068f;
                float runForward = type.progressRunAcceleration;
                float runSide = type.sidewaysRunAcceleration;
                float runBack = type.regressRunAcceleration;
                float diagForward = (runForward + runSide) * 0.5f;

                switch (moveMask)
                {
                    case HumanMoveFlag.Forward:
                        localDir = Vector3.forward; accel = runForward; break;
                    case HumanMoveFlag.Back:
                        localDir = Vector3.back; accel = runBack; break;
                    case HumanMoveFlag.Left:
                        localDir = Vector3.left; accel = runSide; break;
                    case HumanMoveFlag.Right:
                        localDir = Vector3.right; accel = runSide; break;
                    case HumanMoveFlag.Forward | HumanMoveFlag.Left:
                        localDir = new Vector3(-k_invSqrt2, 0, k_invSqrt2); accel = diagForward; break;
                    case HumanMoveFlag.Forward | HumanMoveFlag.Right:
                        localDir = new Vector3(k_invSqrt2, 0, k_invSqrt2); accel = diagForward; break;
                    case HumanMoveFlag.Back | HumanMoveFlag.Left:
                        localDir = new Vector3(-k_invSqrt2, 0, -k_invSqrt2); accel = runBack; break;
                    case HumanMoveFlag.Back | HumanMoveFlag.Right:
                        localDir = new Vector3(k_invSqrt2, 0, -k_invSqrt2); accel = runBack; break;
                }
            }

            if (accel <= 0f) return;

            Vector3 worldDir = Quaternion.Euler(0, m_rotationX, 0) * localDir;
            m_moveVelocity += worldDir * (accel * dt);
        }

        private void LateUpdate()
        {
            // body yaw × death pitch 합성. Alive 시 m_deadPitchAngle = 0 이므로 기존 동작과 동일.
            transform.rotation = Quaternion.Euler(0, m_rotationX, 0)
                               * Quaternion.Euler(m_deadPitchAngle, 0, 0);

            if (!m_human.Alive) return;

            if (m_humanVisual != null)
                m_humanVisual.SetArmPitch(m_armRotationY);
        }
    }
}
