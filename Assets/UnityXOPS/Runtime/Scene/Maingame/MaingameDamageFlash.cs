using UnityEngine;
using UnityEngine.UI;

namespace UnityXOPS
{
    /// <summary>
    /// 플레이어 피격 시 화면 전체를 빨갛게 번쩍이는 효과 (원본 OpenXOPS gamemain.cpp:2690-2691·2928-2932 redflash).
    /// 트리거는 HP 감소가 아니라 피탄 이벤트 플래그(Human.ConsumeHit = 원본 CheckHit 소비형) — 데미지 0(방어) 피탄도 번쩍.
    /// AI 는 플레이어를 스킵하므로 플레이어의 피탄 플래그는 이 효과가 유일하게 소비한다(원본에서 플레이어측 CheckHit 소비자 = redflash 와 동일).
    /// 원본은 알파 0.5 빨강 전체화면 박스를 1프레임(≈30ms) 그리고 즉시 끄는 단발 번쩍(페이드 없음). 가시성을 위해
    /// flashHoldTime(최대 유지) 뒤 fadeTime(선형 감쇠)을 옵션으로 둔다 — fadeTime 0 이면 원본과 동일한 단발 번쩍.
    /// </summary>
    public class MaingameDamageFlash : MonoBehaviour
    {
        [SerializeField] private RawImage flashImage; // 전체화면 오버레이 (Inspector 할당, raycastTarget off 권장)
        [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 0.5f); // 원본 R1 G0 B0 A0.5
        [SerializeField] private float flashHoldTime = 0.05f; // 최대 알파 유지 시간(초). 원본 ≈ 1프레임(0.03).
        [SerializeField] private float fadeTime = 0.1f; // 감쇠 시간(초). 0 이면 원본처럼 페이드 없는 단발 번쩍.

        private Human m_player;
        private float m_timer; // 남은 플래시 시간 (hold + fade)

        private void Start()
        {
            if (flashImage != null)
            {
                if (flashImage.texture == null) flashImage.texture = Texture2D.whiteTexture; // 솔리드 컬러용
                flashImage.raycastTarget = false;
            }

            m_player = MapLoader.Player;
            if (m_player != null) m_player.ConsumeHit(out _); // 진입 시 잔여 피탄 플래그 소비 (첫 프레임 오탐 방지)
            Apply(0f);
        }

        private void Update()
        {
            Human player = MapLoader.Player;

            // 플레이어 교체(F8)/최초 취득 — 새 플레이어의 잔여 피탄 플래그를 소비해 교체 프레임 오탐 방지.
            if (player != m_player)
            {
                m_player = player;
                if (m_player != null) m_player.ConsumeHit(out _);
            }

            // 피탄 플래그 소비 → 살아있으면 플래시 트리거 (원본 CheckHit). 시체 피탄은 소비만 하고 번쩍 안 함.
            if (m_player != null)
            {
                bool hit = m_player.ConsumeHit(out _);
                if (hit && m_player.Alive) m_timer = flashHoldTime + fadeTime;
            }

            float intensity = 0f;
            if (m_timer > 0f)
            {
                m_timer -= Time.deltaTime;
                if (m_timer < 0f) m_timer = 0f;
                // hold 구간은 최대(1), fade 구간은 남은시간/fadeTime 으로 선형 감쇠.
                intensity = (fadeTime > 0f && m_timer < fadeTime) ? (m_timer / fadeTime) : 1f;
            }
            Apply(intensity);
        }

        // 플래시 강도(0~1)를 오버레이 알파에 적용. 0 이면 오브젝트 비활성(렌더 스킵).
        private void Apply(float intensity)
        {
            if (flashImage == null) return;

            bool active = intensity > 0f;
            if (flashImage.gameObject.activeSelf != active) flashImage.gameObject.SetActive(active);

            if (active)
            {
                Color c = flashColor;
                c.a = flashColor.a * intensity;
                flashImage.color = c;
            }
        }
    }
}
