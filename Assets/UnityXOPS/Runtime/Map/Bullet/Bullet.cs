using System.Collections.Generic;
using UnityEngine;
using JJLUtility;
using JJLUtility.IO;

namespace UnityXOPS
{
    /// <summary>
    /// 풀에서 관리되는 발사체. BulletData.useGravity 플래그로 직선 탄 / 수류탄 두 동작을 분기한다.
    /// MonoBehaviour Update 비활성 — BulletManager 가 매 프레임 활성 슬롯만 Tick 호출.
    /// 원본 OpenXOPS bullet (직선) + grenade (포물선·반사) 두 클래스를 단일 컴포넌트 + 데이터 분기로 통합.
    /// </summary>
    public class Bullet : MonoBehaviour
    {
        [SerializeField] private GameObject visual;

        // 관통 시 attacks 감쇠 — OpenXOPS objectmanager.cpp:760-797 부위 명중 후 다음 hit 에 적용.
        // Block 명중은 1차에선 즉시 정지(관통 미구현) 라 별도 상수 없음.
        private const float k_pierceAttenHead = 0.5f;
        private const float k_pierceAttenBody = 0.6f;
        private const float k_pierceAttenLeg  = 0.7f;

        // 수류탄 마찰 — OpenXOPS object.cpp:3010-3026 (× 0.98 / frame, 33.333 fps). dt 가변 변환: 0.98^33.333 ≈ 0.5169.
        private const float k_grenadeFrictionPerSec = 0.5169f;
        // 수류탄 반사 감속 = -Angle×0.2546 + 0.7 (원본 object.cpp:3000). Angle = asin(v̂·n)×-1 ∈ [0, π/2].
        // 정면 충돌(π/2) → 0.3 강한 감속, 스치는 충돌(0) → 0.7. 0.2546 은 원본 매직넘버.
        private const float k_reflectAngleCoef = 0.2546f;
        private const float k_reflectBaseCoef  = 0.7f;
        // 명중 후 hit point 직전 epsilon — 면 안쪽 박힘 방지.
        private const float k_hitEpsilon            = 0.001f;

        // 총알 명중 시 사람에 가하는 knockback — OpenXOPS objectmanager.cpp:935 AddPosOrder(brx, 0, 1.0).
        // 원본 1.0 unit/frame × 33.333 fps × 0.1 m/unit = 3.333 m/s. 부위/무기/탄종 무관 고정.
        private const float k_bulletKnockbackSpeedMps = 3.333f;
        // BulletData.explosionknockbackMax 의 단위 변환 (m/frame → m/s).
        private const float k_frameToSecond = 33.3333f;

        // 수류탄 폭발 사람 판정점 Y (원본 objectmanager.cpp:1076,1088 — HUMAN 좌표 +2.0=발, HUMAN_HEIGHT-2.0=18.0=머리. ×0.1 Unity).
        // 발 점을 지면에서 0.2 띄우는 게 핵심: 지면 정착 수류탄→발 차폐 레이가 바닥을 스쳐 막히면서 발 데미지가 통째로 0이 되는 버그 방지.
        private const float k_grenadeLegPointY  = 0.2f;
        private const float k_grenadeHeadPointY = 1.8f;

        // 수류탄 바운드음 속도 게이트 — 원본 objectmanager.cpp:2889 (반사 직전 speed > 3.4 units/frame 일 때만 재생).
        // 3.4 × 0.1 m/unit × 33.333 fps = 11.333 m/s. 약하게 굴러가는 튕김은 무음.
        private const float k_grenadeBoundSoundMinSpeed = 11.333f;
        // 효과음 볼륨 — 원본 볼륨 상수(폭발 120, 바운드/착탄 95~100)를 최댓값(폭발)으로 정규화한 상대값. 거리 감쇠는 SoundManager 3D 선형 rolloff 가 처리.
        private const float k_explosionVolume = 1.0f;
        private const float k_wallHitVolume   = 0.8f;
        // 사람 피격음 볼륨 — 원본 MAX_SOUNDHITHUMAN 75 / 폭발(120) 정규화. 부위·사망 무관 단일 hit2.wav (원본 soundmanager.cpp:605-612).
        private const float k_humanHitVolume  = 0.625f;

