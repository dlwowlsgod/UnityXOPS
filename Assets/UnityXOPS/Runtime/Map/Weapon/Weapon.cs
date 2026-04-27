using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// Human 슬롯에 장착되는 무기의 데이터/동적 상태와 시각 표현을 보관하는 컴포넌트.
    /// 1차 골격: 발사/장전 로직은 미포함. WeaponData/WeaponModelData 와 슬롯 상태 필드만 보유.
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        [SerializeField]
        private WeaponVisual weaponVisual;
        public WeaponVisual WeaponVisual => weaponVisual;

        private int             m_weaponIndex;
        private WeaponData      m_weaponData;
        private WeaponModelData m_weaponModelData;
        public  int             WeaponIndex     => m_weaponIndex;
        public  WeaponData      WeaponData      => m_weaponData;
        public  WeaponModelData WeaponModelData => m_weaponModelData;

        private int   m_currentMagazine;
        private int   m_reserveAmmo;
        private bool  m_isReloading;
        public  int   CurrentMagazine => m_currentMagazine;
        public  int   ReserveAmmo     => m_reserveAmmo;
        public  bool  IsReloading     => m_isReloading;

        private bool  m_isFalling;
        private float m_velocityX;
        private float m_velocityY;
        private float m_velocityZ;
        public  bool  IsFalling => m_isFalling;

        /// <summary>
        /// weaponIndex 로 WeaponData/WeaponModelData 를 조회해 무기 데이터/탄약/시각/스케일을 초기화한다.
        /// 인덱스가 유효 범위 밖이면 noneWeaponIndex(맨손 취급) 로 폴백한다 — null 슬롯은 만들지 않는다.
        /// localPosition/localRotation 은 건드리지 않으므로, 부착 시 OnEquip() 을 호출하거나 떨어진 무기 spawner 가 직접 설정해야 한다.
        /// </summary>
        /// <param name="weaponIndex">WeaponParameterData.weaponData 인덱스.</param>
        /// <param name="totalBullets">PD1.p3 가 지정한 총탄수(장전탄 포함). 0 미만이면 magazineSize 로 가득 채움.</param>
        /// <param name="dropped">true=맵에 떨어진 모드(부모 weaponRoot, scale 자기 책임), false=Human 부착 모드(부모 weaponAttachRoot 가 weaponScale 강제).</param>
        public void CreateWeapon(int weaponIndex, int totalBullets = -1, bool dropped = false)
        {
            var wp = DataManager.Instance.WeaponParameterData;
            if (weaponIndex < 0 || weaponIndex >= wp.weaponData.Count)
            {
                weaponIndex = wp.weaponGeneralData.noneWeaponIndex;
            }

            m_weaponIndex = weaponIndex;
            m_weaponData  = wp.weaponData[weaponIndex];

            int modelIndex = m_weaponData.modelIndex;
            if (modelIndex >= 0 && modelIndex < wp.weaponModelData.Count)
            {
                m_weaponModelData = wp.weaponModelData[modelIndex];
            }

            // OpenXOPS object.cpp:2383-2408 RunReload 분배 환산: magazine = min(total, mag), reserve = max(0, total - mag).
            int magazineSize = m_weaponData.magazineSize;
            if (totalBullets < 0)
            {
                m_currentMagazine = magazineSize;
                m_reserveAmmo     = 0;
            }
            else
            {
                m_currentMagazine = Mathf.Min(totalBullets, magazineSize);
                m_reserveAmmo     = Mathf.Max(0, totalBullets - magazineSize);
            }
            m_isReloading = false;

            if (m_weaponModelData != null)
            {
                weaponVisual.CreateWeaponVisual(m_weaponData, m_weaponModelData);
            }

            // 시각 스케일은 weaponVisual.visualRoot 책임. Weapon Root 의 localScale 은 prefab 기본값 유지.
            // 부착 모드: 부모(weaponAttachRoot) worldScale 이 weaponScale 강제 → visualRoot.localScale = size 만.
            // 떨어진 모드: 부모(weaponRoot) localScale = 1 → visualRoot.localScale 에 weaponScale 까지 곱.
            float scaleFactor = dropped ? wp.weaponGeneralData.weaponScale : 1f;
            weaponVisual.SetVisualScale(m_weaponData.size * scaleFactor);
        }

        /// <summary>
        /// Human 슬롯에 부착될 때 호출. WeaponData.position 을 적용하고 Weapon Root 회전을 identity 로 reset 한다.
        /// visualRoot 의 prefab 기본 Y180° 보정은 그대로 두어 weaponAttachRoot 의 Y180° 와 누적 상쇄되도록 한다.
        /// </summary>
        public void OnEquip()
        {
            transform.localPosition = m_weaponData.position;
            transform.localRotation = Quaternion.identity;
            m_isFalling = false;
            m_velocityX = 0f;
            m_velocityY = 0f;
            m_velocityZ = 0f;
        }

        /// <summary>
        /// 맵에 떨어진 모드로 배치될 때 호출. Weapon Root 회전을 yaw + Z+90° 모델축 보정으로 설정하고,
        /// visualRoot 의 prefab 기본 Y180° 보정을 identity 로 무효화한다 (떨어진 모드에선 누적 상쇄가 없어 정면 반전 유발).
        /// localPosition 은 spawner 가 사전에 설정해야 한다.
        /// </summary>
        /// <param name="yawDeg">PD1.r 에서 변환된 World Y 축 yaw (degree). PointData.cs 가 좌표계 보정(+180°) 을 이미 적용한 값.</param>
        /// <param name="horizontalVelocity">초기 수평 속도 (m/s). PD1 spawn 은 Vector3.zero, Dump 시는 사람 forward * dropoffHorizontalSpeed.</param>
        public void OnDrop(float yawDeg, Vector3 horizontalVelocity)
        {
            transform.localRotation = Quaternion.Euler(0f, yawDeg, 0f) * Quaternion.Euler(0f, 0f, 90f);
            weaponVisual.SetVisualRotation(Quaternion.identity);
            m_isFalling = true;
            m_velocityX = horizontalVelocity.x;
            m_velocityY = 0f;
            m_velocityZ = horizontalVelocity.z;
        }

        private void Update()
        {
            if (!m_isFalling) return;

            var gen = DataManager.Instance.WeaponParameterData.weaponGeneralData;
            float dt = Time.deltaTime;

            // 수평 감쇠 — OpenXOPS object.cpp:2459-2460 (move_x/z *= 0.96 per frame). dt 가변 변환: pow(damping, dt).
            float damping = Mathf.Pow(gen.horizontalDampingPerSec, dt);
            m_velocityX *= damping;
            m_velocityZ *= damping;

            // 수평 정지 임계 — OpenXOPS object.cpp:2475-2479.
            float horizSpeed = Mathf.Sqrt(m_velocityX * m_velocityX + m_velocityZ * m_velocityZ);
            if (horizSpeed < gen.horizontalStopThreshold)
            {
                m_velocityX = 0f;
                m_velocityZ = 0f;
                horizSpeed  = 0f;
            }

            // 수평 이동 — Block 충돌 시 면 앞 margin 만큼 정지.
            if (horizSpeed > 0f)
            {
                Vector3 horizDir  = new Vector3(m_velocityX, 0f, m_velocityZ) / horizSpeed;
                float   horizDist = horizSpeed * dt;
                if (MapLoader.RaycastBlock(transform.position, horizDir, horizDist + gen.groundCollisionMargin, out float hitDist))
                {
                    transform.position += horizDir * Mathf.Max(0f, hitDist - gen.groundCollisionMargin);
                    m_velocityX = 0f;
                    m_velocityZ = 0f;
                }
                else
                {
                    transform.position += horizDir * horizDist;
                }
            }

            // 수직 중력
            m_velocityY -= gen.gravity * dt;
            if (m_velocityY < gen.terminalVelocityY) m_velocityY = gen.terminalVelocityY;

            float moveY = m_velocityY * dt;

            // Block 레이어와 아래쪽 raycast — moveY 만큼 이동했을 때 충돌하면 면 위 margin 위치에서 정지.
            if (moveY < 0f)
            {
                float castDist = -moveY + gen.groundCollisionMargin;
                if (MapLoader.RaycastBlock(transform.position, Vector3.down, castDist, out float hitDist))
                {
                    transform.position += Vector3.down * (hitDist - gen.groundCollisionMargin);
                    m_velocityY = 0f;
                    m_isFalling = false;
                    return;
                }
            }

            transform.position += Vector3.up * moveY;

            // deadlineY 아래로 추락 시 그 높이에서 정지 (원본에 없는 안전장치).
            if (transform.position.y < gen.deadlineY)
            {
                Vector3 pos = transform.position;
                pos.y = gen.deadlineY;
                transform.position = pos;
                m_velocityY = 0f;
                m_isFalling = false;
            }
        }
    }
}
