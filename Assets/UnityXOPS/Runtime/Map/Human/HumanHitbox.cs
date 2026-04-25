using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 총알 충돌 판정용 인간 신체 부위 구분.
    /// 원본 OpenXOPS HitBulletHuman Hit_id (0=머리, 1=상반신, 2=다리).
    /// </summary>
    public enum HumanHitPart
    {
        Head = 0,
        Body = 1,
        Leg  = 2,
    }

    /// <summary>
    /// Human 자식 오브젝트에 부착해 총알 충돌 대상 및 피격 부위를 식별한다.
    /// Collider 형상은 Inspector에서 직접 부착 (CapsuleCollider 권장).
    /// 원본 OpenXOPS: HUMAN_BULLETCOLLISION_* 상수 기반 세 원기둥 (머리/상반신/다리) 구성.
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    public class HumanHitbox : MonoBehaviour
    {
        [SerializeField] private HumanHitPart part;
        [SerializeField] private Human        human;

        public HumanHitPart Part  => part;
        public Human        Human => human;

        /// <summary>
        /// HumanGeneralData의 히트박스 치수를 CapsuleCollider에 적용하고 Y 위치를 누적 높이로 배치한다.
        /// 원본 OpenXOPS: Leg 밑면 = 0, Body 밑면 = legH, Head 밑면 = legH + bodyH (objectmanager.cpp:755-787).
        /// </summary>
        public void ApplySize(HumanGeneralData general)
        {
            var capsule = GetComponent<CapsuleCollider>();
            if (capsule == null || general == null) return;

            float height, radius, baseY;
            switch (part)
            {
                case HumanHitPart.Head:
                    height = general.headHitboxHeight;
                    radius = general.headHitboxRadius;
                    baseY  = general.legHitboxHeight + general.bodyHitboxHeight;
                    break;
                case HumanHitPart.Body:
                    height = general.bodyHitboxHeight;
                    radius = general.bodyHitboxRadius;
                    baseY  = general.legHitboxHeight;
                    break;
                case HumanHitPart.Leg:
                    height = general.legHitboxHeight;
                    radius = general.legHitboxRadius;
                    baseY  = 0f;
                    break;
                default: return;
            }

            capsule.direction       = 1;
            capsule.height          = height;
            capsule.radius          = radius;
            capsule.center          = Vector3.zero;
            transform.localPosition = new Vector3(0f, baseY + height * 0.5f, 0f);
        }
    }
}