        private BulletData     m_bulletData;
        private Human          m_owner;
        private int            m_team;
        private int            m_attacks;
        private int            m_penetration;
        private Vector3        m_velocity;
        private float          m_lifetimeTimer;
        private float          m_armingTimer;
        private bool           m_active;
        private HashSet<Human>       m_hitHumans  = new HashSet<Human>();
        private HashSet<SmallObject> m_hitObjects = new HashSet<SmallObject>();

        private MeshFilter     m_visualMeshFilter;
        private MeshRenderer   m_visualMeshRenderer;

        public bool IsActive => m_active;

        private void Awake()
        {
            if (visual != null)
            {
                m_visualMeshFilter   = visual.GetComponent<MeshFilter>();
                m_visualMeshRenderer = visual.GetComponent<MeshRenderer>();
            }
        }

        /// <summary>
        /// BulletManager 가 Spawn 직전에 호출. Bullet prefab 의 visual GameObject 의 메시/머티리얼/transform 을 BulletData 에 맞게 교체.
        /// </summary>
        public void ApplyVisual(Mesh mesh, Material material, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            if (m_visualMeshFilter   != null) m_visualMeshFilter.sharedMesh       = mesh;
            if (m_visualMeshRenderer != null) m_visualMeshRenderer.sharedMaterial = material;
            if (visual               != null)
            {
                visual.transform.localPosition = position;
                visual.transform.localRotation = Quaternion.Euler(rotation);
                visual.transform.localScale    = scale;
            }
        }

        /// <summary>
        /// 풀에서 활성화된 슬롯을 발사 상태로 초기화. position/velocity 는 월드 좌표.
        /// </summary>
        public void Spawn(BulletData data, Human owner, int team,
                          int attacks, int penetration,
                          Vector3 position, Vector3 velocity)
        {
            m_bulletData    = data;
            m_owner         = owner;
            m_team          = team;
            m_attacks       = attacks;
            m_penetration   = penetration;
            m_velocity      = velocity;
            m_lifetimeTimer = data.lifetime;
            m_armingTimer   = data.armingDelay;
            m_active        = true;
            m_hitHumans.Clear();
            m_hitObjects.Clear();

            transform.position = position;
            transform.rotation = velocity.sqrMagnitude > 0f
                ? Quaternion.LookRotation(velocity.normalized, Vector3.up)
                : Quaternion.identity;
        }

        /// <summary>
        /// BulletManager 가 매 프레임 호출. 충돌 검사 → 위치 이동 순서 (원본 ObjectManager::Process).
        /// </summary>
        public void Tick(float dt)
        {
            if (!m_active) return;

            m_lifetimeTimer -= dt;
            if (m_lifetimeTimer <= 0f)
            {
                if ((m_bulletData.explosionTrigger & ExplosionTrigger.Lifetime) != 0)
                    Explode();
                else
                    Recycle();
                return;
            }

            if (m_armingTimer > 0f) m_armingTimer -= dt;

            if (m_bulletData.useGravity) UpdateGrenade(dt);
            else                         UpdateStraight(dt);
        }

        private void UpdateStraight(float dt)
        {
            float speed = m_velocity.magnitude;
            if (speed <= 0f) return;

            float   distance = speed * dt;
            Vector3 dir      = m_velocity / speed;

            if (TryHandleCollision(transform.position, dir, distance)) return;
            transform.position += m_velocity * dt;
        }

