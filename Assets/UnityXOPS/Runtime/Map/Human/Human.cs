using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 사망 상태머신. 원본 OpenXOPS human::deadstate (object.cpp:1208-1389) 정수값과 동일.
    /// 0 Alive 정상 / 1 Falling 쓰러지기 시작 / 2 HeadStuck 머리 박힘+자유낙하 /
    /// 3 LegSliding 다리 미끄러뜨리기 / 4 Settling 1프레임 정지 / 5 Done 완전 고정.
    /// </summary>
    public enum HumanDeadState
    {
        Alive      = 0,
        Falling    = 1,
        HeadStuck  = 2,
        LegSliding = 3,
        Settling   = 4,
        Done       = 5,
    }

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
        private HumanDeadState deadState = HumanDeadState.Alive;
        public HumanDeadState DeadState => deadState;
        public bool            Alive    => deadState == HumanDeadState.Alive;

        /// <summary>
        /// 사망 상태를 설정. 전이 로직은 HumanController.Tick에서 호출 예정.
        /// </summary>
        public void SetDeadState(HumanDeadState value)
        {
            deadState = value;
        }

        /// <summary>
        /// 데미지 적용. HP 만 차감. 사망 진입(Falling) + 무기 드롭은 HumanController.EnterDeadState 가 처리한다
        /// (시체 회전 초기화 m_deadDirection/m_deadAddRy 와 함께 한 곳에서 처리하기 위함).
        /// HP ≤ 0 인 채 다음 FixedUpdate 까지의 짧은 갭은 Human.Update 의 Alive 게이트로 보호.
        /// 원본 OpenXOPS human::SubHP (object.cpp:1060-1080) 단순화 버전.
        /// </summary>
        public void ApplyDamage(float damage)
        {
            if (!Alive || damage <= 0f) return;

            hp -= damage;
            if (hp < 0f) hp = 0f;
        }

        // 원본 OpenXOPS human::Hit_rx (object.cpp:1084-1088 SetHitFlag) — 마지막 피격 yaw (월드 deg).
        // 사망 진입 시 HumanController.EnterDeadState 가 이 값과 본인 Yaw 차이로 앞/뒤 쓰러짐 분기.
        // Hit_rx 는 클리어 안 됨: 살아있는 동안 여러 번 맞으면 마지막 hit 방향이 사망 시 사용 (원본 동작 그대로).
        private float m_hitYaw;
        public  float HitYaw => m_hitYaw;

        /// <summary>
        /// 마지막 피격 방향 (월드 yaw, deg). Bullet 측이 명중 시 호출. 사망 분기에만 사용, 살아있는 동안의 동작에는 영향 없음.
        /// </summary>
        public void SetHitYaw(float yawDeg)
        {
            m_hitYaw = yawDeg;
        }

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

        private HumanController m_controller;

        // 조준 오차 중 연사 반동 누적분. 원본 OpenXOPS human::ReactionGunsightErrorRange (object.cpp).
        // 발사 시 WeaponData.recoil 가산, 매 프레임 회복(감소) + 현재 무기 errorRange.max 클램프.
        private float m_reactionErrorRange;

        private RawPointData m_humanParam, m_humanDataParam;

        private int m_identifier;
        public  int Identifier => m_identifier;

        private Weapon[] m_weapons = new Weapon[2];
        private int      m_selectWeapon;
        private float    m_selectWeaponCnt;
        private float    m_reloadingCnt;

        // 사망 시 무기 드롭 속도. 원본 OpenXOPS object.cpp:1232 Dropoff(..., 1.5f) × 0.1 = 0.15 m/s.
        // DumpWeapon (수동 버리기, dropoffHorizontalSpeed JSON ≈ 1.63 × 0.1) 보다 약간 낮음.
        private const float k_deathDropHorizontalSpeed = 0.15f;
        public  Weapon   CurrentWeapon => m_weapons[m_selectWeapon];
        public  int      SelectWeapon  => m_selectWeapon;

        // OpenXOPS 컨벤션: 슬롯 0=보조(SUB), 슬롯 1=주(MAIN). EquipInitialWeapons 의 m_selectWeapon=1 초기화 근거.
        public  Weapon   MainWeapon => m_weapons[1];
        public  Weapon   SubWeapon  => m_weapons[0];

        // 발사/재장전/전환 가드용. UI 측은 IsSwitchingWeapon / IsReloading 으로 분리해서 표시.
        public  bool     IsSwitchingWeapon => m_selectWeaponCnt > 0f;
        public  bool     IsReloading       => m_reloadingCnt > 0f;
        public  bool     IsChanging        => IsSwitchingWeapon || IsReloading;

        /// <summary>
        /// 슬롯 인덱스(0/1) 로 무기 인스턴스를 조회. UI HUD 등에서 비활성 슬롯 무기 정보가 필요할 때 사용.
        /// </summary>
        public Weapon GetWeapon(int slot) => m_weapons[Mathf.Clamp(slot, 0, 1)];

        /// <summary>
        /// 포인트 데이터와 파라미터로부터 인간 캐릭터를 생성 및 초기화한다.
        /// </summary>
        /// <param name="humanParam">인간 배치 포인트 데이터.</param>
        /// <param name="humanDataParam">인간 파라미터 포인트 데이터.</param>
        public void CreateHuman(RawPointData humanParam, RawPointData humanDataParam)
        {
            m_humanParam     = humanParam;
            m_humanDataParam = humanDataParam;
            m_identifier     = humanParam.param3;
            m_controller     = GetComponent<HumanController>();

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

            hp   = m_humanData.hp;
            team = m_humanDataParam.param2;
            deadState = hp > 0 ? HumanDeadState.Alive : HumanDeadState.Done;
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
            // 사망 시 카운터/팔 reaction/픽업 모두 정지. HP ≤ 0 직후 다음 FixedUpdate 의 EnterDeadState 가
            // 호출되기 전 짧은 갭 (Update 가 FixedUpdate 보다 먼저 도는 경우) 도 함께 보호.
            if (!Alive || hp <= 0f) return;

            float dt = Time.deltaTime;
            if (m_selectWeaponCnt > 0f) m_selectWeaponCnt -= dt;

            TickReactionRecovery(dt);

            // m_reloadingCnt 는 SwitchID(SEMI↔FULL) 와 Reload 둘 다 사용. 0 도달 시 활성 무기가 reloading 중이면 매거진 보충.
            if (m_reloadingCnt > 0f)
            {
                m_reloadingCnt -= dt;
                if (m_reloadingCnt <= 0f)
                {
                    Weapon current = m_weapons[m_selectWeapon];
                    if (current != null && current.IsReloading) current.RunReload();
                }
            }

            humanVisual.TickArmReaction(dt);
            TryPickupWeapon();
        }

        /// <summary>
        /// 발사 시점의 실효 조준 오차 (단위: ErrorRange 정수 스케일). 상태 오차 + 반동 누적분의 합.
        /// Weapon.SpawnBullets 가 errorRange.min 하한을 적용한 뒤 0.15° 단위로 환산해 탄도에 더한다.
        /// 원본 OpenXOPS human::GetGunsightErrorRange (object.cpp:2113-2116).
        /// </summary>
        public float GunsightErrorRange => StateErrorRange() + m_reactionErrorRange;

        /// <summary>
        /// 이동/점프/저체력 상태에 따른 조준 오차. 원본 OpenXOPS human::GunsightErrorRange (object.cpp:1130-1152).
        /// 이동 페널티는 대입(=)이라 마지막으로 평가된 조건이 우선 — Walk→Forward→Back→Strafe→airborne 순서. 저체력만 가산(+=).
        /// </summary>
        private float StateErrorRange()
        {
            var wgen = DataManager.Instance.WeaponParameterData.weaponGeneralData;
            int state = 0;

            HumanMoveFlag flag = m_controller != null ? m_controller.MoveFlagLt : HumanMoveFlag.None;
            if ((flag & HumanMoveFlag.Walk)    != 0)                                 state = wgen.walkAccuracyPenalty;
            if ((flag & HumanMoveFlag.Forward) != 0)                                 state = wgen.forwardAccuracyPenalty;
            if ((flag & HumanMoveFlag.Back)    != 0)                                 state = wgen.backAccuracyPenalty;
            if ((flag & (HumanMoveFlag.Left | HumanMoveFlag.Right)) != 0)            state = wgen.strafeAccuracyPenalty;
            if (m_controller != null && !m_controller.Grounded)                      state = wgen.airborneAccuracyPenalty;

            if (hp < wgen.injuryHpThreshold) state += wgen.injuryAccuracyPenalty;

            return state;
        }

        /// <summary>
        /// 발사 후 반동 누적. 원본 OpenXOPS human::ShotWeapon (object.cpp:707) ReactionGunsightErrorRange += reaction.
        /// 상한 클램프는 TickReactionRecovery 가 매 프레임 처리하므로 여기선 가산만.
        /// </summary>
        public void AddShotReaction(float recoil)
        {
            m_reactionErrorRange += recoil;
        }

        /// <summary>
        /// 발사 시 에임 킥(실제 시점 이동) 을 컨트롤러 시점각에 누적. 플레이어는 PlayerController 가 다음 프레임 마우스 누적값으로 되읽어 영구 반영(자동 복원 없음).
        /// 원본 OpenXOPS human::ShotWeapon 의 rotation_x/armrotation_y 가산 (object.cpp:725-726). AI 는 매 틱 재조준으로 덮어써짐.
        /// </summary>
        /// <param name="yawDeg">좌우 킥 (deg). 대칭 분포.</param>
        /// <param name="pitchDeg">상하 킥 (deg). Unity 부호: 음수 = 위로.</param>
        public void AddViewRecoil(float yawDeg, float pitchDeg)
        {
            if (m_controller != null) m_controller.AddYawPitch(yawDeg, pitchDeg);
        }

        /// <summary>
        /// 반동 누적분 회복 — 매 프레임 reactionRecoveryPerSecond 만큼 감소, [0, 현재 무기 errorRange.max] 클램프.
        /// 원본 OpenXOPS object.cpp:1153-1157 (프레임당 -1, 33.333fps).
        /// </summary>
        private void TickReactionRecovery(float dt)
        {
            if (m_reactionErrorRange <= 0f) { m_reactionErrorRange = 0f; return; }

            var wgen = DataManager.Instance.WeaponParameterData.weaponGeneralData;
            m_reactionErrorRange -= wgen.reactionRecoveryPerSecond * dt;
            if (m_reactionErrorRange < 0f) m_reactionErrorRange = 0f;

            Weapon current = m_weapons[m_selectWeapon];
            if (current != null)
            {
                float maxErr = current.WeaponData.errorRange.max;
                if (m_reactionErrorRange > maxErr) m_reactionErrorRange = maxErr;
            }
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
            if (!Alive)         return;
            if (IsChanging)     return;

            Weapon current = m_weapons[m_selectWeapon];
            if (current == null)        return;
            if (!current.StartReload()) return;

            float duration = current.WeaponData.reloadTime;
            float holdDeg  = DataManager.Instance.HumanParameterData.humanGeneralData.armAngleReloading;

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

            int oldMag     = current.CurrentMagazine;
            int oldReserve = current.ReserveAmmo;
            Destroy(current.gameObject);

            Weapon w = InstantiateWeapon(targetIndex, oldMag, oldReserve);
            m_weapons[m_selectWeapon] = w;
            w.gameObject.SetActive(true);
            humanVisual.ApplyWeaponVisual(w);

            float duration    = w.WeaponData.switchTime;
            float holdDeg     = DataManager.Instance.HumanParameterData.humanGeneralData.armAngleReloading;
            m_selectWeaponCnt = duration;
            humanVisual.BeginArmReactionHold(holdDeg, duration);
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
            int noneIdx     = weaponParam.weaponGeneralData.noneWeaponIndex;

            for (int i = 0; i < m_weapons.Length; i++)
            {
                Weapon current = m_weapons[i];
                if (current == null || current.WeaponIndex == noneIdx) continue;

                int idx     = current.WeaponIndex;
                int mag     = current.CurrentMagazine;
                int reserve = current.ReserveAmmo;
                Destroy(current.gameObject);

                // 슬롯에 noneWeapon 부착. 활성 슬롯만 SetActive(true).
                Weapon empty = InstantiateWeapon(noneIdx);
                m_weapons[i] = empty;
                empty.gameObject.SetActive(i == m_selectWeapon);

                // 0~350° 10° 단위 무작위 yaw
                float   yawDeg   = UnityEngine.Random.Range(0, 36) * 10f;
                float   yawRad   = yawDeg * Mathf.Deg2Rad;
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
            m_reloadingCnt    = 0f;

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
            int noneIdx     = weaponParam.weaponGeneralData.noneWeaponIndex;

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
            int noneIdx     = weaponParam.weaponGeneralData.noneWeaponIndex;
            Weapon current  = m_weapons[m_selectWeapon];
            if (current == null || current.WeaponIndex == noneIdx) return;

            int idx     = current.WeaponIndex;
            int mag     = current.CurrentMagazine;
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
            int idx     = dropped.WeaponIndex;
            int mag     = dropped.CurrentMagazine;
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
