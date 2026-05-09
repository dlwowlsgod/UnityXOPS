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
                objectCollider.CreateObjectCollider(m_objectColliderData);
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
        /// 호출자가 attacks * 0.25f (objectmanager.cpp:835) 등 데미지 보정 후 전달해야 한다.
        /// hp 가 0 이하가 되면 m_isDestroyed=true 로 표시만 함 — 회전·점프 애니메이션과 비활성화는 추후 구현.
        /// </summary>
        /// <param name="attacks">감산할 데미지.</param>
        public void HitBullet(float attacks)
        {
            if (m_isDestroyed) return;

            m_hp -= attacks;
            if (m_hp <= 0f)
            {
                m_hp          = 0f;
                m_isDestroyed = true;
                // TODO: Destruction 진입 — jump_cnt/move_rx/add_rx,ry 초기화 + ProcessObject 시작.
            }
        }
    }
}
