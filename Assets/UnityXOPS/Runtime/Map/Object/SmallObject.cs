using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// OpenXOPS smallobject 의 UnityXOPS 포팅 컴포넌트.
    /// ObjectData/ObjectModelData/ObjectColliderData 를 인덱스로 조회해 Visual/Collider 자식 컴포넌트에 위임 빌드한다.
    /// 1차 골격: HP/HitBullet 진입점만 보유. Destruction 회전·점프 애니메이션은 미포함.
    /// </summary>
    public class SmallObject : MonoBehaviour
    {
        [SerializeField]
        private ObjectVisual   objectVisual;
        [SerializeField]
        private ObjectCollider objectCollider;
        public ObjectVisual   ObjectVisual   => objectVisual;
        public ObjectCollider ObjectCollider => objectCollider;

        private int                m_objectIndex;
        private ObjectData         m_objectData;
        private ObjectModelData    m_objectModelData;
        private ObjectColliderData m_objectColliderData;
        public  int                ObjectIndex        => m_objectIndex;
        public  ObjectData         ObjectData         => m_objectData;
        public  ObjectModelData    ObjectModelData    => m_objectModelData;
        public  ObjectColliderData ObjectColliderData => m_objectColliderData;

        private float m_hp;
        private bool  m_isDestroyed;
        private int   m_identifier;
        public  float Hp           => m_hp;
        public  bool  IsDestroyed  => m_isDestroyed;
        public  int   Identifier   => m_identifier;

        // 파괴 점프 — 원본 smallobject::Destruction/ProcessObject (object.cpp:2684-2751) 의 프레임 기반 물리를 시간 기반으로 변환.
        // 원본: move_rx=jump×0.04243(수평/frame), pos_y+=jump_cnt×0.1(수직, jump_cnt 매 프레임 -1 → 등가속 낙하), 34프레임(~1.02s) 후 소멸. 바운스/맵충돌 없음.
        // 좌표 ×0.1, 33.333fps 환산: 수평/초기상승 = ×0.1×33.333, 중력 = ×0.1×33.333².
        private const float k_destroyFps          = 33.3333f;
        private const float k_destroyHorizPerJump = 0.04243f * 0.1f * k_destroyFps;             // jump 당 수평 속도 (m/s)
        private const float k_destroyVertPerJump  = 0.1f     * 0.1f * k_destroyFps;             // jump 당 초기 상승 속도 (m/s)
        private const float k_destroyGravity      = 0.1f     * 0.1f * k_destroyFps * k_destroyFps; // 중력 (m/s²)
        private const float k_destroyLifetime     = 34f / k_destroyFps;                         // 소멸 시간 (원본 cnt > 33)

        private Vector3 m_destroyVelocity;
        private Vector3 m_destroyAngularVel;  // (x, y) deg/s
        private float   m_destroyTimer;

        /// <summary>
        /// objectIndex 로 ObjectData/ObjectModelData/ObjectColliderData 를 조회해 Visual/Collider 컴포넌트를 빌드하고 HP 를 초기화한다.
        /// modelScale 은 Visual root 에만 적용 — Collider 는 OpenXOPS decide 원본 단위와 일치하도록 그대로 둔다.
        /// identifier 는 OpenXOPS PD1 p4 매핑 — MIF 이벤트의 SearchSmallobject 검색용. UnityXOPS 이벤트 시스템 미구현 상태에서는 보존만 함.
        /// </summary>
        /// <param name="objectIndex">ObjectParameterData.objectData 인덱스.</param>
        /// <param name="identifier">PD1 param3 식별 ID. 미사용 시 0.</param>
        public void CreateObject(int objectIndex, int identifier = 0)
        {
            var op = DataManager.Instance.ObjectParameterData;
            if (objectIndex < 0 || objectIndex >= op.objectData.Count)
            {
                Debug.LogError($"SmallObject.CreateObject: objectIndex {objectIndex} 범위 초과 ({op.objectData.Count})");
                return;
            }

            m_objectIndex = objectIndex;
            m_objectData  = op.objectData[objectIndex];
            m_identifier  = identifier;

            int modelIndex = m_objectData.modelIndex;
            if (modelIndex >= 0 && modelIndex < op.objectModelData.Count)
            {
                m_objectModelData = op.objectModelData[modelIndex];
            }

            int colliderIndex = m_objectData.colliderIndex;
            if (colliderIndex >= 0 && colliderIndex < op.objectColliderData.Count)
            {
                m_objectColliderData = op.objectColliderData[colliderIndex];
            }

            m_hp          = m_objectData.hp;
            m_isDestroyed = false;

            if (m_objectModelData != null)
            {
                objectVisual.CreateObjectVisual(m_objectModelData);
                objectVisual.SetVisualScale(op.objectGeneralData.modelScale);
            }
            if (m_objectColliderData != null)
            {
                objectCollider.CreateObjectCollider(m_objectColliderData, this);
            }
        }

        /// <summary>
        /// OpenXOPS object.cpp:2633-2658 매핑. 자기 위치 +ε 에서 아래로 ray 를 쏘아 첫 블록 위에 정착시킨다.
        /// 다중 콜라이더 중 local y 최저점을 가진 shape 를 기준으로 root.y 를 조정 — 가장 낮은 콜라이더 바닥이 ground 와 일치하게.
        /// 가장 낮은 shape 가 Sphere 일 때만 size.x × (3/13) 만큼 ground 아래로 묻히게 보정 (OpenXOPS decide/10 의 Unity 단위 환산).
        /// Box/Capsule 또는 콜라이더 정보가 없으면 보정 없이 정확 정렬.
        /// </summary>
        /// <returns>ray 가 블록을 hit 해 위치 보정이 적용됐으면 true.</returns>
        public bool SnapToGround()
        {
            // OpenXOPS COLLISION_ADDSIZE 0.01f × 0.1 = 0.001f, 1000.0f × 0.1 = 100f.
            const float k_snapEpsilon     = 0.001f;
            const float k_snapMaxDistance = 100f;

            Vector3 origin = transform.position + Vector3.up * k_snapEpsilon;
            if (!MapLoader.RaycastBlock(origin, Vector3.down, k_snapMaxDistance + k_snapEpsilon, out float hitDist))
            {
                return false;
            }

            float groundY = origin.y - hitDist;

            // 다중 콜라이더 중 local y 최저점을 가진 shape 를 찾는다. shape 회전은 identity, colliderRoot Y -180° 회전은 y 축에 영향 없음.
            float         lowestBottomY = 0f;
            ColliderShape lowestShape   = null;

            if (m_objectColliderData != null && m_objectColliderData.shapes != null)
            {
                for (int i = 0; i < m_objectColliderData.shapes.Count; i++)
                {
                    ColliderShape shape   = m_objectColliderData.shapes[i];
                    float         bottomY = shape.center.y - GetShapeHalfExtentY(shape);
                    if (lowestShape == null || bottomY < lowestBottomY)
                    {
                        lowestBottomY = bottomY;
                        lowestShape   = shape;
                    }
                }
            }

            // 가장 낮은 shape 가 Sphere 면 OpenXOPS decide/10 묻힘 보정 적용.
            // Unity sphere radius = decide × 0.013, 보정 = decide × 0.01 = radius × (10/13). 이 중 radius - 보정 = radius × (3/13) 만큼 ground 아래로 묻힘.
            float sinkOffset = 0f;
            if (lowestShape != null && lowestShape.type == ColliderShapeType.Sphere)
            {
                sinkOffset = lowestShape.size.x * (3f / 13f);
            }

            // 가장 낮은 shape 의 bottom (world y) = groundY - sinkOffset → root.y = groundY - lowestBottomY - sinkOffset.
            Vector3 pos = transform.position;
            pos.y = groundY - lowestBottomY - sinkOffset;
            transform.position = pos;
            return true;
        }

        /// <summary>
        /// shape 의 local y 절반 크기 (center.y 에서 bottom 까지 거리) 를 반환.
        /// Capsule 은 size.z(direction) 가 1=Y 축이고 height >= 2*radius 일 때만 height/2, 그 외엔 radius 로 처리 (Unity CapsuleCollider 와 일치).
        /// </summary>
        private static float GetShapeHalfExtentY(ColliderShape shape)
        {
            switch (shape.type)
            {
                case ColliderShapeType.Sphere:
                    return shape.size.x;

                case ColliderShapeType.Box:
                    return shape.size.y * 0.5f;

                case ColliderShapeType.Capsule:
                    int   direction = (int)shape.size.z;
                    float radius    = shape.size.x;
                    float height    = shape.size.y;
                    if (direction == 1)
                    {
                        return Mathf.Max(height, 2f * radius) * 0.5f;
                    }
                    return radius;

                default:
                    return 0f;
            }
        }

        /// <summary>
        /// 총탄 피격 진입점. OpenXOPS object.cpp:2663-2669 미러.
        /// 호출자가 floor(attacks × 0.25) (objectmanager.cpp:835) 등 데미지 보정 후 전달해야 한다.
        /// </summary>
        /// <param name="attacks">감산할 데미지.</param>
        public void HitBullet(float attacks) => TakeDamage(attacks);

        /// <summary>
        /// 수류탄 폭발 피격 진입점. OpenXOPS object.cpp:2674-2680 미러.
        /// 호출자(Bullet.Explode)가 거리 감쇠 + 벽 차폐를 적용한 데미지를 전달한다.
        /// </summary>
        /// <param name="damage">감산할 데미지.</param>
        public void HitGrenadeExplosion(float damage) => TakeDamage(damage);

        /// <summary>
        /// hp 차감 후 0 이하면 파괴(점프) 진입. 원본 object.cpp:2663-2680 의 공통 패턴 (hp -= attacks; if(hp<=0) Destruction()).
        /// </summary>
        private void TakeDamage(float amount)
        {
            if (m_isDestroyed) return;

            m_hp -= amount;
            if (m_hp <= 0f)
            {
                m_hp = 0f;
                StartDestruction();
            }
        }

        /// <summary>
        /// 파괴 진입 — 원본 smallobject::Destruction (object.cpp:2684-2710). jump 값으로 무작위 방향 점프 + 회전 시작.
        /// 방향 0~350°(10° 단위), 수평속도 jump×0.04243, 초기 상승속도 jump×0.1(/frame), 회전 0~19°/frame.
        /// </summary>
        private void StartDestruction()
        {
            m_isDestroyed = true;

            int jump = m_objectData != null ? m_objectData.jump : 0;

            float dirRad = UnityEngine.Random.Range(0, 36) * 10f * Mathf.Deg2Rad;  // 원본 10° × GetRand(36)
            float horiz  = jump * k_destroyHorizPerJump;
            float vert   = jump * k_destroyVertPerJump;

            // 원본: pos_x += cos(jump_rx)*move_rx, pos_z += sin(jump_rx)*move_rx
            m_destroyVelocity = new Vector3(Mathf.Cos(dirRad) * horiz, vert, Mathf.Sin(dirRad) * horiz);

            // 원본 add_rx/add_ry = 1° × GetRand(20) (per frame) → deg/s
            m_destroyAngularVel = new Vector3(
                UnityEngine.Random.Range(0, 20) * k_destroyFps,
                UnityEngine.Random.Range(0, 20) * k_destroyFps,
                0f);

            m_destroyTimer = k_destroyLifetime;
        }

        /// <summary>
        /// 파괴 점프 물리 진행 — 원본 smallobject::ProcessObject (object.cpp:2713-2751) 의 시간 기반 변환.
        /// 등가속 포물선 + 회전 누적, 맵 충돌/바운스 없음, 수명 종료 시 비활성화 (원본 EnableFlag=false).
        /// </summary>
        private void Update()
        {
            if (!m_isDestroyed) return;

            float dt = Time.deltaTime;

            m_destroyVelocity.y -= k_destroyGravity * dt;
            transform.position  += m_destroyVelocity * dt;
            transform.Rotate(m_destroyAngularVel.x * dt, m_destroyAngularVel.y * dt, 0f, Space.Self);

            m_destroyTimer -= dt;
            if (m_destroyTimer <= 0f)
                gameObject.SetActive(false);
        }
    }
}
