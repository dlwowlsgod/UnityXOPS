using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UnityXOPS
{
    /// <summary>
    /// RawImage 컴포넌트의 알파값을 페이드 인/아웃으로 제어하는 UI 컴포넌트.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class FadeRawImage : MonoBehaviour
    {
        private RawImage _rawImage;

        /// <summary>
        /// RawImage 참조를 캐싱하고 초기 색상을 검은색으로 설정한다.
        /// </summary>
        private void Awake()
        {
            _rawImage = GetComponent<RawImage>();
            _rawImage.color = Color.black;
        }

        /// <summary>
        /// RawImage 알파값을 즉시 0으로 설정한다.
        /// </summary>
        public void SetAlphaZero()
        {
            var c = _rawImage.color;
            _rawImage.color = new Color(c.r, c.g, c.b, 0f);
        }

        /// <summary>
        /// RawImage 알파값을 즉시 1로 설정한다.
        /// </summary>
        public void SetAlphaOne()
        {
            var c = _rawImage.color;
            _rawImage.color = new Color(c.r, c.g, c.b, 1f);
        }

        /// <summary>
        /// 지정 시간 동안 알파를 0에서 1로 서서히 증가시킨다(페이드 아웃).
        /// </summary>
        public void FadeOut(float duration)
        {
            StopAllCoroutines();
            StartCoroutine(FadeRoutine(0f, 1f, duration));
        }

        /// <summary>
        /// 지정 시간 동안 알파를 1에서 0으로 서서히 감소시킨다(페이드 인).
        /// </summary>
        public void FadeIn(float duration)
        {
            StopAllCoroutines();
            StartCoroutine(FadeRoutine(1f, 0f, duration));
        }

        /// <summary>
        /// 알파값을 from에서 to로 duration 시간에 걸쳐 선형 보간하는 코루틴.
        /// </summary>
        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            var c = _rawImage.color;
            _rawImage.color = new Color(c.r, c.g, c.b, from);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                c = _rawImage.color;
                _rawImage.color = new Color(c.r, c.g, c.b, Mathf.Lerp(from, to, t));
                yield return null;
            }

            c = _rawImage.color;
            _rawImage.color = new Color(c.r, c.g, c.b, to);
        }
    }
}
