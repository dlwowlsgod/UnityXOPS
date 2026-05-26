using JJLUtility;
using JJLUtility.IO;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 이펙트 빌보드 쿼드 풀의 사전할당, 재생(Play), 매 프레임 Tick, 텍스처별 머티리얼 캐시를 관리하는 싱글톤.
    /// 원본 OpenXOPS ObjectManager 의 effect 풀(MAX_EFFECT 256) + Process/Render 루프 미러.
    /// 한 EffectData 프리셋 = 여러 EffectEmitter 의 컴포지트 (예: 폭발 = 섬광 1 + 연기 4).
    /// </summary>
    public class EffectManager : SingletonBehavior<EffectManager>
    {
        // 원본 MAX_EFFECT 256 (objectmanager.h:41). 동시 발생이 이를 넘으면 새 이펙트는 drop.
        [SerializeField] private int poolSize = 256;

        // 벽 데칼을 표면에서 살짝 띄워 z-fighting 방지 (원본 1.0 unit × 0.1 = 0.1 보다 보수적으로).
        private const float k_decalSurfaceOffset = 0.05f;

        private Effect[]                  m_pool;
        private Mesh                      m_quadMesh;
        private Dictionary<int, Material> m_materialCache = new Dictionary<int, Material>();

        protected override void Awake()
        {
            base.Awake();
            BuildQuadMesh();
            InitializePool();
        }

        // 1×1 빌보드 쿼드 (XY 평면, normal +Z). transform.localScale 로 크기 조절.
        private void BuildQuadMesh()
        {
            m_quadMesh = new Mesh { name = "EffectQuad" };
            m_quadMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f),
            };
            m_quadMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
            };
            // Cull Off 이므로 와인딩 무관.
            m_quadMesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            m_quadMesh.RecalculateBounds();
        }

        private void InitializePool()
        {
            m_pool = new Effect[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                GameObject obj = new GameObject($"Effect_{i}");
                obj.transform.SetParent(transform, false);

                MeshFilter mf = obj.AddComponent<MeshFilter>();
                mf.sharedMesh = m_quadMesh;

                MeshRenderer mr = obj.AddComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows    = false;

                m_pool[i] = obj.AddComponent<Effect>();
                obj.SetActive(false);
            }
        }

        private void Update()
        {
            if (m_pool == null) return;

            Camera  cam    = Camera.main;
            Vector3 camPos = cam != null ? cam.transform.position : Vector3.zero;
            float   dt     = Time.deltaTime;

            for (int i = 0; i < m_pool.Length; i++)
            {
                if (m_pool[i].IsActive) m_pool[i].Tick(dt, camPos);
            }
        }

        /// <summary>
        /// effectIndex 프리셋을 position 에서 재생 (방향 없음·등배·정지). 탄착/폭발 등 무지향 이펙트용.
        /// </summary>
        public void Play(int effectIndex, Vector3 position)
            => Play(effectIndex, position, Quaternion.identity, 1f, Vector3.zero);

        /// <summary>
        /// effectIndex 프리셋을 position 에서 재생. 각 emitter 를 spawnCount 회 풀 슬롯에 스폰하고 랜덤 범위를 적용.
        /// index 가 범위 밖이거나 emitter 가 비어 있으면 무시 (NONE=0 처리 포함). 풀 포화 시 drop.
        ///
        /// 무기 발사 이펙트(머즐/연기/탄피)용 파라미터:
        ///  - orientation: emitter 의 positionOffset/velocity 를 회전시키는 기준 (보통 무기 attach-root 회전). 빌보드와는 무관(빌보드는 항상 카메라 향함).
        ///  - sizeScale: emitter.size 에 곱하는 per-weapon 크기 (muzzleFlashSize/shellSize). 프리셋 size=1.0 은 중립 배수. sizeRate 는 원본대로 절대값 유지.
        ///  - worldExtraVelocity: 회전과 무관한 월드 기본 속도(탄피 배출 = direction×speed). emitter.velocity 에 가산.
        /// </summary>
        public void Play(int effectIndex, Vector3 position, Quaternion orientation, float sizeScale, Vector3 worldExtraVelocity)
        {
            if (m_pool == null) return;

            List<EffectData> all = DataManager.Instance.EffectParameterData?.effectData;
            if (all == null || effectIndex < 0 || effectIndex >= all.Count) return;

            List<EffectEmitter> emitters = all[effectIndex].emitters;
            if (emitters == null) return;

            for (int e = 0; e < emitters.Count; e++)
            {
                EffectEmitter em  = emitters[e];
                Material      mat = GetMaterial(em.textureIndex);
                if (mat == null) continue;

                for (int s = 0; s < em.spawnCount; s++)
                {
                    Effect slot = GetIdleSlot();
                    if (slot == null) return; // 풀 포화 — 이번 재생의 잔여 입자 drop

                    Vector3 pos = position
                                + orientation * (em.positionOffset + RandomVector(em.positionRandomRange));
                    Vector3 vel = orientation * (em.velocity + RandomVector(em.velocityRandomRange))
                                + worldExtraVelocity;
                    float rot     = em.rotationDeg     + RandomRange(em.rotationRandomRange);
                    float rotRate = em.rotationRateDeg + RandomRange(em.rotationRateRandomRange);
                    float size    = (em.size + RandomRange(em.sizeRandomRange)) * sizeScale;

                    slot.Init(mat, pos, vel, em.gravityY, rot, rotRate, size, em.sizeRate,
                              em.alpha, em.alphaRate, em.lifetime, em.flags, orientation);
                }
            }
        }

        /// <summary>
        /// 혈흔 입자가 Block 에 닿았을 때 Effect 가 호출 — wallBloodEffectIndex 데칼을 충돌 지점에 벽 법선 방향(NoBillboard)으로 생성.
        /// 원본 OpenXOPS AddMapEffect (objectmanager.cpp:2869) 대응.
        /// </summary>
        public void SpawnWallBlood(Vector3 point, Vector3 normal)
        {
            EffectGeneralData general = DataManager.Instance.EffectParameterData?.effectGeneralData;
            if (general == null) return;

            // 벽 법선을 향하는 고정 회전. 바닥/천장(법선이 수직)일 때 LookRotation 퇴화 방지로 up 기준을 교체.
            Vector3    up       = Mathf.Abs(normal.y) > 0.99f ? Vector3.forward : Vector3.up;
            Quaternion fixedRot = Quaternion.LookRotation(normal, up);

            // Play 의 orientation 이 곧 NoBillboard 쿼드의 고정 회전으로 Effect.Init 에 전달됨.
            Play(general.wallBloodEffectIndex, point + normal * k_decalSurfaceOffset, fixedRot, 1f, Vector3.zero);
        }

        /// <summary>
        /// 활성 이펙트를 전부 즉시 회수. 맵/미션 전환 시 호출 (BulletManager/SoundManager.ClearPool 과 동일 역할).
        /// </summary>
        public void ClearPool()
        {
            if (m_pool == null) return;
            for (int i = 0; i < m_pool.Length; i++)
            {
                if (m_pool[i].IsActive) m_pool[i].Deactivate();
            }
        }

        private Effect GetIdleSlot()
        {
            for (int i = 0; i < m_pool.Length; i++)
            {
                if (!m_pool[i].IsActive) return m_pool[i];
            }
            return null;
        }

        // 텍스처 인덱스별 머티리얼 캐시 — EffectMaterial 베이스 복제 + EffectGeneralData.texturePaths 텍스처. (BulletManager.GetBulletMaterial 미러)
        private Material GetMaterial(int textureIndex)
        {
            if (m_materialCache.TryGetValue(textureIndex, out var cached)) return cached;

            EffectGeneralData general = DataManager.Instance.EffectParameterData?.effectGeneralData;
            if (general?.texturePaths == null ||
                textureIndex < 0 || textureIndex >= general.texturePaths.Count) return null;

            Material baseMat = MaterialManager.Instance.EffectMaterial;
            if (baseMat == null)
            {
                Debugger.LogError("MaterialManager.EffectMaterial is not assigned.", this, nameof(EffectManager));
                return null;
            }

            string    fullPath = SafePath.Combine(Application.streamingAssetsPath, general.texturePaths[textureIndex]);
            Texture2D tex      = ImageLoader.LoadTexture(fullPath);
            if (tex == null) return null;
            tex.name = Path.GetFileName(fullPath);

            Material mat = new Material(baseMat);
            mat.mainTexture = tex;
            mat.name        = tex.name;

            m_materialCache[textureIndex] = mat;
            return mat;
        }

        private static Vector3 RandomVector(Vector3 range) => new Vector3(
            Random.Range(-range.x, range.x),
            Random.Range(-range.y, range.y),
            Random.Range(-range.z, range.z));

        private static float RandomRange(float range) => Random.Range(-range, range);
    }
}
