using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 오브젝트 prefab 의 Collider GameObject 에 부착되어, ObjectColliderData.shapes 를 자식 Collider_{i} 로 빌드하는 컴포넌트.
    /// shape 1 개당 GameObject 1 개를 생성해 인스펙터에서 구분이 쉽도록 한다.
    /// modelScale 은 적용하지 않는다 — JSON 의 size 값이 OpenXOPS decide × 0.013 으로 이미 Unity 단위 등가이기 때문.
    /// </summary>
    public class ObjectCollider : MonoBehaviour
    {
        [SerializeField]
        private Transform colliderRoot;

        // 이 콜라이더들이 소속된 SmallObject. 총알/폭발 피격 시 GetComponentInParent<ObjectCollider>().Owner 로 역참조.
        private SmallObject m_owner;
        public  SmallObject Owner => m_owner;

        /// <summary>
        /// ObjectColliderData.shapes 를 순회해 형상별 Collider 컴포넌트를 자식 Collider_{i} GameObject 에 부착한다.
        /// Capsule 의 shape.size.z 는 direction (0=X, 1=Y, 2=Z) 으로 해석한다.
        /// </summary>
        /// <param name="colliderData">오브젝트 콜라이더 파라미터 데이터.</param>
        /// <param name="owner">콜라이더가 소속된 SmallObject. 총알/폭발 피격 시 역참조용으로 보관한다.</param>
        public void CreateObjectCollider(ObjectColliderData colliderData, SmallObject owner)
        {
            m_owner = owner;

            for (int i = 0; i < colliderData.shapes.Count; i++)
            {
                ColliderShape shape = colliderData.shapes[i];

                var shapeObj = new GameObject($"Collider_{i}");
                shapeObj.transform.SetParent(colliderRoot, false);
                shapeObj.transform.SetLocalPositionAndRotation(shape.center, Quaternion.identity);

                switch (shape.type)
                {
                    case ColliderShapeType.Sphere:
                        var sphere    = shapeObj.AddComponent<SphereCollider>();
                        sphere.radius = shape.size.x;
                        break;

                    case ColliderShapeType.Box:
                        var box  = shapeObj.AddComponent<BoxCollider>();
                        box.size = shape.size;
                        break;

                    case ColliderShapeType.Capsule:
                        var capsule       = shapeObj.AddComponent<CapsuleCollider>();
                        capsule.radius    = shape.size.x;
                        capsule.height    = shape.size.y;
                        capsule.direction = (int)shape.size.z;
                        break;
                }
            }
        }
    }
}
