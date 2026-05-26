using JJLUtility;
using JJLUtility.IO;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// Bullet 풀의 사전할당, Spawn, 매 프레임 Tick, 메시/머티리얼 캐시를 관리하는 싱글톤.
    /// 풀 크기 = 원본 OpenXOPS MAX_BULLET(128) + MAX_GRENADE(32) = 160 (단일 Bullet 컴포넌트로 통합).
    /// 원본 ObjectManager 의 bullet/grenade 풀 + Process() 매 프레임 순회 패턴 미러.
    /// </summary>
    public class BulletManager : SingletonBehavior<BulletManager>
    {
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private int        poolSize = 160;

        private Bullet[]                     m_pool;
        private Dictionary<string, Mesh>     m_meshCache     = new Dictionary<string, Mesh>();
        private Dictionary<string, Material> m_materialCache = new Dictionary<string, Material>();

        protected override void Awake()
        {
            base.Awake();
            if (bulletPrefab == null)
            {
                Debugger.LogError("BulletManager.bulletPrefab is not assigned.", this, nameof(BulletManager));
                return;
            }
            InitializePool();
        }

        private void InitializePool()
        {
            m_pool = new Bullet[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                GameObject obj = Instantiate(bulletPrefab, transform);
                obj.name = $"Bullet_{i}";
                obj.SetActive(false);
                m_pool[i] = obj.GetComponent<Bullet>();
            }
        }

        private void Update()
        {
            if (m_pool == null) return;

            float dt = Time.deltaTime;
            for (int i = 0; i < m_pool.Length; i++)
            {
                if (m_pool[i].IsActive) m_pool[i].Tick(dt);
            }
        }

        /// <summary>
        /// 풀에서 idle 슬롯을 찾아 발사 상태로 활성화. 풀 포화 시 null 반환 (원본: ShotWeapon 자체가 무효 처리).
        /// </summary>
        public Bullet Spawn(BulletData data, Human owner, int team,
                            int attacks, int penetration,
                            Vector3 position, Vector3 velocity)
        {
            if (data == null) return null;

            Bullet slot = GetIdleSlot();
            if (slot == null) return null;

            Mesh     mesh     = GetBulletMesh(data.modelPath);
            Material material = GetBulletMaterial(data.texturePath);

            slot.gameObject.SetActive(true);
            slot.ApplyVisual(mesh, material, data.modelPosition, data.modelRotation, data.modelScale);
            slot.Spawn(data, owner, team, attacks, penetration, position, velocity);
            return slot;
        }

        // first-fit 선형 스캔 — 원본 GetNewBulletObject (objectmanager.cpp:1737-1747).
        private Bullet GetIdleSlot()
        {
            for (int i = 0; i < m_pool.Length; i++)
            {
                if (!m_pool[i].IsActive) return m_pool[i];
            }
            return null;
        }

        /// <summary>
        /// 풀의 모든 활성 슬롯을 즉시 회수. 씬/미션 전환 시 호출 (이전 미션의 탄이 다음 미션에 잔존하지 않도록).
        /// </summary>
        public void ClearPool()
        {
            if (m_pool == null) return;
            for (int i = 0; i < m_pool.Length; i++)
            {
                if (m_pool[i].IsActive) m_pool[i].Deactivate();
            }
        }

        public Mesh GetBulletMesh(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (m_meshCache.TryGetValue(path, out var cached)) return cached;

            string fullPath = SafePath.Combine(Application.streamingAssetsPath, path);
            Mesh mesh = ModelLoader.LoadMesh(fullPath);
            if (mesh != null) m_meshCache[path] = mesh;
            return mesh;
        }

        public Material GetBulletMaterial(string texturePath)
        {
            if (string.IsNullOrEmpty(texturePath)) return MaterialManager.Instance.MainMaterial;
            if (m_materialCache.TryGetValue(texturePath, out var cached)) return cached;

            string fullPath = SafePath.Combine(Application.streamingAssetsPath, texturePath);
            Texture2D tex   = ImageLoader.LoadTexture(fullPath);
            if (tex == null) return MaterialManager.Instance.MainMaterial;
            tex.name = Path.GetFileName(fullPath);

            Material mat   = new Material(MaterialManager.Instance.MainMaterial);
            mat.mainTexture = tex;
            mat.name        = tex.name;

            m_materialCache[texturePath] = mat;
            return mat;
        }
    }
}
