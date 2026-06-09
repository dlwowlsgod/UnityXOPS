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

        // 관통 시 attacks 감쇠 — OpenXOPS objectmanager.cpp:760-797 부위 명중 후 다음 hit 에 적용. 정수 truncation(원본 (int) 캐스팅) 유지.
        private const float k_pierceAttenHead = 0.5f;
        private const float k_pierceAttenBody = 0.6f;
        private const float k_pierceAttenLeg  = 0.7f;
        // 벽(Block) 관통 후 attacks 감쇠 — OpenXOPS objectmanager.cpp:854 (penetration >= 0 일 때 × 0.6). 사람·벽이 penetration 카운터 공유.
        private const float k_pierceAttenWall = 0.6f;
        // 벽 두께 1 서브스텝 = 원본 BULLET_SPEEDSCALE 2.5 × 0.1. 벽 내부에 걸친 서브스텝마다 penetration 1 소비 → 두꺼운 벽은 관통력 있어도 소진돼 막힘 (원본 objectmanager.cpp:845-859).
        private const float k_blockStepSize        = 0.25f;
        // 두께 측정 상한 (Collider.Raycast 역방향 탐색). 이 이상이면 측정 실패 = 매우 두꺼움 → 사실상 막힘.
        private const float k_maxBlockThickness    = 10f;
        // 얇은 벽(서브스텝보다 얇아 inside 점이 안 걸림) — penetration 무소비, 위력만 감쇠 (원본 경로 B objectmanager.cpp:870-874).
        private const float k_thinWallAttenPierce  = 0.75f; // penetration > 0
        private const float k_thinWallAttenStop    = 0.55f; // penetration <= 0

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
        // 총알 통과음(hyu) 볼륨 — 원본 MAX_SOUNDPASSING 80 / 폭발(120) 정규화 (soundmanager.h:40).
        private const float k_passingVolume   = 0.667f;

        private BulletData     m_bulletData;
        private Human          m_owner;
        private int            m_team;
        private int            m_attacks;
        private int            m_penetration;
        private Vector3        m_velocity;
        private float          m_lifetimeTimer;
        private float          m_armingTimer;
        private bool           m_active;
        private float          m_onTargetWeight; // 명중 통계 가중(산탄=2/pellet, 단발=1). Weapon.SpawnBullets 가 Spawn 직후 주입.
        private Vector3        m_visualOrigin;   // visual 표시 기준점 (무기 머즐플래시 위치). 원본엔 없는 UnityXOPS 연출.
        private bool           m_visualVisible;  // bulletBoundAdjust 거리 도달 후 true
        private bool           m_passingSoundDone; // 통과음(hyu) 한 발당 1회만 재생 — closest-approach 프레임에서 true
        private HashSet<Human>       m_hitHumans  = new HashSet<Human>();
        private HashSet<SmallObject> m_hitObjects = new HashSet<SmallObject>();

        private MeshFilter     m_visualMeshFilter;
        private MeshRenderer   m_visualMeshRenderer;

        public bool IsActive => m_active;

        /// <summary>명중 통계 가중치 주입 (Weapon.SpawnBullets 가 Spawn 직후 호출). 산탄=2/pellet, 단발=1.</summary>
        public void SetOnTargetWeight(float weight) => m_onTargetWeight = weight;

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
                          Vector3 position, Vector3 velocity, Vector3 visualOrigin)
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
            m_onTargetWeight = 1f; // 기본 단발 가중. 산탄은 SpawnBullets 가 SetOnTargetWeight 로 덮어씀.
            m_visualOrigin  = visualOrigin;
            m_passingSoundDone = false;
            m_hitHumans.Clear();
            m_hitObjects.Clear();

            transform.position = position;
            transform.rotation = velocity.sqrMagnitude > 0f
                ? Quaternion.LookRotation(velocity.normalized, Vector3.up)
                : Quaternion.identity;

            // visual 게이팅 — 머즐플래시 위치에서 bulletBoundAdjust 만큼 멀어지기 전까지 숨김 (카메라 발사 총알이 화면 앞에 크게 보이는 현상 방지).
            SetVisualVisible(data.bulletBoundAdjust <= 0f);
        }

        private void SetVisualVisible(bool value)
        {
            m_visualVisible = value;
            if (visual != null) visual.SetActive(value);
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

            Vector3 startPos = transform.position;

            if (m_bulletData.useGravity) UpdateGrenade(dt);
            else                         UpdateStraight(dt);

            // 충돌로 회수됐으면(m_active=false) 통과음/visual 스킵.
            if (m_active)
            {
                TickPassingSound(startPos);
                NotifyAiBulletPass(startPos);

                // 머즐 기준 bound 거리 도달 시 visual 표시.
                if (!m_visualVisible)
                {
                    float bound = m_bulletData.bulletBoundAdjust;
                    if ((transform.position - m_visualOrigin).sqrMagnitude >= bound * bound)
                        SetVisualVisible(true);
                }
            }
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
                    HandleObjectHit(objCollider.Owner, hit, dir);
                    continue; // 소품은 총알을 막지 않음 — 데미지 + attacks 감쇠 후 관통 (원본 objectmanager.cpp:835-839)
                }

                if (hit.collider.gameObject.layer == MapLoader.BlockLayer)
                {
                    HandleBlockHit(hit, dir, out bool blockConsumed);
                    if (blockConsumed) return true;
                    continue; // 벽 관통 — attacks 감쇠 후 다음 hit 진행
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

            float hpBefore = human.HP;                  // 킬 판정용 스냅샷 (원본 hp_old, objectmanager.cpp:977)
            float dealtDmg = hitbox.OnBulletHit(m_attacks);
            m_hitHumans.Add(human);

            // 발사자 통계 — 명중(산탄 가중)/헤드샷/킬. RecordHit/RecordKill 가 발사자==Player 게이트. 원본 objectmanager.cpp:975-979.
            MapLoader.RecordHit(m_owner, hitbox.Part == HumanHitPart.Head, m_onTargetWeight);
            if (hpBefore > 0f && human.HP <= 0f) MapLoader.RecordKill(m_owner);

            PlayHumanHitSound(hit.point); // 원본 objectmanager.cpp:971 HitHuman — 부위·사망 무관 탄착점에서 1회
            // 혈흔 (원본 objectmanager.cpp:968 SetHumanBlood, flowing=true) — 데미지 비례 분사(damage/10) 포함.
            EffectManager.Instance.Play(m_bulletData.humanHitEffectIndex, hit.point, dealtDmg);

            // 피탄음 AI 인지 (원본 HIT_HUMAN_*, 부위별 거리, 팀 무관 → enemyDist==allyDist). 근처 AI 경계 트리거.
            var hg = DataManager.Instance.HumanParameterData.humanGeneralData;
            float hitDist = hitbox.Part switch
            {
                HumanHitPart.Head => hg.aiHearHitHumanHead,
                HumanHitPart.Leg  => hg.aiHearHitHumanLeg,
                _                 => hg.aiHearHitHumanBody,
            };
            WorldSound.EmitPointSound(hit.point, m_team, hitDist, hitDist);

            float atten = hitbox.Part switch
            {
                HumanHitPart.Head => k_pierceAttenHead,
                HumanHitPart.Body => k_pierceAttenBody,
                HumanHitPart.Leg  => k_pierceAttenLeg,
                _                 => 1f,
            };
            m_attacks = Mathf.FloorToInt(m_attacks * atten);
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

        /// <summary>
        /// 벽(Block) hit 처리. consumed=true 면 정지/폭발로 종료, false 면 관통(다음 hit 진행).
        /// 원본 OpenXOPS objectmanager.cpp:845-860 CollideBullet 벽 판정 — penetration 소진 시 정지, 남으면 attacks×0.6 후 관통.
        /// </summary>
        private void HandleBlockHit(RaycastHit hit, Vector3 dir, out bool consumed)
        {
            consumed = false;

            if ((m_bulletData.explosionTrigger & ExplosionTrigger.Block) != 0)
            {
                if (m_armingTimer > 0f) { Recycle(); consumed = true; return; }
                transform.position = hit.point - dir * k_hitEpsilon;
                Explode();
                consumed = true;
                return;
            }

            // 탄착 이펙트/사운드 — 관통 여부와 무관하게 표면에 1회 (원본 objectmanager.cpp:849 HitBulletMap).
            PlayWallHitSound(hit.point);
            EffectManager.Instance.Play(m_bulletData.wallHitEffectIndex, hit.point);
            // 벽 탄착음 AI 인지 (원본 HIT_MAP, 팀 무관 → enemyDist==allyDist). 근처 AI 경계 트리거.
            WorldSound.EmitPointSound(hit.point, m_team,
                DataManager.Instance.HumanParameterData.humanGeneralData.aiHearBulletWallHitDist,
                DataManager.Instance.HumanParameterData.humanGeneralData.aiHearBulletWallHitDist);

            float thickness = MeasureBlockThickness(hit, dir);

            // 얇은 벽 (원본 경로 B) — penetration 무소비, 위력만 감쇠 후 통과.
            if (thickness < k_blockStepSize)
            {
                m_attacks = Mathf.FloorToInt(m_attacks * (m_penetration > 0 ? k_thinWallAttenPierce : k_thinWallAttenStop));
                return; // consumed=false → 관통
            }

            // 두꺼운 벽 (원본 경로 A) — 두께 내부에 걸친 서브스텝 점 개수만큼 반복: 매 점마다 penetration 1 소비 + attacks ×0.6. 소진되면 그 지점(벽 내부)에 박혀서 정지.
            // 점 개수 = floor(두께 / 스텝). 원본은 진입 위상에 따라 floor~floor+1 이지만, 올림(ceil)은 애매한 두께를 과다 소비해 관통을 막으므로 내림 사용.
            int steps = Mathf.FloorToInt(thickness / k_blockStepSize);
            for (int s = 0; s < steps; s++)
            {
                m_penetration--;
                if (m_penetration < 0)
                {
                    // 소진된 서브스텝 위치 = 진입면에서 s 스텝 들어간 지점 (원본은 벽 내부에서 멈춤).
                    transform.position = hit.point + dir * (s * k_blockStepSize);
                    Recycle();
                    consumed = true;
                    return;
                }
                m_attacks = Mathf.FloorToInt(m_attacks * k_pierceAttenWall);
            }
            // 관통 성공 — 위치는 옮기지 않음 (UpdateStraight 가 dt 끝까지 진행). consumed=false → 다음 hit 진행.
        }

        /// <summary>
        /// 벽 두께 측정 — 진입 지점에서 총알 이동방향(dir)으로 같은 벽의 나가는 면(backface)까지 거리 = 경로상 실제 통과 두께.
        /// 원본의 "탄도를 따라 벽 내부를 검사" 방식 대응 — 모서리에 비스듬히 걸치면 경로가 길어져 자동으로 두껍게 측정된다.
        /// backface 를 잡기 위해 측정 동안만 queriesHitBackfaces 를 켜고 즉시 복원(동기 실행이라 다른 raycast 영향 없음). 단일 collider 한정이라 가벼움.
        /// exit 면을 못 찾으면(상한 초과 두께) 상한값 반환 → 호출부에서 사실상 막힘 처리.
        /// </summary>
        private float MeasureBlockThickness(RaycastHit entry, Vector3 dir)
        {
            bool prev = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;

            float   thickness = k_maxBlockThickness;
            Vector3 start     = entry.point + dir * k_hitEpsilon;
            if (entry.collider.Raycast(new Ray(start, dir), out RaycastHit exit, k_maxBlockThickness))
                thickness = exit.distance + k_hitEpsilon;

            Physics.queriesHitBackfaces = prev;
            return thickness;
        }

        /// <summary>
        /// 소품 콜라이더(Sphere/Box/Capsule = 프리미티브) 통과 길이(chord) 측정.
        /// 프리미티브 콜라이더는 queriesHitBackfaces 가 적용되지 않고 내부 시작 raycast 도 hit 를 안 잡으므로,
        /// 콜라이더 뒤쪽(바깥)에서 진행 반대방향으로 쏴 먼 면(exit)을 찾는 방식을 쓴다. 단일 collider 한정.
        /// 측정 실패(빗맞음) 시 0 반환 → 호출부에서 최소 1회 처리.
        /// </summary>
        private float MeasureObjectChord(Collider collider, Vector3 entryPoint, Vector3 dir)
        {
            Vector3 farStart = entryPoint + dir * k_maxBlockThickness;
            if (collider.Raycast(new Ray(farStart, -dir), out RaycastHit exit, k_maxBlockThickness))
                return Mathf.Max(0f, k_maxBlockThickness - exit.distance);
            return 0f;
        }

        /// <summary>
        /// 소품(SmallObject) 총알 피격. 원본 objectmanager.cpp:831-840 — 데미지 = floor(attacks×배율), 명중 후 잔여 attacks ×= 감쇠.
        /// 원본은 총알을 BULLET_SPEEDSCALE 간격으로 서브스텝하며 매 스텝 소물 충돌 구체를 검사하는데, 사람(BulletObj_HumanIndex)과 달리
        /// "이미 맞음" 가드가 없어 구체를 지나는 서브스텝 수만큼 반복 피격된다 → 한 발이 통과 두께만큼 여러 번 데미지를 준다.
        /// 우리는 콜라이더 통과 길이(chord) / 서브스텝 크기로 그 명중 횟수를 재현한다. 소품은 penetration 을 소비하지 않는다.
        /// </summary>
        private void HandleObjectHit(SmallObject obj, RaycastHit hit, Vector3 dir)
        {
            if (obj == null || obj.IsDestroyed) return;
            if (!m_hitObjects.Add(obj))         return; // 같은 소품은 1발당 1회 진입 — 통과 전체 서브스텝을 여기서 처리

            var gen = DataManager.Instance.ObjectParameterData.objectGeneralData;

            // chord(콜라이더 통과 길이) / k_blockStepSize(= BULLET_SPEEDSCALE×0.1) = 명중 횟수. 최소 1회.
            // 소물은 프리미티브 콜라이더라 벽용 MeasureBlockThickness 가 안 통함 → 전용 측정 사용.
            float chord = MeasureObjectChord(hit.collider, hit.point, dir);
            int   steps = Mathf.Max(1, Mathf.FloorToInt(chord / k_blockStepSize));

            for (int s = 0; s < steps; s++)
            {
                obj.HitBullet(Mathf.FloorToInt(m_attacks * gen.bulletDamageMultiplier));     // 파괴 후엔 SmallObject 가 자체 가드로 no-op
                m_attacks = Mathf.FloorToInt(m_attacks * gen.bulletPenetrationAttenuation);
            }

            EffectManager.Instance.Play(m_bulletData.objectHitEffectIndex, hit.point);

            // 소품 피탄음 AI 인지 (원본 HIT_SMALLOBJECT, 팀 무관).
            float od = DataManager.Instance.HumanParameterData.humanGeneralData.aiHearHitSmallObject;
            WorldSound.EmitPointSound(hit.point, m_team, od, od);
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

            // 폭발음 AI 인지 — 팀 무관(원본 GRE_EXPLOSION 팀 비교 없음). 청취 범위 내 AI 경계 전환.
            float hearDist = DataManager.Instance.HumanParameterData.humanGeneralData.aiHearExplosionDist;
            WorldSound.EmitPointSound(transform.position, m_team, hearDist, hearDist);

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

                    float hpBefore = hb.Human.HP; // 수류탄 킬 판정 스냅샷 (원본 objectmanager.cpp:1113-1115)
                    hb.Human.ApplyDamage(total);
                    // 수류탄 킬만 집계 — 명중/헤드샷은 미집계(원본 비대칭). RecordKill 가 발사자==Player 게이트.
                    if (hpBefore > 0f && hb.Human.HP <= 0f) MapLoader.RecordKill(m_owner);
                    hb.Human.SetHitReaction(DataManager.Instance.HumanParameterData.humanGeneralData.grenadeHitReaction); // 원본 object.cpp:1079 HitGrenadeExplosion =10
                    // 폭발 혈흔 — 원본 objectmanager.cpp:1119 SetHumanBlood(flowing=false): 메인 혈흔만, 분사 없음.
                    // triggerValue 미전달(=0) → countPerTrigger 분사 emitter 는 0개. (총알/좀비 피격만 데미지 비례 분사)
                    EffectManager.Instance.Play(m_bulletData.humanHitEffectIndex, hb.Human.transform.position);
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
            PlayRandomSound(m_bulletData.wallHitSounds, position, k_wallHitVolume);
        }

        private void PlayHumanHitSound(Vector3 position)
        {
            PlayRandomSound(m_bulletData.humanHitSounds, position, k_humanHitVolume);
        }

        /// <summary>
        /// 총알이 카메라(리스너) 근처를 스쳐 지나가는 hyu 음. 원본 OpenXOPS SoundManager::CheckApproach + PlaySound(BULLET) 대응.
        /// 궤적이 카메라에 최단 접근하는 프레임 1회만 재생 (이전/현재/다음 프레임 거리 비교). 아군(플레이어 팀) 총알은 제외.
        /// 비어있는 bulletPassingSounds(GRENADE 등)면 재생 안 함.
        /// </summary>
        private void TickPassingSound(Vector3 startPos)
        {
            if (m_passingSoundDone) return;

            List<string> sounds = m_bulletData.bulletPassingSounds;
            if (sounds == null || sounds.Count == 0) return;

            // 아군 총알 제외 — 원본은 플레이어 "팀" 단위 제외 (soundmanager.cpp:626).
            Human player = MapLoader.Player;
            if (player == null || m_team == player.Team) return;

            Vector3 listener = SoundManager.Instance.ListenerPosition;
            Vector3 current  = transform.position;
            Vector3 move     = current - startPos;

            if (!IsClosestApproach(startPos, current, move, listener, out Vector3 closestPoint)) return;

            PlayRandomSound(sounds, closestPoint, k_passingVolume);
            m_passingSoundDone = true;
        }

        /// <summary>
        /// 총알이 AI 머리 근처를 스쳐 지나가면 그 AI 에게 위협 신호 (원본 GetWorldSound BULLET, maxdist 20→2.0, 팀 무관 거리 판정).
        /// 카메라 hyu 와 달리 팀 무관 — 아군 탄이 스쳐도 경계. 플레이어 Human 은 brain 이 없어 제외. closest-approach 머신은 카메라용과 공유.
        /// </summary>
        private void NotifyAiBulletPass(Vector3 startPos)
        {
            if (!HumanController.TickEnabled) return;

            var humans = MapLoader.Humans;
            if (humans == null) return;

            var     gen      = DataManager.Instance.HumanParameterData.humanGeneralData;
            float   maxDist  = gen.aiHearBulletDist;
            float   eye      = gen.cameraAttachPosition;
            Human   player   = MapLoader.Player;
            Vector3 current  = transform.position;
            Vector3 move     = current - startPos;

            for (int i = 0; i < humans.Count; i++)
            {
                Human h = humans[i];
                if (h == null || h == player || !h.Alive) continue;

                Vector3 head = h.transform.position + Vector3.up * eye;
                if (IsClosestApproach(startPos, current, move, head, out Vector3 cp) &&
                    (cp - head).sqrMagnitude < maxDist * maxDist)
                    h.NotifyThreatHeard();
            }
        }

        /// <summary>
        /// 이번 프레임이 listener 에 대한 궤적 최단접근(통과) 프레임인지 판정. 원본 CheckApproach (soundmanager.cpp:513) 의 3점 비교.
        /// true 면 궤적 직선 위 listener 수직 투영점(closestPoint)을 출력 — 3D 음원 위치로 사용. (AI 총알 감지에서 재사용 가능)
        /// </summary>
        private static bool IsClosestApproach(Vector3 prev, Vector3 current, Vector3 move, Vector3 listener, out Vector3 closestPoint)
        {
            closestPoint = current;

            Vector3 next = current + move;
            float d1 = (listener - prev).sqrMagnitude;
            float d2 = (listener - current).sqrMagnitude;
            float d3 = (listener - next).sqrMagnitude;
            // 현재 위치가 직전·직후보다 가까운 국소 최소 = 막 스쳐가는 프레임.
            if (!(d1 > d2 && d2 < d3)) return false;

            float moveLen = move.magnitude;
            if (moveLen > 1e-5f)
            {
                Vector3 mdir = move / moveLen;
                closestPoint = current + mdir * Vector3.Dot(listener - current, mdir);
            }
            return true;
        }

        private static void PlayRandomSound(List<string> sounds, Vector3 position, float volume)
        {
            if (sounds == null || sounds.Count == 0) return;

            string path = sounds[Random.Range(0, sounds.Count)];
            if (string.IsNullOrEmpty(path)) return;

            AudioClip clip = SoundLoader.LoadAudio(SafePath.Combine(Application.streamingAssetsPath, path));
            SoundManager.Instance.PlayAt(clip, position, volume);
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
