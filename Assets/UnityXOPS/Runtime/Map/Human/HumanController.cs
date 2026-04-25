using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityXOPS
{
    [Flags]
    public enum HumanMoveFlag
    {
        None    = 0,
        Forward = 1 << 0,
        Back    = 1 << 1,
        Left    = 1 << 2,
        Right   = 1 << 3,
        Walk    = 1 << 4,
        Jump    = 1 << 5,
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
        private const float k_climbHeight         = 0.32f;   // HUMAN_MAPCOLLISION_CLIMBHEIGHT
        private const float k_climbForwardDist    = 0.2f;    // 원본 dir*2.0f (Step climb 전방 체크)
        private const float k_groundHeight        = -0.05f;  // HUMAN_MAPCOLLISION_GROUND_HEIGHT
        private const float k_groundR1            = 0.015f;  // 플레이어 접지 반경 1
        private const float k_groundR2            = 0.05f;   // 플레이어 접지 반경 2
        private const float k_groundR3            = 0.03f;   // NPC 접지 반경
        private const float k_collisionAddSize    = 0.001f;  // COLLISION_ADDSIZE
        private const int   k_moveYUpperCooldown  = 8;       // 경사 slide 후 점프/climb 금지 프레임

        // 원본 AddCollisionFlag 추가 허리 체크 높이 (SCHOOL 맵 좁은 통로 대응, 항상 적용)
        private const float k_addHeightA          = 0.9f;
        private const float k_addHeightB          = 1.3f;

        // Step climb 최소 이동 속도 (원본: |move| > 0.2/frame = 0.666 m/s)
        private const float k_climbMinSpeed       = 0.666f;

        // 경사 미끄러짐 예측 시간 (원본: move*3f @ 33fps = 90ms)
        private const float k_slidePredictionTime = 0.09f;

        private Human       m_human;
        private HumanVisual m_humanVisual;

        private float m_rotationX;
        private float m_armRotationY;

        private Vector3       m_moveVelocity;
        private HumanMoveFlag m_moveFlag;
        private HumanMoveFlag m_moveFlagLt;
        private int           m_moveYUpper;

        public float         Yaw          => m_rotationX;
        public float         Pitch        => m_armRotationY;
        public Vector3       MoveVelocity => m_moveVelocity;
        public HumanMoveFlag MoveFlag     => m_moveFlag;
        public HumanMoveFlag MoveFlagLt   => m_moveFlagLt;

        private void Awake()
        {
            m_human       = GetComponent<Human>();
            m_humanVisual = m_human.HumanVisual;
        }

        private void Start()
        {
            m_rotationX = transform.eulerAngles.y;
        }

        public void SetMoveFlag(HumanMoveFlag flag) { m_moveFlag |= flag; }

        public void SetYawPitch(float yaw, float pitch)
        {
            m_rotationX    = yaw;
            m_armRotationY = pitch;
        }

        public void AddYawPitch(float deltaYaw, float deltaPitch)
        {
            m_rotationX    += deltaYaw;
            m_armRotationY += deltaPitch;
        }

        private void FixedUpdate()
        {
            if (!TickEnabled) return;
            if (!m_human.Alive) return;

            Tick();

            m_moveFlagLt = m_moveFlag;
            m_moveFlag   = HumanMoveFlag.None;

            // 원본 human::ProcessObject 말미의 MotionCtrl->ProcessObject 호출 대응.
            // MoveFlag_lt (= 이번 프레임 입력)와 현재 body yaw로 다리 애니메이션/회전 갱신.
            if (m_humanVisual != null)
                m_humanVisual.TickLeg(Time.fixedDeltaTime, m_moveFlagLt, m_rotationX, m_human.Alive);
        }

        private void Tick()
        {
            HumanTypeData type = m_human.HumanTypeData;
            if (type == null) return;

            HumanGeneralData gen = DataManager.Instance.HumanParameterData.humanGeneralData;
            float            dt  = Time.fixedDeltaTime;

            ApplyAcceleration(type, dt);

            Vector3 pos2 = transform.position;
            Vector3 pos  = pos2;

            // 1. XZ 이동 반영 (원본: pos_x += move_x; pos_z += move_z;)
            pos.x += m_moveVelocity.x * dt;
            pos.z += m_moveVelocity.z * dt;

            // 2. XZ 감쇠 (원본: move_x *= 0.5; 프레임당 50%, Unity는 continuous decay)
            float decay = Mathf.Exp(-type.attenuation * dt);
            m_moveVelocity.x *= decay;
            m_moveVelocity.z *= decay;

            // 3. 이동 벡터 정규화
            float dx    = pos.x - pos2.x;
            float dz    = pos.z - pos2.z;
            float speed = Mathf.Sqrt(dx*dx + dz*dz);
            float dirX  = 0f;
            float dirZ  = 0f;
            if (speed > 1e-6f) { dirX = dx / speed; dirZ = dz / speed; }

            float R          = gen.controllerRadiusControllerToMap;
            float H          = gen.controllerHeight;
            float waistY     = H * 0.5f;  // 원본 HUMAN_MAPCOLLISION_HEIGHT=10.0 → 허리
            float slopeLimit = gen.controllerSlopeLimit * Mathf.Deg2Rad;

            IReadOnlyList<Block> blocks = MapLoader.BlockColliders;

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
                        bool predInAny = false;
                        for (int j = 0; j < blocks.Count; j++)
                        {
                            if (blocks[j].Contains(pred)) { predInAny = true; break; }
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
            for (int ycnt = 0; ycnt < 3; ycnt++)
            {
                float ang = Mathf.Atan2(m_moveVelocity.z, m_moveVelocity.x);

                // 낙하
                pos.y += m_moveVelocity.y * dt * (1f / 3f);

                // 플레이어 8점 접지 체크 (NPC 분기는 추후)
                float gy = pos.y + k_groundHeight;

                // 4방향 R1: 모두 블록 내부면 접지
                int cnt = 0;
                if (AnyBlockContains(blocks, pos.x + Mathf.Cos(ang)*k_groundR1,                   gy, pos.z + Mathf.Sin(ang)*k_groundR1)) cnt++;
                if (AnyBlockContains(blocks, pos.x - Mathf.Cos(ang)*k_groundR1,                   gy, pos.z - Mathf.Sin(ang)*k_groundR1)) cnt++;
                if (AnyBlockContains(blocks, pos.x + Mathf.Cos(ang + Mathf.PI*0.5f)*k_groundR1,   gy, pos.z + Mathf.Sin(ang + Mathf.PI*0.5f)*k_groundR1)) cnt++;
                if (AnyBlockContains(blocks, pos.x + Mathf.Cos(ang - Mathf.PI*0.5f)*k_groundR1,   gy, pos.z + Mathf.Sin(ang - Mathf.PI*0.5f)*k_groundR1)) cnt++;
                if (cnt == 4) { fallFlag = true; break; }

                // 4방향 R2
                cnt = 0;
                if (AnyBlockContains(blocks, pos.x + Mathf.Cos(ang)*k_groundR2,                   gy, pos.z + Mathf.Sin(ang)*k_groundR2)) cnt++;
                if (AnyBlockContains(blocks, pos.x - Mathf.Cos(ang)*k_groundR2,                   gy, pos.z - Mathf.Sin(ang)*k_groundR2)) cnt++;
                if (AnyBlockContains(blocks, pos.x + Mathf.Cos(ang + Mathf.PI*0.5f)*k_groundR2,   gy, pos.z + Mathf.Sin(ang + Mathf.PI*0.5f)*k_groundR2)) cnt++;
                if (AnyBlockContains(blocks, pos.x + Mathf.Cos(ang - Mathf.PI*0.5f)*k_groundR2,   gy, pos.z + Mathf.Sin(ang - Mathf.PI*0.5f)*k_groundR2)) cnt++;
                if (cnt == 4) { fallFlag = true; break; }

                // 중력 1 서브스텝분
                m_moveVelocity.y -= gen.gravityAcceleration * dt * (1f / 3f);
                if (m_moveVelocity.y < gen.fallMaxSpeed) m_moveVelocity.y = gen.fallMaxSpeed;
            }

            // 7. 접지 처리 및 경사 미끄러짐
            if (fallFlag)
            {
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
                            m_moveVelocity.x = n.x *  4.0f;
                            m_moveVelocity.y = n.y * -1.667f;
                            m_moveVelocity.z = n.z *  4.0f;

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

            transform.position = pos;
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

            Vector3 v    = pos - posOld;
            float   dist = v.magnitude;
            if (dist < 1e-6f) return false;
            v /= dist;

            // 시작점: inV - v*dist (프레임 시작 시의 체크 포인트)
            // rayStart를 살짝 뒤로 밀고 maxDist를 그만큼 늘려, 경계 위에서 시작하는 경우도 안전하게 면 감지.
            const float k_rayStartMargin = 1e-4f;
            Vector3     rayStart         = inV - v * (dist + k_rayStartMargin);

            if (!block.IntersectRay(rayStart, v, dist + k_rayStartMargin, out int face, out _))
                return false;

            // 면과 이동 벡터의 각도 = acos(dot(v, n)). dot이 음수여야 정면 충돌.
            Vector3 n    = block.faceNormals[face];
            float   dot  = Vector3.Dot(v, n);
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

            float   temp   = per * dist;
            Vector3 newPos = vBlend * temp + posOld;

            // 최종 위치가 여전히 블록 내부면 롤백
            if (block.Contains(newPos)) newPos = posOld;

            // 모드별 Y 보정
            if (mode == 0x01 && newPos.y > posBackup.y) newPos.y = posBackup.y;
            if (mode == 0x02)                            newPos.y = posBackup.y;

            pos = newPos;
            return true;
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

        private void ApplyAcceleration(HumanTypeData type, float dt)
        {
            HumanMoveFlag moveMask = m_moveFlag & (
                HumanMoveFlag.Forward | HumanMoveFlag.Back |
                HumanMoveFlag.Left    | HumanMoveFlag.Right);
            bool walk = (m_moveFlag & HumanMoveFlag.Walk) != 0;

            Vector3 localDir = Vector3.zero;
            float   accel    = 0f;

            if (walk)
            {
                localDir = Vector3.forward;
                accel    = type.progressWalkAcceleration;
            }
            else
            {
                const float k_invSqrt2 = 0.7071068f;
                float runForward  = type.progressRunAcceleration;
                float runSide     = type.sidewaysRunAcceleration;
                float runBack     = type.regressRunAcceleration;
                float diagForward = (runForward + runSide) * 0.5f;

                switch (moveMask)
                {
                    case HumanMoveFlag.Forward:
                        localDir = Vector3.forward;                              accel = runForward;  break;
                    case HumanMoveFlag.Back:
                        localDir = Vector3.back;                                 accel = runBack;     break;
                    case HumanMoveFlag.Left:
                        localDir = Vector3.left;                                 accel = runSide;     break;
                    case HumanMoveFlag.Right:
                        localDir = Vector3.right;                                accel = runSide;     break;
                    case HumanMoveFlag.Forward | HumanMoveFlag.Left:
                        localDir = new Vector3(-k_invSqrt2, 0,  k_invSqrt2);     accel = diagForward; break;
                    case HumanMoveFlag.Forward | HumanMoveFlag.Right:
                        localDir = new Vector3( k_invSqrt2, 0,  k_invSqrt2);     accel = diagForward; break;
                    case HumanMoveFlag.Back | HumanMoveFlag.Left:
                        localDir = new Vector3(-k_invSqrt2, 0, -k_invSqrt2);     accel = runBack;     break;
                    case HumanMoveFlag.Back | HumanMoveFlag.Right:
                        localDir = new Vector3( k_invSqrt2, 0, -k_invSqrt2);     accel = runBack;     break;
                }
            }

            if (accel <= 0f) return;

            Vector3 worldDir = Quaternion.Euler(0, m_rotationX, 0) * localDir;
            m_moveVelocity += worldDir * (accel * dt);
        }

        private void LateUpdate()
        {
            if (!m_human.Alive) return;
            transform.rotation = Quaternion.Euler(0, m_rotationX, 0);

            if (m_humanVisual != null)
                m_humanVisual.SetArmPitch(m_armRotationY);
        }
    }
}
