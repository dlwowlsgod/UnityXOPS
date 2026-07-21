using UnityEngine;

namespace UnityXOPS
{
    // Human 의 무기 슬롯 라이프사이클(장착/전환/드롭/픽업/재장전) 담당 partial.
    public partial class Human
    {
        private Weapon[] m_weapons = new Weapon[2];
        private int m_selectWeapon;
        private float m_selectWeaponCnt;
        private float m_reloadingCnt;

        // 사망 시 무기 드롭 속도. 원본 OpenXOPS object.cpp:1232 Dropoff(..., 1.5f) × 0.1 = 0.15 m/s.
        // DumpWeapon (수동 버리기, dropoffHorizontalSpeed JSON ≈ 1.63 × 0.1) 보다 약간 낮음.
        private const float k_deathDropHorizontalSpeed = 0.15f;
        public Weapon CurrentWeapon => m_weapons[m_selectWeapon];
        public int SelectWeapon => m_selectWeapon;

        // OpenXOPS 컨벤션: 슬롯 0=보조(SUB), 슬롯 1=주(MAIN). EquipInitialWeapons 의 m_selectWeapon=1 초기화 근거.
        public Weapon MainWeapon => m_weapons[1];
        public Weapon SubWeapon => m_weapons[0];

        // 발사/재장전/전환 가드용. UI 측은 IsSwitchingWeapon / IsReloading 으로 분리해서 표시.
        public bool IsSwitchingWeapon => m_selectWeaponCnt > 0f;
        public bool IsReloading => m_reloadingCnt > 0f;
        public bool IsChanging => IsSwitchingWeapon || IsReloading;

        /// <summary>
        /// 슬롯 인덱스(0/1) 로 무기 인스턴스를 조회. UI HUD 등에서 비활성 슬롯 무기 정보가 필요할 때 사용.
        /// </summary>
        public Weapon GetWeapon(int slot) => m_weapons[Mathf.Clamp(slot, 0, 1)];