        private void UpdateGrenade(float dt)
        {
            // 중력 — BulletData.gravityScale 자체를 m/s² 단위로 가정 (1차 사이클 가정, 검증 단계에서 보정).
            m_velocity.y -= m_bulletData.gravityScale * dt;
            // 마찰
            m_velocity   *= Mathf.Pow(k_grenadeFrictionPerSec, dt);

            float speed = m_velocity.magnitude;
            if (speed <= 0f) return;

            float   distance = speed * dt;
            Vector3 dir      = m_velocity / speed;

            // Block 충돌 — 원본 grenade::ProcessObject 처럼 우선 검사.
            if (Physics.Raycast(transform.position, dir, out RaycastHit blockHit,
                                distance, MapLoader.BlockLayerMask))
            {
                // 임팩트 폭발 grenade(미래 Sticky/RPG)
                if ((m_bulletData.explosionTrigger & ExplosionTrigger.Block) != 0)
                {
                    if (m_armingTimer > 0f) { Recycle(); return; }
                    transform.position = blockHit.point - dir * k_hitEpsilon;
                    Explode();
                    return;
                }

                // 일반 수류탄 — 반사 + 입사각 기반 감속 (원본 grenade::ProcessObject object.cpp:2987-3005).
                float dot   = Mathf.Clamp(Vector3.Dot(dir, blockHit.normal), -1f, 1f);
                float angle = -Mathf.Asin(dot);                          // 원본 Collision::AngleVector (collision.cpp:883)
                float accel = -angle * k_reflectAngleCoef + k_reflectBaseCoef;
                m_velocity = Vector3.Reflect(m_velocity, blockHit.normal) * accel;
                transform.position = blockHit.point + blockHit.normal * k_hitEpsilon;

                // 바운드음 — 반사 직전 속도가 게이트를 넘을 때만 (원본 objectmanager.cpp:2889).
                if (speed > k_grenadeBoundSoundMinSpeed) PlayWallHitSound(blockHit.point);
                return;
            }

            // 임팩트 폭발 grenade 만 사람 검사 (일반 수류탄은 사람 통과).
            if ((m_bulletData.explosionTrigger & ExplosionTrigger.Human) != 0 &&
                TryHandleCollision(transform.position, dir, distance))
            {
                return;
            }

            transform.position += m_velocity * dt;
        }

        /// <summary>
        /// 이번 프레임 이동 선분에서 RaycastAll → 거리순 Block / HumanHitbox 순차 처리.
        /// 관통 중 attacks/penetration 누적 감쇠. 폭발 트리거 충돌 시 즉시 Explode/Recycle.
        /// 원본 sub-step 분해는 폐기 — RaycastAll 의 선분이 같은 의도(이번 프레임 구간 전체) 를 1회로 처리.
        /// </summary>
        private bool TryHandleCollision(Vector3 from, Vector3 dir, float distance)
        {
            RaycastHit[] hits = Physics.RaycastAll(from, dir, distance + k_hitEpsilon);
            if (hits.Length == 0) return false;
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];

                HumanHitbox hitbox = hit.collider.GetComponent<HumanHitbox>();
                if (hitbox != null)
                {
                    if (HandleHumanHit(hitbox, hit, dir, out bool consumed))
                    {
                        if (consumed) return true;
                        continue; // 자기 자신/시체/이미 맞춘 사람 → 무시 후 다음 hit
                    }
                    // 관통 — 이번 hit 처리 후 다음 hit 으로 진행
                    continue;
                }

                ObjectCollider objCollider = hit.collider.GetComponentInParent<ObjectCollider>();
                if (objCollider != null && objCollider.Owner != null)
                {
                    HandleObjectHit(objCollider.Owner, hit.point);
                    continue; // 소품은 총알을 막지 않음 — 데미지 + attacks 감쇠 후 관통 (원본 objectmanager.cpp:835-839)
                }

                if (hit.collider.gameObject.layer == MapLoader.BlockLayer)
                {
                    return HandleBlockHit(hit, dir);
                }
                // 그 외 collider(CharacterController, weapon prefab 등) 는 무시.
            }

