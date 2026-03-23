using UnityEngine;
using System.Collections;

namespace UnityXOPS
{
    /// <summary>
    /// 알파 페이드 인/아웃 애니메이션 기능이 추가된 스프라이트 텍스트 컴포넌트.
    /// </summary>
    public class XOPSSpriteTextFade : XOPSSpriteText
    {
        // alpha를 0으로 설정
        /// <summary>
        /// 텍스트 알파값을 즉시 0으로 설정한다.
        /// </summary>
        public void SetAlphaZero()
        {
            color = new Color(color.r, color.g, color.b, 0f);
        }

        // alpha를 1로 설정
        /// <summary>
        /// 텍스트 알파값을 즉시 1로 설정한다.
        /// </summary>
        public void SetAlphaOne()
        {
            color = new Color(color.r, color.g, color.b, 1f);
        }

        // alpha 0 → 1 (페이드 인)
        /// <summary>
        /// 지정 시간 동안 알파를 0에서 1로 서서히 증가시킨다(페이드 인).
        /// </summary>
        public void FadeIn(float duration)
        {
            StopAllCoroutines();
            StartCoroutine(FadeRoutine(0f, 1f, duration));
        }

        // alpha 1 → 0 (페이드 아웃)
        /// <summary>
        /// 지정 시간 동안 알파를 1에서 0으로 서서히 감소시킨다(페이드 아웃).
        /// </summary>
        public void FadeOut(float duration)
        {
            StopAllCoroutines();
            StartCoroutine(FadeRoutine(1f, 0f, duration));
        }

        /// <summary>
        /// 알파값을 from에서 to로 duration 시간에 걸쳐 선형 보간하는 코루틴.
        /// </summary>
        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            color = new Color(color.r, color.g, color.b, from);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                color = new Color(color.r, color.g, color.b, Mathf.Lerp(from, to, t));
                yield return null;
            }

            color = new Color(color.r, color.g, color.b, to);
        }
    }
}