        /// <summary>
        /// 이번 틱 무기 액션 플래그를 소비해 실제 무기 메서드를 호출한다. 플레이어 입력(PlayerController)이 채운 의도를 실행.
        /// 각 액션의 가드(IsChanging/Alive/매거진 등)는 개별 메서드가 담당하므로 여기선 플래그 순서대로 위임만 한다.
        /// </summary>
        /// <param name="input">이번 틱 입력. weapon 필드의 플래그만 사용.</param>
        public void ApplyWeaponInput(in HumanInput input)
        {
            HumanWeaponAction w = input.weapon;
            if ((w & HumanWeaponAction.SelectFirst) != 0) SetSelectWeapon(0);
            if ((w & HumanWeaponAction.SelectSecond) != 0) SetSelectWeapon(1);
            if ((w & HumanWeaponAction.Drop) != 0) DropCurrentWeapon();
            if ((w & HumanWeaponAction.SwitchPrevious) != 0) SwitchWeaponPrevious();
            if ((w & HumanWeaponAction.SwitchNext) != 0) SwitchWeaponNext();
            if ((w & HumanWeaponAction.Reload) != 0) ReloadCurrentWeapon();
            if ((w & HumanWeaponAction.Fire) != 0) CurrentWeapon?.Shoot(this);
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
        /// 치트(F9 복제) — 두 슬롯 무기를 source 가 현재 들고 있는 무기 "종류"로 교체한다(탄약은 기본값, 현재 탄약 무시).
        /// 활성 슬롯도 source 와 일치시킨다. 원본 F9: weapon_paramid[]=현재 무기 종류번호, 탄약은 AddVisualWeaponIndex 기본값.
        /// </summary>
        /// <param name="source">무기 구성을 복사할 원본 Human.</param>
        public void CopyHeldWeaponsFrom(Human source)
        {
            if (source == null) return;
            int noneIdx = DataManager.Instance.WeaponParameterData.weaponGeneralData.noneWeaponIndex;

            for (int i = 0; i < m_weapons.Length; i++)
            {
                Weapon src = source.GetWeapon(i);
                int idx = src != null ? src.WeaponIndex : noneIdx;
                if (m_weapons[i] != null) Destroy(m_weapons[i].gameObject);
                m_weapons[i] = InstantiateWeapon(idx); // 기본 탄약
            }

            m_selectWeapon = Mathf.Clamp(source.SelectWeapon, 0, 1);
            m_weapons[0].gameObject.SetActive(m_selectWeapon == 0);
            m_weapons[1].gameObject.SetActive(m_selectWeapon == 1);
            humanVisual.ApplyWeaponVisual(m_weapons[m_selectWeapon]);
        }

        /// <summary>
        /// MapLoader.WeaponPrefab 을 Instantiate 하고 Weapon.CreateWeapon 으로 초기화한 뒤
        /// WeaponData.fixWeapon 에 따라 fixed/dynamicWeaponAttachRoot 중 하나로 부모를 설정한다.
        /// magazine/reserve 모두 음수면 default(가득 + 0예비). drop/pickup/SwitchID 는 옛 값을 그대로 넘겨 보존.
        /// </summary>
        /// <param name="weaponIndex">WeaponParameterData.weaponData 인덱스. 무효면 Weapon 측이 noneWeaponIndex 로 폴백.</param>
        /// <param name="magazine">장전된 탄. -1=default(magazineSize).</param>
        /// <param name="reserve">예비 탄. -1=default(0).</param>
        private Weapon InstantiateWeapon(int weaponIndex, int magazine = -1, int reserve = -1)
        {
            GameObject instance = Instantiate(MapLoader.Instance.WeaponPrefab);
            Weapon weapon = instance.GetComponent<Weapon>();
            weapon.CreateWeapon(weaponIndex, magazine, reserve);

            Transform parent = weapon.WeaponData.fixWeapon
                ? humanVisual.FixedWeaponAttachRoot
                : humanVisual.DynamicWeaponAttachRoot;
            instance.transform.SetParent(parent, false);

            weapon.OnEquip();
            return weapon;
        }

        /// <summary>
        /// 현재 슬롯 무기를 재장전. WeaponReloadStyle 분기는 Weapon.RunReload 가 처리.
        /// 가드: Alive / IsChanging / 무기 가드 (StartReload 안). 통과 시 m_reloadingCnt = reloadTime + 팔 hold reaction 시작.
        /// 원본 OpenXOPS human::ReloadWeapon (object.cpp:743-782) 대응. 사운드 트리거는 별도 사이클.
        /// </summary>
        public void ReloadCurrentWeapon()
        {
            if (!Alive) return;
            if (IsChanging) return;

            Weapon current = m_weapons[m_selectWeapon];
            if (current == null) return;
            if (!current.StartReload()) return;

            float duration = current.WeaponData.reloadTime;
            float holdDeg = DataManager.Instance.HumanParameterData.humanGeneralData.armAngleReloading;

            // reloadTime <= 0 (예: GRENADE 같은 즉시 reload 무기): m_reloadingCnt 카운트다운이 발생하지 않으므로
            // RunReload 를 즉시 호출해 매거진 보충. m_reloadingCnt 는 0 유지 (IsChanging 안 됨, 다음 발사 즉시 가능).
            if (duration <= 0f)
            {
                // 즉시 재장전(예: GRENADE, reloadTime 0) — 원본 OpenXOPS 처럼 팔 reaction 을 건드리지 않는다.
                // (ReloadCnt 가 0 이라 ProcessObject 가 reaction_y 를 오버라이드하지 않는 거동.)
                // 발사 직후 자동 재장전 경로에서 방금 세팅한 발사 스윙(BeginArmShotReaction)이 와이프되지 않도록 함.
                current.RunReload();
                return;
            }

            m_reloadingCnt = duration;
            humanVisual.BeginArmReactionHold(holdDeg, duration);
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

            int oldMag = current.CurrentMagazine;
            int oldReserve = current.ReserveAmmo;
            Destroy(current.gameObject);

            Weapon w = InstantiateWeapon(targetIndex, oldMag, oldReserve);
            m_weapons[m_selectWeapon] = w;
            w.gameObject.SetActive(true);
            humanVisual.ApplyWeaponVisual(w);

            float duration = w.WeaponData.switchTime;
            float holdDeg = DataManager.Instance.HumanParameterData.humanGeneralData.armAngleReloading;
            m_selectWeaponCnt = duration;
            humanVisual.BeginArmReactionHold(holdDeg, duration);
        }

        /// <summary>
        /// 치트(F7) — 현재 슬롯 무기를 parameter 무기 목록에서 dir 방향으로 강제 순환 교체한다.
        /// 현재 탄약(매거진/예비)을 새 무기 장탄수에 맞추지 않고 그대로 유지한다(예: PSG1 5/10 → MP5 5/10, NONE 0/0 → MP5 0/0).
        /// 전환/재장전 락 없이 즉시 교체. 원본 ObjectManager::CheatNewWeapon (objectmanager.cpp:2320).
        /// UnityXOPS 무기는 prefab 인스턴스라 모델/텍스처를 CreateWeapon 으로 재생성해야 하므로 destroy + InstantiateWeapon 패턴을 쓴다.
        /// </summary>
        /// <param name="dir">-1 = 이전 무기, +1 = 다음 무기 (weaponData 인덱스 순환, noneWeapon 포함).</param>
        public void CheatCycleWeapon(int dir)
        {
            var list = DataManager.Instance.WeaponParameterData.weaponData;
            if (list == null || list.Count == 0) return;

            Weapon current = m_weapons[m_selectWeapon];
            int count = list.Count;
            int cur = current != null ? current.WeaponIndex : 0;
            int next = ((cur + dir) % count + count) % count;

            // 현재 탄약을 그대로 새 무기에 이전 (새 무기 장탄수에 맞추지 않음).
            int mag = current != null ? current.CurrentMagazine : 0;
            int reserve = current != null ? current.ReserveAmmo : 0;

            if (current != null) Destroy(current.gameObject);

            Weapon w = InstantiateWeapon(next);
            w.CheatSetAmmo(mag, reserve);
            m_weapons[m_selectWeapon] = w;
            w.gameObject.SetActive(true);
            humanVisual.ApplyWeaponVisual(w);

            // 교체 전 무기의 진행 중 전환/재장전 카운터가 새 무기에 남지 않게 해제 → 즉시 사용 가능.
            m_selectWeaponCnt = 0f;
            m_reloadingCnt = 0f;
        }

        /// <summary>
        /// 사망 시 두 슬롯의 무기를 모두 무작위 방향으로 흩뿌리고 슬롯을 noneWeapon 으로 교체한다.
        /// 원본 OpenXOPS object.cpp:1228-1237 (CheckAndProcessDead 사망 진입 루프) + weapon::Dropoff 대응.
        /// 무작위 yaw: 0~350° 10° 단위 (원본 DegreeToRadian(10)*GetRand(36)). speed = k_deathDropHorizontalSpeed (1.5 × 0.1).
        /// noneWeaponIndex 슬롯은 건너뛴다. IsChanging 락은 무시 (사망은 즉시 처리).
        /// </summary>
        public void DropAllWeaponsOnDeath()
        {
            var weaponParam = DataManager.Instance.WeaponParameterData;
            int noneIdx = weaponParam.weaponGeneralData.noneWeaponIndex;

            for (int i = 0; i < m_weapons.Length; i++)
            {
                Weapon current = m_weapons[i];
                if (current == null || current.WeaponIndex == noneIdx) continue;

                int idx = current.WeaponIndex;
                int mag = current.CurrentMagazine;
                int reserve = current.ReserveAmmo;
                Destroy(current.gameObject);

                // 슬롯에 noneWeapon 부착. 활성 슬롯만 SetActive(true).
                Weapon empty = InstantiateWeapon(noneIdx);
                m_weapons[i] = empty;
                empty.gameObject.SetActive(i == m_selectWeapon);

                // 0~350° 10° 단위 무작위 yaw
                float yawDeg = UnityEngine.Random.Range(0, 36) * 10f;
                float yawRad = yawDeg * Mathf.Deg2Rad;
                Vector3 horizDir = new Vector3(Mathf.Sin(yawRad), 0f, Mathf.Cos(yawRad));
                Vector3 spawnPos = transform.position + horizDir * 0.5f + Vector3.up * 1.6f;

                GameObject droppedObj = Instantiate(MapLoader.Instance.WeaponPrefab, MapLoader.Instance.WeaponRoot);
                droppedObj.transform.position = spawnPos;
                Weapon dropped = droppedObj.GetComponent<Weapon>();
                dropped.CreateWeapon(idx, mag, reserve, dropped: true);
                // 모델 yaw: 던진 방향 + 180° (원본 rotation_x + π).
                float weaponYaw = yawDeg + 180f;
                dropped.OnDrop(weaponYaw, horizDir * k_deathDropHorizontalSpeed);
            }

            // 무기 카운터 리셋 (원본 selectweaponcnt/weaponshotcnt/weaponreloadcnt = 0).
            m_selectWeaponCnt = 0f;
            m_reloadingCnt = 0f;

            // 활성 슬롯 noneWeapon 으로 팔 visual 갱신.
            humanVisual.ApplyWeaponVisual(m_weapons[m_selectWeapon]);
        }

        /// <summary>
        /// 현재 슬롯 무기를 슬롯에서 제거 (떨어진 무기 spawn 없이 noneWeapon 으로 교체).
        /// AutoReload 무기가 매거진 0 + 예비 0 도달 시 호출 — 무기 자체가 소진. 락/reaction 없음.
        /// </summary>
        public void ConsumeCurrentWeapon()
        {
            var weaponParam = DataManager.Instance.WeaponParameterData;
            int noneIdx = weaponParam.weaponGeneralData.noneWeaponIndex;

            Weapon current = m_weapons[m_selectWeapon];
            if (current == null || current.WeaponIndex == noneIdx) return;

            Destroy(current.gameObject);

            Weapon empty = InstantiateWeapon(noneIdx);
            m_weapons[m_selectWeapon] = empty;
            empty.gameObject.SetActive(true);
            humanVisual.ApplyWeaponVisual(empty);
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
            int noneIdx = weaponParam.weaponGeneralData.noneWeaponIndex;
            Weapon current = m_weapons[m_selectWeapon];
            if (current == null || current.WeaponIndex == noneIdx) return;

            int idx = current.WeaponIndex;
            int mag = current.CurrentMagazine;
            int reserve = current.ReserveAmmo;
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
            dropped.CreateWeapon(idx, mag, reserve, dropped: true);
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

            var general = DataManager.Instance.HumanParameterData.humanGeneralData;
            float radius = general.weaponPickupRadius;
            float yMin = general.weaponPickupVerticalRange.min;
            float yMax = general.weaponPickupVerticalRange.max;
            float radiusSq = radius * radius;

            Vector3 humanPos = transform.position;
            Transform weaponRoot = MapLoader.Instance.WeaponRoot;
            if (weaponRoot == null) return;

            for (int i = 0; i < weaponRoot.childCount; i++)
            {
                Transform child = weaponRoot.GetChild(i);
                Weapon dropped = child.GetComponent<Weapon>();
                if (dropped == null) continue;

                Vector3 wPos = child.position;
                float dy = wPos.y - humanPos.y;
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
            int idx = dropped.WeaponIndex;
            int mag = dropped.CurrentMagazine;
            int reserve = dropped.ReserveAmmo;
            Destroy(dropped.gameObject);

            Weapon old = m_weapons[m_selectWeapon];
            if (old != null) Destroy(old.gameObject);

            Weapon w = InstantiateWeapon(idx, mag, reserve);
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