            return false;
        }

        /// <summary>
        /// 사람 hit 처리. consumed=true 면 풀 회수 또는 폭발로 종료, false 면 관통(다음 hit 진행).
        /// 반환값 true 는 "이번 hit 을 인식함" (관통/소비 무관), false 는 "필터링되어 무시" (다음 hit 으로 fall-through).
        /// </summary>
        private bool HandleHumanHit(HumanHitbox hitbox, RaycastHit hit, Vector3 dir, out bool consumed)
        {
            consumed = false;
            Human human = hitbox.Human;
            if (human == null)               return false;
            if (human == m_owner)            return false; // 자기 사격 차단
            if (human.Team == m_team)        return false; // 같은 팀 차단 (총알 한정, 폭발은 Explode 에서 차단 X)
            if (!human.Alive)                return false; // 시체 통과
            if (m_hitHumans.Contains(human)) return false; // HashSet 차단

            // 임팩트 폭발 트리거 활성 — 즉시 Explode 또는 dud Recycle.
            if ((m_bulletData.explosionTrigger & ExplosionTrigger.Human) != 0)
            {
                if (m_armingTimer > 0f) { Recycle(); consumed = true; return true; }
                transform.position = hit.point - dir * k_hitEpsilon;
                Explode();
                consumed = true;
                return true;
            }

            // 사망 분기용 hit yaw 저장 + knockback. 원본 OpenXOPS objectmanager.cpp:935-951 AddPosOrder + SetHitFlag.
            // 데미지 적용 (OnBulletHit) 보다 먼저 호출 — OnBulletHit 안에서 사망 진입 시 EnterDeadState 가 HitYaw 사용.
            float knockYaw = Mathf.Atan2(m_velocity.x, m_velocity.z) * Mathf.Rad2Deg;
            human.SetHitYaw(knockYaw);
            HumanController controller = human.GetComponent<HumanController>();
            if (controller != null) controller.AddKnockback(knockYaw, 0f, k_bulletKnockbackSpeedMps);

            hitbox.OnBulletHit(m_attacks);
            m_hitHumans.Add(human);
            PlayHumanHitSound(hit.point); // 원본 objectmanager.cpp:971 HitHuman — 부위·사망 무관 탄착점에서 1회
            EffectManager.Instance.Play(m_bulletData.humanHitEffectIndex, hit.point); // 혈흔 (원본 objectmanager.cpp:968 SetHumanBlood)

            float atten = hitbox.Part switch
            {
                HumanHitPart.Head => k_pierceAttenHead,
                HumanHitPart.Body => k_pierceAttenBody,
                HumanHitPart.Leg  => k_pierceAttenLeg,
                _                 => 1f,
            };
            m_attacks = Mathf.RoundToInt(m_attacks * atten);
            m_penetration--;

            if (m_penetration < 0)
            {
                transform.position = hit.point;  // 마지막 hit 지점에 정지
                Recycle();
                consumed = true;
                return true;
            }
            // 관통 — 위치는 옮기지 않음. UpdateStraight 가 dt 끝까지 진행시키도록.
            return true;
        }

        private bool HandleBlockHit(RaycastHit hit, Vector3 dir)
        {
            if ((m_bulletData.explosionTrigger & ExplosionTrigger.Block) != 0)
            {
                if (m_armingTimer > 0f) { Recycle(); return true; }
                transform.position = hit.point - dir * k_hitEpsilon;
                Explode();
                return true;
            }

            // 일반 탄 — 면 직전에 박혀 정지. 1차에선 Block 관통(원본 L865-878) 미구현.
            transform.position = hit.point - dir * k_hitEpsilon;
            PlayWallHitSound(hit.point); // 원본 objectmanager.cpp:897 HitMap
            EffectManager.Instance.Play(m_bulletData.wallHitEffectIndex, hit.point);
            Recycle();
            return true;
        }

        /// <summary>
        /// 소품(SmallObject) 총알 피격. 원본 objectmanager.cpp:835-839 — 데미지 = floor(attacks×0.25), 통과 후 잔여 attacks ×0.7.
        /// 소품은 총알을 멈추지 않고 관통시키며, 같은 소품은 1발당 1회만 처리.
        /// </summary>
        private void HandleObjectHit(SmallObject obj, Vector3 hitPoint)
        {
            if (obj == null || obj.IsDestroyed) return;
            if (!m_hitObjects.Add(obj))         return; // 같은 소품 중복 hit 차단

            obj.HitBullet(Mathf.FloorToInt(m_attacks * 0.25f));
            m_attacks = Mathf.FloorToInt(m_attacks * 0.7f);
            EffectManager.Instance.Play(m_bulletData.objectHitEffectIndex, hitPoint);
        }

        /// <summary>
        /// 폭발 데미지 — explosionRadius 내 모든 사람에게 거리 기반 선형감쇠. owner 자폭 포함.
        /// 머리/다리 별도 raycast 차폐 검사 (원본 OpenXOPS objectmanager.cpp:1039-1230 GrenadeExplosion).
        /// 1차 사이클: 이펙트/사운드/소품 데미지/knockback 미구현.
        /// </summary>
        private void Explode()
        {
            PlayExplosionSound(transform.position); // 원본 objectmanager.cpp:1227 GrenadeExplosion, 폭발 1회
            EffectManager.Instance.Play(m_bulletData.explosionEffectIndex, transform.position);

            float radius = m_bulletData.explosionRadius;
            if (radius > 0f)
            {
                Vector3 origin     = transform.position;
                float   headDmgMax = m_bulletData.humanExplosiveHeadDamageMax;
                float   legDmgMax  = m_bulletData.humanExplosiveLegDamageMax;
                float   knockMax   = m_bulletData.explosionknockbackMax * k_frameToSecond;

                Collider[] cols = Physics.OverlapSphere(origin, radius);
                HashSet<Human> processed = new HashSet<Human>();

                for (int i = 0; i < cols.Length; i++)
                {
                    HumanHitbox hb = cols[i].GetComponent<HumanHitbox>();
                    if (hb == null || hb.Human == null) continue;
                    if (!processed.Add(hb.Human)) continue;
                    if (!hb.Human.Alive)         continue;

                    Vector3 humanPos = hb.Human.transform.position;
                    float legDmg  = ComputeExplosionDamage(origin, humanPos + Vector3.up * k_grenadeLegPointY,  radius, legDmgMax);
                    float headDmg = ComputeExplosionDamage(origin, humanPos + Vector3.up * k_grenadeHeadPointY, radius, headDmgMax);

                    float total = legDmg + headDmg;
                    if (total <= 0f) continue;

                    // knockback - 사람이 폭심에서 멀어지는 방향. 폭심이 사람 위면 수평만 (지면 박힘 방지, 원본 :1129-1135).
                    // 사망 분기용 SetHitYaw 도 같이 — 데미지 적용보다 먼저 (사망 진입 시 EnterDeadState 가 HitYaw 사용).
                    Vector3 toHuman = humanPos - origin;
                    float   dist    = toHuman.magnitude;
                    float pushYaw   = Mathf.Atan2(toHuman.x, toHuman.z) * Mathf.Rad2Deg;
                    hb.Human.SetHitYaw(pushYaw);

                    if (dist > k_hitEpsilon)
                    {
                        Vector3 pushDir = toHuman / dist;
                        if (pushDir.y < 0f)
                        {
                            pushDir.y = 0f;
                            float horiz = new Vector2(pushDir.x, pushDir.z).magnitude;
                            if (horiz > k_hitEpsilon) pushDir /= horiz;
                        }
                        float knockSpeed = knockMax * Mathf.Max(0f, 1f - dist / radius);
                        if (knockSpeed > 0f)
                        {
                            HumanController controller = hb.Human.GetComponent<HumanController>();
                            if (controller != null) controller.AddKnockbackVector(pushDir, knockSpeed);
                        }
                    }

                    hb.Human.ApplyDamage(total);
                    hb.Human.SetHitReaction(DataManager.Instance.WeaponParameterData.weaponGeneralData.hitReactionExplosion); // 원본 object.cpp:1079 HitGrenadeExplosion =10
                }

                // 소품(SmallObject) 폭발 피격 — 단일 중심점 거리 감쇠 + 벽 차폐 (원본 objectmanager.cpp:1171-1211, damage = 80 - r).
                // 사람용 ComputeExplosionDamage 재사용: objectExplosiveDamageMax × (1 - d/radius) = 원본과 수학적 동일. 머리/다리 구분·넉백 없음.
                float objDmgMax = m_bulletData.objectExplosiveDamageMax;
                if (objDmgMax > 0f)
                {
                    HashSet<SmallObject> processedObj = new HashSet<SmallObject>();
                    for (int i = 0; i < cols.Length; i++)
                    {
                        ObjectCollider oc = cols[i].GetComponentInParent<ObjectCollider>();
                        if (oc == null || oc.Owner == null) continue;
                        if (oc.Owner.IsDestroyed)           continue;
                        if (!processedObj.Add(oc.Owner))    continue;

                        float objDmg = ComputeExplosionDamage(origin, oc.Owner.transform.position, radius, objDmgMax);
                        if (objDmg > 0f) oc.Owner.HitGrenadeExplosion(objDmg);
                    }
                }
            }

            Recycle();
        }

        private float ComputeExplosionDamage(Vector3 origin, Vector3 target, float radius, float maxDamage)
        {
            Vector3 toTarget = target - origin;
            float   dist     = toTarget.magnitude;
            if (dist >= radius) return 0f;

            // Block 차폐 검사 — 원본 raycast (벽 뒤 사람에 데미지 안 들어감).
            if (dist > k_hitEpsilon &&
                Physics.Raycast(origin, toTarget / dist, dist, MapLoader.BlockLayerMask))
            {
                return 0f;
            }
            // 원본 objectmanager.cpp:1081 — 감쇠항만 정수 절단: damage = max - (int)(max/maxdist × r). 음수는 0 (가산 안 함).
            float damage = maxDamage - Mathf.Floor(maxDamage / radius * dist);
            return damage > 0f ? damage : 0f;
        }

        /// <summary>
        /// 벽 충돌음 재생. wallHitSounds 에서 균등 랜덤 선택 — 원본 hit1.wav 2/3 · hit3.wav 1/3 (objectmanager.cpp:594) 은
        /// 리스트에 hit1 을 두 번 넣어 가중치로 재현. 수류탄은 [cco.wav] 단일이라 바운드음으로 동작.
        /// </summary>
        private void PlayWallHitSound(Vector3 position)
        {
            List<string> sounds = m_bulletData.wallHitSounds;
            if (sounds == null || sounds.Count == 0) return;

            string path = sounds[Random.Range(0, sounds.Count)];
            if (string.IsNullOrEmpty(path)) return;

            AudioClip clip = SoundLoader.LoadAudio(SafePath.Combine(Application.streamingAssetsPath, path));
            SoundManager.Instance.PlayAt(clip, position, k_wallHitVolume);
        }

        private void PlayHumanHitSound(Vector3 position)
        {
            if (string.IsNullOrEmpty(m_bulletData.humanHitSound)) return;

            AudioClip clip = SoundLoader.LoadAudio(SafePath.Combine(Application.streamingAssetsPath, m_bulletData.humanHitSound));
            SoundManager.Instance.PlayAt(clip, position, k_humanHitVolume);
        }

        private void PlayExplosionSound(Vector3 position)
        {
            if (string.IsNullOrEmpty(m_bulletData.explosionSound)) return;

            AudioClip clip = SoundLoader.LoadAudio(SafePath.Combine(Application.streamingAssetsPath, m_bulletData.explosionSound));
            SoundManager.Instance.PlayAt(clip, position, k_explosionVolume);
        }

        /// <summary>
        /// 외부(BulletManager.ClearPool)에서 강제 회수. m_active 를 내려 다음 Tick 에서 처리되지 않게 한다.
        /// gameObject.SetActive(false) 만으로는 BulletManager.Update 가 IsActive(=m_active) 로 Tick 여부를 판단하므로 비행이 멈추지 않는다.
        /// </summary>
        public void Deactivate() => Recycle();

        private void Recycle()
        {
            m_active = false;
            m_hitHumans.Clear();
            m_hitObjects.Clear();
            gameObject.SetActive(false);
        }
    }
}
