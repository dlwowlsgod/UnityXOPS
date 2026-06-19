using UnityEngine;
using System.Collections;

namespace UnityXOPS
{
    /// <summary>
    /// 오프닝 씬의 페이드 인/아웃 시퀀스를 타이밍에 맞춰 실행하는 컴포넌트.
    /// </summary>
    public class OpeningFadeSequence : MonoBehaviour
    {
        [SerializeField]
        private FadeRawImage fadeRawImage;

        private float m_time;

        private void Start()
        {
            m_time = Time.time;

            OpeningFadeData data = GetComponent<OpeningScene>().OpeningData.openingFadeData;
            StartCoroutine(FadeRoutine(data));
        }

        /// <summary>
        /// 인자 data 의 타이밍에 따라 페이드 인 후 페이드 아웃을 순차 실행하는 코루틴.
        /// </summary>
        private IEnumerator FadeRoutine(OpeningFadeData data)
        {
            fadeRawImage.SetAlphaOne();

            yield return new WaitUntil(() => Time.time - m_time >= data.fadeInStart);
            fadeRawImage.FadeIn(data.fadeInEnd - data.fadeInStart);

            yield return new WaitUntil(() => Time.time - m_time >= data.fadeOutStart);
            fadeRawImage.FadeOut(data.fadeOutEnd - data.fadeOutStart);
        }
    }
}
