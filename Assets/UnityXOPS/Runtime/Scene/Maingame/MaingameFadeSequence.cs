using System.Collections;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 메인게임 씬의 페이드 인/아웃 시퀀스를 제어하는 컴포넌트.
    /// </summary>
    public class MaingameFadeSequence : MonoBehaviour
    {
        [SerializeField]
        private FadeRawImage fadeRawImage;
        [SerializeField]
        private RectTransform missionEndText;
        [SerializeField]
        private float fadeInTime, fadeOutTime, endTextFadeInTime, endTextHoldTime, endTextFadeOutTime;

        private XOPSSpriteTextFade m_missionEndText;

        // 종료 시퀀스(화면 암전 + 종료 텍스트) 완료 플래그 — 둘 중 더 긴 쪽이 끝나면 true. MaingameScene 이 Result 전환 트리거로 폴링.
        private bool m_missionEndComplete;
        public  bool MissionEndComplete => m_missionEndComplete;

        /// <summary>
        /// 페이드 인 시퀀스를 시작한다.
        /// </summary>
        private void Start()
        {
            StartCoroutine(FadeInRoutine());
        }

        /// <summary>
        /// 검은색에서 페이드 인하는 코루틴을 실행한다.
        /// </summary>
        private IEnumerator FadeInRoutine()
        {
            fadeRawImage.SetAlphaOne();
            yield return null;
            fadeRawImage.FadeIn(fadeInTime);
        }

        /// <summary>
        /// 페이드 아웃 시퀀스를 시작한다.
        /// </summary>
        public void FadeOut()
        {
            StartCoroutine(FadeOutRoutine());
        }

        private IEnumerator FadeOutRoutine()
        {
            fadeRawImage.SetAlphaZero();
            yield return null;
            fadeRawImage.FadeOut(fadeOutTime);
        }

        /// <summary>
        /// 미션 종료 시퀀스 시작 — 화면 암전(FadeOut) + 종료 텍스트(MissionEndText)를 함께 가동하고,
        /// 두 페이드 중 더 긴 쪽이 끝나면 MissionEndComplete=true (MaingameScene 의 Result 전환 신호).
        /// 매 프레임 호출돼도 1회만 (m_missionEndText 가드).
        /// </summary>
        public void PlayMissionEndSequence()
        {
            if (m_missionEndText != null) return;

            FadeOut();
            MissionEndText();
            StartCoroutine(MissionEndCompleteWatcher());
        }

        /// <summary>화면 암전(fadeOutTime)과 종료 텍스트(인+유지+아웃) 중 더 긴 쪽이 끝날 때까지 대기 후 완료 신호.</summary>
        private IEnumerator MissionEndCompleteWatcher()
        {
            float screenTotal = fadeOutTime;
            float textTotal   = endTextFadeInTime + endTextHoldTime + endTextFadeOutTime;
            yield return new WaitForSeconds(Mathf.Max(screenTotal, textTotal));
            m_missionEndComplete = true;
        }

        public void MissionEndText()
        {
            if (m_missionEndText != null) return; // 이미 표시 중 — 중복 생성/시퀀스 방지 (Result 폴링으로 매 프레임 호출돼도 안전)

            Vector2 center = new Vector2(0.5f, 0.5f);
            Vector2 fontSize = new Vector2(28f, 32f);

            string text = EventManager.Instance.Result == MissionResult.Complete ? "objective complete" : "mission failure";
            Color32 color = EventManager.Instance.Result == MissionResult.Complete ? new Color(1f, 0.5f, 0f, 1.0f) : new Color(1f, 0f, 0f, 1.0f);
            m_missionEndText = FontManager.CreateSpriteText<XOPSSpriteTextFade>(
                missionEndText, text, center, center, Vector2.zero, Vector2.zero, fontSize, color, TextAnchor.MiddleCenter, 0f);

            StartCoroutine(MissionEndTextRoutine());
        }

        /// <summary>
        /// 종료 텍스트 페이드 시퀀스 — 페이드 인 → 유지 → 페이드 아웃. 원본 maingame::Render2D (gamemain.cpp:3310-3319)
        /// 의 인1초/유지2초/아웃1초(총 4초) 대응. 각 시간은 인스펙터(endTextFadeInTime/HoldTime/FadeOutTime)에서 조절.
        /// </summary>
        private IEnumerator MissionEndTextRoutine()
        {
            m_missionEndText.FadeIn(endTextFadeInTime);
            yield return new WaitForSeconds(endTextFadeInTime + endTextHoldTime);
            m_missionEndText.FadeOut(endTextFadeOutTime);
        }

        /// <summary>
        /// 미션 재시작(F12) 시 호출 — 진행 중인 페이드/종료텍스트 코루틴 정리, 이전 종료 텍스트 제거, 검은 화면 다시 페이드 인.
        /// 안 하면 종료로 어두워진 화면이 그대로 남고, m_missionEndText 가 non-null 이라 다음 종료 시 텍스트가 안 뜬다.
        /// </summary>
        public void ResetForRestart()
        {
            StopAllCoroutines();
            m_missionEndComplete = false; // 재시작 시 완료 신호 리셋 (안 하면 즉시 Result 로 튕김)

            if (m_missionEndText != null)
            {
                Destroy(m_missionEndText.gameObject);
                m_missionEndText = null;
            }

            StartCoroutine(FadeInRoutine()); // 검은 화면 → 다시 페이드 인 (진입 시와 동일)
        }
    }
}
