using JJLUtility;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 풀링된 AudioSource 로 3D 위치 효과음을 재생하는 싱글톤. 격발/폭발/피격/재장전 등 모든 SFX 의 토대.
    /// 원본 OpenXOPS SoundManager(soundlist 큐 → PlayWorldSound → Play3DSound) 의 Unity 대응.
    /// 거리 감쇠는 원본 선형(volume = max × (1 − dist/335)) 재현: Linear rolloff, maxDistance 33.5 유닛, 도플러 0, 패닝 최소(원본 pan=0).
    /// 음원 = 발생 오브젝트(무기 등) 월드 위치. 리스너 = 메인 카메라의 AudioListener.
    /// </summary>
    public class SoundManager : SingletonBehavior<SoundManager>
    {
        // 원본 MAX_SOUNDDIST 335 × 0.1 (Unity scale)
        private const float k_maxDistance = 33.5f;
        // 이 거리 내는 풀볼륨 — 플레이어 무기(카메라 근처)가 풀볼륨으로 들리도록 보장 (원본 플레이어 2D 풀볼륨 대응).
        private const float k_minDistance = 1.0f;
        private const int   k_poolSize    = 24;

        private AudioSource[] m_pool;
        private int           m_next;

        protected override void Awake()
        {
            base.Awake();

            m_pool = new AudioSource[k_poolSize];
            for (int i = 0; i < k_poolSize; i++)
            {
                var obj = new GameObject($"SfxSource_{i}");
                obj.transform.SetParent(transform, false);

                var src = obj.AddComponent<AudioSource>();
                src.playOnAwake  = false;
                src.spatialBlend = 1f;                       // 3D
                src.rolloffMode  = AudioRolloffMode.Linear;  // 원본 선형 감쇠
                src.minDistance  = k_minDistance;
                src.maxDistance  = k_maxDistance;
                src.dopplerLevel = 0f;                       // 도플러 없음
                src.spread       = 180f;                     // 패닝 최소 (원본 pan=0)
                m_pool[i] = src;
            }
        }

        /// <summary>
        /// 지정 월드 위치에서 clip 을 3D 재생. volume 은 0~1. 비어있는 소스를 우선 사용, 모두 사용 중이면 라운드로빈으로 가장 오래된 소스를 재활용한다.
        /// </summary>
        /// <param name="clip">재생할 오디오 클립. null 이면 무시.</param>
        /// <param name="position">음원 월드 위치.</param>
        /// <param name="volume">0~1 볼륨. 0 이하면 무시.</param>
        public void PlayAt(AudioClip clip, Vector3 position, float volume)
        {
            if (clip == null || volume <= 0f || m_pool == null) return;

            AudioSource src = GetSource();
            src.transform.position = position;
            src.clip             = clip;
            src.volume           = Mathf.Clamp01(volume);
            src.Play();
        }

        private AudioSource GetSource()
        {
            for (int i = 0; i < m_pool.Length; i++)
            {
                if (!m_pool[i].isPlaying) return m_pool[i];
            }
            // 전부 재생 중 — 라운드로빈으로 가장 오래된 소스 재활용(가장 오래된 재생 중단).
            AudioSource src = m_pool[m_next];
            m_next = (m_next + 1) % m_pool.Length;
            return src;
        }
    }
}
