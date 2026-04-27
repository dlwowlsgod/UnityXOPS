using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 게임 맵에 배치된 인간 캐릭터의 데이터와 시각 표현을 관리하는 컴포넌트.
    /// </summary>
    public class Human : MonoBehaviour
    {
        [SerializeField]
        private float hp;
        public float HP => hp;

        [SerializeField]
        private int team;
        public int Team => team;

        [SerializeField]
        private bool alive;
        public bool Alive => alive;

        [SerializeField]
        private Transform cameraRoot;
        public Transform CameraRoot => cameraRoot;

        [SerializeField]
        private HumanVisual humanVisual;
        public HumanVisual HumanVisual => humanVisual;

        [SerializeField] private HumanHitbox headHitbox;
        [SerializeField] private HumanHitbox bodyHitbox;
        [SerializeField] private HumanHitbox legHitbox;

        private HumanData m_humanData;
        private HumanTypeData m_humanTypeData;
        public HumanTypeData HumanTypeData => m_humanTypeData;

        private RawPointData m_humanParam, m_humanDataParam;

        private Weapon[] m_weapons = new Weapon[2];
        private int      m_selectWeapon;
        private float    m_selectWeaponCnt;
        private float    m_reloadingCnt;
        public  Weapon   CurrentWeapon => m_weapons[m_selectWeapon];
        public  int      SelectWeapon  => m_selectWeapon;
        public  bool     IsChanging    => m_selectWeaponCnt > 0f || m_reloadingCnt > 0f;

        /// <summary>
        /// 포인트 데이터와 파라미터로부터 인간 캐릭터를 생성 및 초기화한다.
        /// </summary>
        /// <param name="humanParam">인간 배치 포인트 데이터.</param>
        /// <param name="humanDataParam">인간 파라미터 포인트 데이터.</param>
        public void CreateHuman(RawPointData humanParam, RawPointData humanDataParam)
        {
            m_humanParam = humanParam;
            m_humanDataParam = humanDataParam;

            var humanParamData = DataManager.Instance.HumanParameterData;
            int humanIndex = m_humanDataParam.param1;
            if (humanIndex >= 0 && humanIndex < humanParamData.humanData.Count)
            {
                m_humanData = humanParamData.humanData[humanIndex];

                int typeIndex = m_humanData.typeIndex;
                if (typeIndex >= 0 && typeIndex < humanParamData.humanTypeData.Count)
                {
                    m_humanTypeData = humanParamData.humanTypeData[typeIndex];
                }
            }

            humanVisual.CreateHumanVisual(m_humanData);

            var general = humanParamData.humanGeneralData;
            if (headHitbox != null) headHitbox.ApplySize(general);
            if (bodyHitbox != null) bodyHitbox.ApplySize(general);
            if (legHitbox  != null) legHitbox .ApplySize(general);

            float cameraAttachPosition = general.cameraAttachPosition;
            cameraRoot.localPosition = new Vector3(0, cameraAttachPosition, 0);

            EquipInitialWeapons();

            hp = m_humanData.hp;
            team = m_humanDataParam.param2;
            alive = hp > 0;
        }

        /// <summary>
        /// 슬롯 0/1 사이에서 활성 무기를 전환한다. 비활성 슬롯의 weapon GameObject 는 SetActive(false).
        /// 전환 중(IsChanging) 이거나 같은 슬롯이면 무시. 전환 완료 후 slotChangeTime 동안 IsChanging 락 + dynamicArm reaction 복원.
        /// 원본 OpenXOPS human::ChangeHaveWeapon (object.cpp:454-504) 대응.
        /// </summary>
        /// <param name="slot">활성화할 슬롯 인덱스 (0 또는 1).</param>
        public void SetSelectWeapon(int slot)
        {
            slot = Mathf.Clamp(slot, 0, 1);
            if (IsChanging || slot == m_selectWeapon) return;

            m_selectWeapon = slot;

            if (m_weapons[0] != null) m_weapons[0].gameObject.SetActive(slot == 0);
            if (m_weapons[1] != null) m_weapons[1].gameObject.SetActive(slot == 1);

            humanVisual.ApplyWeaponVisual(m_weapons[m_selectWeapon]);

            float duration = m_weapons[m_selectWeapon].WeaponData.slotChangeTime;
            float startDeg = DataManager.Instance.HumanParameterData.humanGeneralData.armAngleReloading;
            m_selectWeaponCnt = duration;
            humanVisual.BeginArmReaction(startDeg, duration);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (m_selectWeaponCnt > 0f) m_selectWeaponCnt -= dt;
            if (m_reloadingCnt    > 0f) m_reloadingCnt    -= dt;
            humanVisual.TickArmReaction(dt);
            TryPickupWeapon();
        }

        /// <summary>
        /// HumanData.weaponIndex0/1 을 사용해 슬롯 2 개에 weapon prefab 을 인스턴스화하고 슬롯 0 을 활성화한다.
        /// noneWeaponIndex 또는 무효 인덱스도 정상 폴백되어 두 슬롯 모두 항상 weapon GameObject 를 가진다.
        /// </summary>
        private void EquipInitialWeapons()
        {
            var weaponParam = DataManager.Instance.WeaponParameterData;
            humanVisual.ApplyWeaponAttachScale(weaponParam.weaponGeneralData.weaponScale);

            int weaponIndex0 = m_humanData.weaponIndex0;
            int weaponIndex1 = m_humanData.weaponIndex1;

            // POINT_P1TYPE_HUMAN2(param0=6): 주 무기 슬롯을 noneWeapon 으로 비운 채 스폰.
            // 원본 objectmanager.cpp:230-244 가 Weapon[1] 가상무기 생성을 스킵하는 동작에 대응.
            if (m_humanParam.param0 == 6)
            {
                weaponIndex1 = weaponParam.weaponGeneralData.noneWeaponIndex;
            }

            m_weapons[0] = InstantiateWeapon(weaponIndex0);
            m_weapons[1] = InstantiateWeapon(weaponIndex1);

            // 원본 OpenXOPS: 슬롯 1(주 무기) 이 게임 시작 시 활성화
            m_selectWeapon = 1;
            m_weapons[0].gameObject.SetActive(false);
            m_weapons[1].gameObject.SetActive(true);

            humanVisual.ApplyWeaponVisual(m_weapons[m_selectWeapon]);
        }

        /// <summary>
        /// MapLoader.WeaponPrefab 을 Instantiate 하고 Weapon.CreateWeapon 으로 초기화한 뒤
        /// WeaponData.fixWeapon 에 따라 fixed/dynamicWeaponAttachRoot 중 하나로 부모를 설정한다.
        /// </summary>
        /// <param name="weaponIndex">WeaponParameterData.weaponData 인덱스. 무효면 Weapon 측이 noneWeaponIndex 로 폴백.</param>
        /// <param name="totalBullets">총탄수(매거진+예비). 0 미만이면 magazineSize 로 가득 채움.</param>
        private Weapon InstantiateWeapon(int weaponIndex, int totalBullets = -1)
        {
            GameObject instance = Instantiate(MapLoader.Instance.WeaponPrefab);
            Weapon weapon = instance.GetComponent<Weapon>();
            weapon.CreateWeapon(weaponIndex, totalBullets);

            Transform parent = weapon.WeaponData.fixWeapon
                ? humanVisual.FixedWeaponAttachRoot
                : humanVisual.DynamicWeaponAttachRoot;
            instance.transform.SetParent(parent, false);

            weapon.OnEquip();
            return weapon;
        }

        /// <summary>
        /// 현재 슬롯 weapon 의 previousWeaponIndex 로 같은 슬롯 내에서 무기 ID 전환 (예: GLOCK SEMI → FULL).
        /// </summary>
        public void SwitchWeaponPrevious()
        {
            Weapon current = m_weapons[m_selectWeapon];
            if (current == null) return;
            SwitchWeaponID(current.WeaponData.previousWeaponIndex);
        }

        /// <summary>
        /// 현재 슬롯 weapon 의 nextWeaponIndex 로 같은 슬롯 내에서 무기 ID 전환.
        /// </summary>
        public void SwitchWeaponNext()
        {
            Weapon current = m_weapons[m_selectWeapon];
            if (current == null) return;
            SwitchWeaponID(current.WeaponData.nextWeaponIndex);
        }

        /// <summary>
        /// 같은 슬롯 안에서 무기 인덱스를 targetIndex 로 교체. ammo 그대로 이전, switchTime 만큼 IsChanging 락 + arm reaction.
        /// 원본 OpenXOPS human::ChangeWeaponID (object.cpp:516-580) 대응. selectweapon 슬롯/슬롯 자체는 변경 X.
        /// </summary>
        private void SwitchWeaponID(int targetIndex)
        {
            if (IsChanging) return;
            if (targetIndex < 0) return;

            Weapon current = m_weapons[m_selectWeapon];
            if (current == null || targetIndex == current.WeaponIndex) return;

            int totalBullets = current.CurrentMagazine + current.ReserveAmmo;
            Destroy(current.gameObject);

            Weapon w = InstantiateWeapon(targetIndex, totalBullets);
            m_weapons[m_selectWeapon] = w;
            w.gameObject.SetActive(true);
            humanVisual.ApplyWeaponVisual(w);

            float duration = w.WeaponData.switchTime;
            float holdDeg  = DataManager.Instance.HumanParameterData.humanGeneralData.armAngleReloading;
            m_reloadingCnt = duration;
            humanVisual.BeginArmReactionHold(holdDeg, duration);
        }

        /// <summary>
        /// 현재 슬롯의 무기를 사람 전방으로 던지고 슬롯을 noneWeapon 으로 교체한다.
        /// 원본 OpenXOPS human::DumpWeapon (object.cpp:787-816) + weapon::Dropoff (object.cpp:2308-2322) 대응.
        /// noneWeapon 자체는 못 버림. selectweaponcnt 락 없음, 팔 reaction 없음 — 즉시 다른 슬롯 전환 가능.
        /// </summary>
        public void DropCurrentWeapon()
        {
            if (IsChanging) return;

            var weaponParam = DataManager.Instance.WeaponParameterData;
            int noneIdx     = weaponParam.weaponGeneralData.noneWeaponIndex;
            Weapon current  = m_weapons[m_selectWeapon];
            if (current == null || current.WeaponIndex == noneIdx) return;

            int idx          = current.WeaponIndex;
            int totalBullets = current.CurrentMagazine + current.ReserveAmmo;
            Destroy(current.gameObject);

            // 슬롯에 noneWeapon 즉시 부착.
            Weapon empty = InstantiateWeapon(noneIdx);
            m_weapons[m_selectWeapon] = empty;
            empty.gameObject.SetActive(true);
            humanVisual.ApplyWeaponVisual(empty);

            // 떨어진 무기 spawn — 사람 전방 0.5m + 위 1.6m 에 놓고 forward 로 dropoffHorizontalSpeed 던진다.
            var gen = weaponParam.weaponGeneralData;
            Vector3 forward = transform.forward;
            Vector3 spawnPos = transform.position + forward * 0.5f + Vector3.up * 1.6f;

            GameObject droppedObj = Instantiate(MapLoader.Instance.WeaponPrefab, MapLoader.Instance.WeaponRoot);
            droppedObj.transform.position = spawnPos;
            Weapon dropped = droppedObj.GetComponent<Weapon>();
            dropped.CreateWeapon(idx, totalBullets, dropped: true);
            // 모델 yaw: 사람 yaw + 180° (OpenXOPS rotation_x + π — 무기가 사람 반대 방향 향함).
            float weaponYaw = transform.eulerAngles.y + 180f;
            dropped.OnDrop(weaponYaw, forward * gen.dropoffHorizontalSpeed);
        }

        /// <summary>
        /// 매 프레임 weaponRoot 의 떨어진 무기들과 픽업 거리 판정. 현재 슬롯이 noneWeapon 인 경우만 줍는다.
        /// 원본 OpenXOPS ObjectManager::PickupWeapon (objectmanager.cpp:1332-1371) + human::PickupWeapon (object.cpp:422-449) 대응.
        /// 거리: HUMAN_PICKUPWEAPON_R/L/H → HumanGeneralData.weaponPickupRadius / weaponPickupVerticalRange.
        /// </summary>
        private void TryPickupWeapon()
        {
            if (IsChanging) return;
            if (m_humanTypeData != null && !m_humanTypeData.canPickupWeapon) return;

            var weaponParam = DataManager.Instance.WeaponParameterData;
            int noneIdx = weaponParam.weaponGeneralData.noneWeaponIndex;
            Weapon current = m_weapons[m_selectWeapon];
            if (current == null || current.WeaponIndex != noneIdx) return;

            var general    = DataManager.Instance.HumanParameterData.humanGeneralData;
            float radius   = general.weaponPickupRadius;
            float yMin     = general.weaponPickupVerticalRange.min;
            float yMax     = general.weaponPickupVerticalRange.max;
            float radiusSq = radius * radius;

            Vector3   humanPos    = transform.position;
            Transform weaponRoot  = MapLoader.Instance.WeaponRoot;
            if (weaponRoot == null) return;

            for (int i = 0; i < weaponRoot.childCount; i++)
            {
                Transform child = weaponRoot.GetChild(i);
                Weapon dropped  = child.GetComponent<Weapon>();
                if (dropped == null) continue;

                Vector3 wPos = child.position;
                float dy     = wPos.y - humanPos.y;
                if (dy < yMin || dy > yMax) continue;

                float dx = wPos.x - humanPos.x;
                float dz = wPos.z - humanPos.z;
                if (dx * dx + dz * dz >= radiusSq) continue;

                PickupDroppedWeapon(dropped);
                return;
            }
        }

        /// <summary>
        /// 떨어진 무기를 현재 슬롯의 noneWeapon 자리에 인스턴스 교체로 부착하고 slotChangeTime 락/팔 reaction 시작.
        /// 떨어진 무기의 ammo (매거진+예비) 그대로 이전. 떨어진 weapon GameObject 제거.
        /// </summary>
        private void PickupDroppedWeapon(Weapon dropped)
        {
            int idx          = dropped.WeaponIndex;
            int totalBullets = dropped.CurrentMagazine + dropped.ReserveAmmo;
            Destroy(dropped.gameObject);

            Weapon old = m_weapons[m_selectWeapon];
            if (old != null) Destroy(old.gameObject);

            Weapon w = InstantiateWeapon(idx, totalBullets);
            m_weapons[m_selectWeapon] = w;
            w.gameObject.SetActive(true);

            humanVisual.ApplyWeaponVisual(w);

            float duration = w.WeaponData.slotChangeTime;
            float startDeg = DataManager.Instance.HumanParameterData.humanGeneralData.armAngleReloading;
            m_selectWeaponCnt = duration;
            humanVisual.BeginArmReaction(startDeg, duration);
        }
    }
}
