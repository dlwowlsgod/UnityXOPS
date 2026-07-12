using JJLUtility;
using JJLUtility.IO;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// Human 슬롯에 장착되는 무기의 데이터/동적 상태와 발사·장전·낙하 로직을 담당하는 코어 컴포넌트.
    /// 시각 표현은 WeaponVisual 에 위임하고, 발사 시 BulletManager 로 탄을 spawn 한다.
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        // ErrorRange 정수 1 단위 = 0.15°. 원본 OpenXOPS objectmanager.cpp:1980 DegreeToRadian(0.15f).
        public const float ErrorRangeUnitDegrees = 0.15f;

        [SerializeField]
        private WeaponVisual weaponVisual;
        public WeaponVisual WeaponVisual => weaponVisual;

        private int m_weaponIndex;
        private WeaponData m_weaponData;
        private WeaponModelData m_weaponModelData;
        public int WeaponIndex => m_weaponIndex;
        public WeaponData WeaponData => m_weaponData;
        public WeaponModelData WeaponModelData => m_weaponModelData;

        private int m_currentMagazine;
        private int m_reserveAmmo;
        private bool m_isReloading;
        public int CurrentMagazine => m_currentMagazine;
        public int ReserveAmmo => m_reserveAmmo;
        public bool IsReloading => m_isReloading;

        private bool m_isFalling;
        private float m_velocityX;
        private float m_velocityY;
        private float m_velocityZ;
        public bool IsFalling => m_isFalling;

        private float m_fireRateTimer;
        public float FireRateTimer => m_fireRateTimer;

        // 격발음 clip (캐싱). soundVolume 0 무기(NONE/GRENADE/CASE)는 null.
        private AudioClip m_fireSound;

        /// <summary>
        /// weaponIndex 로 WeaponData/WeaponModelData 를 조회해 무기 데이터/탄약/시각/스케일을 초기화한다.
        /// 인덱스가 유효 범위 밖이면 noneWeaponIndex(맨손 취급) 로 폴백한다 — null 슬롯은 만들지 않는다.
        /// localPosition/localRotation 은 건드리지 않으므로, 부착 시 OnEquip() 을 호출하거나 떨어진 무기 spawner 가 직접 설정해야 한다.
        ///
        /// **탄창 처리**: magazine / reserve 두 값을 명시적으로 받는다. 음수면 default (magazine=magazineSize, reserve=0).
        /// drop/pickup/SwitchID 같은 보존 시나리오에서는 호출자가 옛 값을 그대로 넘겨야 자동 재장전이 일어나지 않는다.
        /// totalBullets → (magazine, reserve) 분배는 PD1[2] / [7] 같은 spawn 측이 직접 inline 처리.
        /// </summary>
        /// <param name="weaponIndex">WeaponParameterData.weaponData 인덱스.</param>
        /// <param name="magazine">현재 장전된 탄. 음수면 magazineSize 로 가득.</param>
        /// <param name="reserve">예비 탄. 음수면 magazineSize × 2 (Initial spawn / PD1 [7] 보급 default).</param>
        /// <param name="dropped">true=맵에 떨어진 모드(부모 weaponRoot, scale 자기 책임), false=Human 부착 모드(부모 weaponAttachRoot 가 weaponScale 강제).</param>
        public void CreateWeapon(int weaponIndex, int magazine = -1, int reserve = -1, bool dropped = false)
        {
            var wp = DataManager.Instance.WeaponParameterData;
            if (weaponIndex < 0 || weaponIndex >= wp.weaponData.Count)
            {
                weaponIndex = wp.weaponGeneralData.noneWeaponIndex;
            }

            m_weaponIndex = weaponIndex;
            m_weaponData = wp.weaponData[weaponIndex];

            int modelIndex = m_weaponData.modelIndex;
            if (modelIndex >= 0 && modelIndex < wp.weaponModelData.Count)
            {
                m_weaponModelData = wp.weaponModelData[modelIndex];
            }

            // 격발음 clip 로드 (SoundLoader 가 경로 단위 캐싱). soundVolume 0 무기는 무음 → 로드 스킵.
            m_fireSound = (m_weaponData.soundVolume > 0f && !string.IsNullOrEmpty(m_weaponData.soundPath))
                ? SoundLoader.LoadAudio(SafePath.Combine(Application.streamingAssetsPath, m_weaponData.soundPath))
                : null;

            int magazineSize = m_weaponData.magazineSize;
            if (magazine < 0) magazine = magazineSize;
            if (reserve < 0) reserve = magazineSize * 2;
            m_currentMagazine = Mathf.Clamp(magazine, 0, magazineSize);
            m_reserveAmmo = reserve;
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
        /// 치트(F6) — 예비탄에 장탄수(magazineSize)만큼 1회 추가한다. 현재 탄창은 그대로.
        /// 원본 ObjectManager::CheatAddBullet (objectmanager.cpp:2285) — nbs += nbsmax. noneWeapon(magazineSize≤0)은 무시. 상한 없음.
        /// </summary>
        public void CheatAddMagazine()
        {
            if (m_weaponData == null || m_weaponData.magazineSize <= 0) return;
            m_reserveAmmo += m_weaponData.magazineSize;
        }

        /// <summary>
        /// 치트(F7 무기변경) — 매거진/예비탄을 장탄수 클램프 없이 그대로 설정한다.
        /// 무기 종류를 바꿔도 현재 탄약을 새 무기에 그대로 유지(새 무기 장탄수에 안 맞춤)하는 용도.
        /// </summary>
        /// <param name="magazine">설정할 현재 탄창 수 (클램프 없음).</param>
        /// <param name="reserve">설정할 예비탄 수.</param>
        public void CheatSetAmmo(int magazine, int reserve)
        {
            m_currentMagazine = magazine;
            m_reserveAmmo = reserve;
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
        /// 발사 — owner 의 시점 위치에서 발사 방향 벡터로 BulletManager 에 발사체 spawn 요청.
        /// fireRate 쿨다운, 매거진 잔량, 발사자 상태(IsChanging/Alive) 검사 후 실제 발사. 산탄(pelletCount > 1) 은 박스 분포 분산.
        /// 원본 OpenXOPS ObjectManager::ShotWeapon (objectmanager.cpp:1926-2060) 대응.
        /// </summary>
        /// <param name="owner">발사 주체 Human.</param>
        /// <returns>실제로 발사됐으면 true, 쿨다운·전환·재장전·매거진 0 등으로 발사 못 하면 false (원본 ShotWeapon 성공 여부).</returns>
        public bool Shoot(Human owner)
        {
            if (owner == null || !owner.Alive) return false;
            if (owner.IsChanging) return false;
            if (m_isFalling) return false;
            if (m_fireRateTimer > 0f) return false;
            if (m_isReloading) return false;
            if (m_currentMagazine <= 0) return false;
            if (m_weaponData.fireRate <= 0f) return false; // noneWeapon 등 발사 불가능 무기

            var wp = DataManager.Instance.WeaponParameterData;
            int bulletIdx = m_weaponData.bulletIndex;
            if (bulletIdx < 0 || bulletIdx >= wp.bulletData.Count) return false;

            BulletData bulletData = wp.bulletData[bulletIdx];
            SpawnBullets(owner, bulletData);

            // 발사 통계 — 플레이어 총기 발사만 +1 (수류탄 제외: 원본 ShotWeapon 반환 1만 카운트 gamemain.cpp:2240). RecordFire 가 owner==Player 게이트.
            if (m_weaponIndex != wp.weaponGeneralData.grenadeWeaponIndex)
                MapLoader.RecordFire(owner);

            // 이번 발사 분 반동을 누적 — 다음 발사부터 조준 오차에 반영 (원본 human::ShotWeapon object.cpp:707).
            owner.AddShotReaction(m_weaponData.recoil);

            // 팔/총 모델 시각 반동 — 발사 시 위로 까딱→자동 복원 (정확도와 별개, 무기별 데이터값).
            // 원본 MotionCtrl::ShotWeapon (object.cpp:3341-3362) 의 reaction_y 를 per-weapon 데이터로 일반화.
            if (owner.HumanVisual != null)
                owner.HumanVisual.BeginArmShotReaction(m_weaponData.armReactionAngle);

            // 에임 킥 — 실제 시점이 움직임 (영구 누적, 자동 복원 없음). 원본 human::ShotWeapon (object.cpp:710-728).
            // recoilAimHorizontal=좌우(대칭), recoilAimVertical=상하(양수 저장=위로 → Unity 적용 시 부호 반전).
            float yawKick = UnityEngine.Random.Range(m_weaponData.recoilAimHorizontal.min, m_weaponData.recoilAimHorizontal.max);
            float pitchKick = UnityEngine.Random.Range(m_weaponData.recoilAimVertical.min, m_weaponData.recoilAimVertical.max);

            // 스코프 조준 중이면 ScopeData 반동을 추가 가산 (원본 object.cpp:710-728, 대체가 아닌 += 가산).
            // PSG1 은 WeaponData 에, AUG 는 ScopeData 에 반동값이 들어있어 — 둘 다 같은 조준 킥으로 합쳐짐.
            ScopeData scope = owner.ActiveScope;
            if (scope != null)
            {
                yawKick += UnityEngine.Random.Range(scope.recoilAimHorizontalAdjust.min, scope.recoilAimHorizontalAdjust.max);
                pitchKick += UnityEngine.Random.Range(scope.recoilAimVerticalAdjust.min, scope.recoilAimVerticalAdjust.max);
            }

            if (yawKick != 0f || pitchKick != 0f)
                owner.AddViewRecoil(yawKick, -pitchKick);

            // 격발음 — 무기 위치를 음원으로 3D 재생 (원본 objectmanager.cpp:2051). soundVolume 0 무기는 m_fireSound=null.
            if (m_fireSound != null)
                SoundManager.Instance.PlayAt(m_fireSound, transform.position, m_weaponData.soundVolume);

            // 총성 AI 인지 — soundVolume>0 무기만 (원본 objectmanager.cpp:2051 발포음 등록 게이트). suppressor 면 청취 거리 단축.
            // 음원은 사수 머리 높이 (원본 WEAPONSHOT_HEIGHT). AI 는 청취 범위 안이면 경계 전환.
            if (m_weaponData.soundVolume > 0f)
            {
                var humanGen = DataManager.Instance.HumanParameterData.humanGeneralData;
                float enemyDist = m_weaponData.suppressor ? humanGen.aiHearGunfireSilencerDist : humanGen.aiHearGunfireDist;
                Vector3 shotPos = owner.transform.position + Vector3.up * humanGen.cameraAttachPosition;
                WorldSound.EmitPointSound(shotPos, owner.Team, enemyDist, humanGen.aiHearGunfireAllyDist);
            }

            // 발사 이펙트 — 머즐플래시 + 총구연기 + 탄피 (원본 ShotWeaponEffect/ShotWeaponYakkyou objectmanager.cpp:2065,2119).
            PlayFireEffects();

            m_currentMagazine--;
            m_fireRateTimer = 1f / m_weaponData.fireRate;

            // AutoReload — 매거진 0 도달 시 자동 처리. 원본 OpenXOPS 에는 없는 UnityXOPS 추가 분기.
            //  - reserve > 0 → 자동 reload (예비에서 매거진 보충, reload 모션 없이 즉시 reloadTime=0 무기 가정).
            //  - reserve == 0 + discardAfterAutoReloadIfNoAmmo=true → 슬롯에서 무기 제거 (GRENADE).
            //  - reserve == 0 + discardAfterAutoReloadIfNoAmmo=false → 매거진 0 그대로 유지 (발사 불가, 슬롯 보존).
            if (m_currentMagazine <= 0 &&
                m_weaponData.reloadStyle == WeaponReloadStyle.AutoReload)
            {
                if      (m_reserveAmmo > 0)                              owner.ReloadCurrentWeapon();
                else if (m_weaponData.discardAfterAutoReloadIfNoAmmo)    owner.ConsumeCurrentWeapon();
            }

            return true;
        }

        /// <summary>
        /// 재장전 진입 가드. 통과 시 m_isReloading=true 로 표시. 실제 매거진 보충은 Human 의 카운터가 0 도달 시 RunReload 호출.
        /// 원본 OpenXOPS weapon::StartReload (object.cpp:2371-2379) — 예비탄 0 차단. UnityXOPS 추가: 매거진 풀 시도 차단 (재장전 의미 없음).
        /// </summary>
        public bool StartReload()
        {
            if (m_isReloading) return false;
            if (m_isFalling) return false;
            if (m_weaponData.magazineSize <= 0) return false; // noneWeapon
            if (m_reserveAmmo <= 0) return false; // 예비탄 없음
            if (m_currentMagazine >= m_weaponData.magazineSize) return false; // 매거진 풀

            m_isReloading = true;
            return true;
        }

        /// <summary>
        /// 매거진 보충 — Human 카운터가 0 도달 시 호출. WeaponReloadStyle 별 분기.
        /// 원본 OpenXOPS weapon::RunReload (object.cpp:2383-2408) = DiscardAndReload 단일 동작. 나머지는 UnityXOPS 추가.
        /// ShellByShellReload 는 1차 단순화: RetainAndReload 와 동일 (발 단위 분할/발사 중단은 다음 사이클).
        /// </summary>
        public void RunReload()
        {
            if (!m_isReloading) return;
            int magSize = m_weaponData.magazineSize;

            switch (m_weaponData.reloadStyle)
            {
                case WeaponReloadStyle.DiscardAndReload:
                    // 매거진 잔탄 폐기 + 예비에서 풀까지 채움. 원본 OpenXOPS 동작.
                    int loadD = Mathf.Min(m_reserveAmmo, magSize);
                    m_currentMagazine = loadD;
                    m_reserveAmmo -= loadD;
                    break;

                case WeaponReloadStyle.RetainAndReload:
                case WeaponReloadStyle.AutoReload:
                case WeaponReloadStyle.ShellByShellReload:
                    // 잔탄 보존 + 부족분만 채움. AutoReload (GRENADE 등) 도 reserve 정상 차감.
                    int needed = magSize - m_currentMagazine;
                    int loadR = Mathf.Min(m_reserveAmmo, needed);
                    m_currentMagazine += loadR;
                    m_reserveAmmo -= loadR;
                    break;
            }

            m_isReloading = false;
        }

        /// <summary>
        /// 이 무기로 owner 가 지금 발사할 때의 실효 조준 오차 (ErrorRange 정수 단위). 상태+반동 오차(owner.GunsightErrorRange)에
        /// errorRange.min 하한을 적용하고, ignoreAimError(투척) 면 0. 탄도(SpawnBullets)와 크로스헤어 UI 가 공유하는 단일 소스.
        /// </summary>
        public float GetEffectiveErrorRange(Human owner)
        {
            if (m_weaponData == null || owner == null) return 0f;
            if (m_weaponData.ignoreAimError)           return 0f;
            return Mathf.Max(owner.GunsightErrorRange, m_weaponData.errorRange.min);
        }

        /// <summary>
        /// 발사 시 머즐플래시·총구연기·탄피 이펙트를 무기 attach-root 기준으로 재생.
        /// 머즐/탄피 offset 은 WeaponData.position 과 동일한 attach-root 로컬 프레임 → TransformPoint 로 yaw/pitch/scale 자동 반영.
        /// muzzleFlashSize/shellSize 는 per-weapon 크기(소음기 분기 포함 이미 baked). 총구연기는 별도 크기 필드가 없어 muzzleFlashSize 를 재사용.
        /// size 0 무기(GRENADE/NONE/CASE)는 스킵 — 발사 이펙트 없음.
        /// </summary>
        private void PlayFireEffects()
        {
            if (m_weaponModelData == null) return;

            Transform attach = transform.parent; // 장착 시 fixed/dynamicWeaponAttachRoot
            if (attach == null) return;

            EffectManager em = EffectManager.Instance;

            float mfSize = m_weaponModelData.muzzleFlashSize;
            if (mfSize > 0f)
            {
                Vector3 muzzle = attach.TransformPoint(m_weaponModelData.muzzleFlashOffset);
                em.Play(m_weaponModelData.muzzleFlashEffectIndex,  muzzle, attach.rotation, mfSize, Vector3.zero);
                em.Play(m_weaponModelData.gunfireSmokeEffectIndex, muzzle, attach.rotation, mfSize, Vector3.zero);
            }

            float shSize = m_weaponModelData.shellSize;
            if (shSize > 0f)
            {
                Vector3 shellPos = attach.TransformPoint(m_weaponModelData.shellEjectOffset);
                Vector3 shellVel = attach.TransformDirection(m_weaponModelData.shellEjectDirection.normalized)
                                 * m_weaponModelData.shellEjectSpeed;
                em.Play(m_weaponModelData.shellEffectIndex, shellPos, attach.rotation, shSize, shellVel);
            }
        }

        /// <summary>
        /// 무기 머즐플래시 월드 위치 — Bullet visual 게이팅 기준점. 모델 데이터/부착 루트 없으면 fallback 을 반환.
        /// </summary>
        /// <param name="fallback">모델 데이터/부착 루트가 없을 때 반환할 대체 위치(보통 시점 위치).</param>
        /// <returns>머즐플래시 오프셋을 부착 루트로 변환한 월드 좌표, 또는 fallback.</returns>
        private Vector3 MuzzleWorldPosition(Vector3 fallback)
        {
            Transform attach = transform.parent;
            if (m_weaponModelData == null || attach == null) return fallback;
            return attach.TransformPoint(m_weaponModelData.muzzleFlashOffset);
        }

        /// <summary>
        /// owner 시점 위치 + yaw/pitch + 조준 오차로 pelletCount 발 BulletManager.Spawn 호출.
        /// 원본 OpenXOPS ObjectManager::ShotWeapon (objectmanager.cpp:1966-2026) 의 탄도 오차 로직 재현:
        ///  1. 실효 오차 = max(owner.GunsightErrorRange(상태+반동), errorRange.min). pitch/yaw 각각 독립 box 분포 × 0.15° — 펠릿 루프 밖 1회 계산해 모든 펠릿이 공유.
        ///  2. 산탄(pelletCount>1) 은 펠릿마다 폴라 추가 확산 (방향 0~350° 10°단위, 반경 {5,7,9,11,13}) × 0.15° (objectmanager.cpp:2005-2009).
        /// 발사 위치: 사람 머리(owner.position + cameraAttachPosition) — 1인칭/3인칭/AI 모두 동일 (objectmanager.cpp:2026 그대로).
        /// </summary>
        /// <param name="owner">발사 주체 Human.</param>
        /// <param name="bulletData">발사할 탄 데이터.</param>
        private void SpawnBullets(Human owner, BulletData bulletData)
        {
            var humanGen = DataManager.Instance.HumanParameterData.humanGeneralData;
            Vector3 spawnPos = owner.transform.position + Vector3.up * humanGen.cameraAttachPosition;
            Vector3 visualOrigin = MuzzleWorldPosition(spawnPos); // 총알 visual 은 이 지점에서 bulletBoundAdjust 만큼 멀어진 후 표시

            HumanController controller = owner.GetComponent<HumanController>();

            // 발사 방향은 사람 시점각(rx/ry) 그대로 — 카메라·조준점 무관 직사 (원본 objectmanager.cpp:1970-1973).
            float yaw = controller.Yaw;
            float pitch = controller.Pitch;

            // 기본 탄도 오차 (모든 펠릿 공유). 실효 오차 공식은 GetEffectiveErrorRange 단일 소스 (크로스헤어와 공유).
            float effError = GetEffectiveErrorRange(owner);
            float basePitchErr = 0f;
            float baseYawErr = 0f;
            if (effError > 0f)
            {
                basePitchErr = UnityEngine.Random.Range(-effError, effError) * ErrorRangeUnitDegrees;
                baseYawErr = UnityEngine.Random.Range(-effError, effError) * ErrorRangeUnitDegrees;
            }

            int pellets = Mathf.Max(1, m_weaponData.pelletCount);
            // 산탄은 펠릿당 데미지/명중가중을 (pellet/2) 로 나눠 전탄 명중 시 합이 단발의 2배가 되게 함. 원본 objectmanager.cpp:1998-2014.
            float onTargetWeight = pellets > 1 ? 2f / pellets : 1f;
            int perPelletDamage = pellets > 1
                ? (int)(m_weaponData.damage / (pellets / 2f))
                : (int)m_weaponData.damage;
            for (int i = 0; i < pellets; i++)
            {
                float pitchErr = basePitchErr;
                float yawErr = baseYawErr;
                if (pellets > 1)
                {
                    float a = UnityEngine.Random.Range(0, 36) * 10f * Mathf.Deg2Rad; // 방향 0~350°, 10° 단위
                    float len = UnityEngine.Random.Range(0, 5) * 2 + 5; // 반경 {5,7,9,11,13}
                    pitchErr += Mathf.Cos(a) * len * ErrorRangeUnitDegrees;
                    yawErr += Mathf.Sin(a) * len * ErrorRangeUnitDegrees;
                }

                Quaternion rot = Quaternion.Euler(pitch + pitchErr, yaw + yawErr, 0f);
                Vector3 velocity = rot * Vector3.forward * m_weaponData.bulletSpeed;

                Bullet b = BulletManager.Instance.Spawn(
                    bulletData, owner, owner.Team,
                    attacks: perPelletDamage,
                    penetration: m_weaponData.penetration,
                    position: spawnPos,
                    velocity: velocity,
                    visualOrigin: visualOrigin);
                if (b != null) b.SetOnTargetWeight(onTargetWeight); // 명중 통계용 가중치 — 펠릿마다 동일
            }
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
            float dt = Time.deltaTime;
            if (m_fireRateTimer > 0f) m_fireRateTimer -= dt;

            if (!m_isFalling) return;

            var gen = DataManager.Instance.WeaponParameterData.weaponGeneralData;

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
