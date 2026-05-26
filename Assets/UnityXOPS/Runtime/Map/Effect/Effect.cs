using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 풀에서 관리되는 단일 이펙트 빌보드 쿼드. EffectManager 가 매 프레임 활성 슬롯만 Tick 호출 (MonoBehaviour Update 비활성).
    /// 원본 OpenXOPS effect 클래스(object.cpp:3049-3231) 의 ProcessObject/Render 를 단일 컴포넌트로 통합.
    /// 위치/속도/중력/크기율/알파율/회전율을 매 프레임 적분하고, 카메라를 향한 빌보드로 그린다.
    /// </summary>
    public class Effect : MonoBehaviour
    {
        // 빌보드 LookRotation 안정화용 — 카메라와 거의 겹칠 때 회전 미적용.
        private const float k_lookEpsilon = 1e-6f;

        private MeshRenderer          m_renderer;
        private MaterialPropertyBlock m_mpb;
        private int                   m_colorId;

        private Vector3 m_velocity;
        private float   m_gravityY;
        private float   m_rotation;        // deg — 텍스처(보드) 자체 회전
        private float   m_rotationRate;    // deg/sec
        private float   m_size;
        private float   m_sizeRate;        // size/sec
        private float   m_alpha;
        private float   m_alphaRate;       // /sec
        private float      m_lifetime;
        private bool       m_billboard;
        private bool       m_collideMap;       // Block 에 닿으면 벽 데칼로 전환 (혈흔 입자)
        private Quaternion m_fixedRotation;    // NoBillboard 일 때 쿼드 고정 회전 (벽 데칼 = 벽 법선 방향)
        private bool       m_active;

        public bool IsActive => m_active;

        private void Awake()
        {
            m_renderer = GetComponent<MeshRenderer>();
            m_mpb      = new MaterialPropertyBlock();
            m_colorId  = Shader.PropertyToID("_Color");
        }

        /// <summary>
        /// EffectManager 가 Spawn 시 호출. 풀 슬롯을 발생 상태로 초기화. position/velocity 는 월드 좌표.
        /// </summary>
        public void Init(Material material, Vector3 position, Vector3 velocity, float gravityY,
                         float rotation, float rotationRate, float size, float sizeRate,
                         float alpha, float alphaRate, float lifetime, EffectFlags flags, Quaternion fixedRotation)
        {
            m_renderer.sharedMaterial = material;

            m_velocity      = velocity;
            m_gravityY      = gravityY;
            m_rotation      = rotation;
            m_rotationRate  = rotationRate;
            m_size          = size;
            m_sizeRate      = sizeRate;
            m_alpha         = alpha;
            m_alphaRate     = alphaRate;
            m_lifetime      = lifetime;
            m_billboard     = (flags & EffectFlags.NoBillboard) == 0;
            m_collideMap    = (flags & EffectFlags.CollideMap)  != 0;
            m_fixedRotation = fixedRotation;
            m_active        = true;

            transform.position = position;
            ApplyTransform(position);
            ApplyAlpha();

            gameObject.SetActive(true);
        }

        /// <summary>
        /// 매 프레임 EffectManager 가 호출. 원본 effect::ProcessObject 미러 — 적분 후 lifetime/alpha/size 소진 시 회수.
        /// </summary>
        public void Tick(float dt, Vector3 cameraPosition)
        {
            if (!m_active) return;

            m_lifetime -= dt;
            if (m_lifetime <= 0f) { Recycle(); return; }

            Vector3 pos = transform.position + m_velocity * dt;

            // 이동 혈흔 입자(CollideMap)가 Block 에 닿으면 그 지점에 벽 법선 방향 데칼 생성 후 소멸 (원본 CollideBlood→AddMapEffect objectmanager.cpp:1281-1320).
            if (m_collideMap)
            {
                Vector3 delta = pos - transform.position;
                float   dist  = delta.magnitude;
                if (dist > k_lookEpsilon &&
                    Physics.Raycast(transform.position, delta / dist, out RaycastHit hit, dist, MapLoader.BlockLayerMask))
                {
                    EffectManager.Instance.SpawnWallBlood(hit.point, hit.normal);
                    Recycle();
                    return;
                }
            }

            m_velocity.y += m_gravityY * dt;

            m_size += m_sizeRate * dt;
            if (m_size <= 0f) { Recycle(); return; }

            // 원본 object.cpp:3196 — alpha<=0 이면 lifetime 전이라도 즉시 소멸.
            m_alpha += m_alphaRate * dt;
            if (m_alpha <= 0f) { Recycle(); return; }

            m_rotation += m_rotationRate * dt;

            ApplyTransform(pos, cameraPosition);
            ApplyAlpha();
        }

        /// <summary>
        /// 외부(EffectManager.ClearPool)에서 강제 회수.
        /// </summary>
        public void Deactivate() => Recycle();

        private void ApplyTransform(Vector3 position) => ApplyTransform(position, position);

        private void ApplyTransform(Vector3 position, Vector3 cameraPosition)
        {
            Quaternion rot;
            if (m_billboard)
            {
                // 카메라를 향한 빌보드(쿼드 normal +Z) + 텍스처 회전(시선축 roll). 원본 object.cpp:3222-3229.
                Vector3 toCamera = cameraPosition - position;
                Quaternion look  = toCamera.sqrMagnitude > k_lookEpsilon
                    ? Quaternion.LookRotation(toCamera, Vector3.up)
                    : Quaternion.identity;
                rot = look * Quaternion.Euler(0f, 0f, m_rotation);
            }
            else
            {
                // NoBillboard — 고정 회전(벽 법선 방향) × 텍스처 회전(roll). 벽/바닥 데칼용.
                rot = m_fixedRotation * Quaternion.Euler(0f, 0f, m_rotation);
            }

            transform.SetPositionAndRotation(position, rot);
            transform.localScale = new Vector3(m_size, m_size, m_size);
        }

        private void ApplyAlpha()
        {
            m_renderer.GetPropertyBlock(m_mpb);
            m_mpb.SetColor(m_colorId, new Color(1f, 1f, 1f, Mathf.Clamp01(m_alpha)));
            m_renderer.SetPropertyBlock(m_mpb);
        }

        private void Recycle()
        {
            m_active = false;
            gameObject.SetActive(false);
        }
    }
}
