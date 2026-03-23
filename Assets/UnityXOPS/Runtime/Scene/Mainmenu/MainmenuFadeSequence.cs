using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UnityXOPS
{
    /// <summary>
    /// 메인메뉴 진입 시 페이드 인 애니메이션과 클릭 허용 타이밍을 제어하는 컴포넌트.
    /// </summary>
    public class MainmenuFadeSequence : MonoBehaviour
    {
        [SerializeField]
        private FadeRawImage fadeRawImage;
        [SerializeField]
        private float fadeTime = 2f, clickAllowTime = 0.2f;

        private float m_time;

        /// <summary>
        /// 페이드 및 클릭 허용 코루틴을 시작한다.
        /// </summary>
        private void Start()
        {
            m_time = Time.time;
            StartCoroutine(FadeRoutine());
            StartCoroutine(ClickAllowRoutine());
        }

        /// <summary>
        /// 화면을 완전 불투명 상태에서 페이드 인하는 코루틴.
        /// </summary>
        private IEnumerator FadeRoutine()
        {
            fadeRawImage.SetAlphaOne();

            yield return new WaitUntil(() => Time.time - m_time >= 0);
            fadeRawImage.FadeIn(fadeTime);
        }

        /// <summary>
        /// 일정 시간 후 페이드 RawImage의 레이캐스트 타깃을 비활성화하여 클릭을 허용하는 코루틴.
        /// </summary>
        private IEnumerator ClickAllowRoutine()
        {
            yield return new WaitUntil(() => Time.time - m_time >= clickAllowTime);
            fadeRawImage.GetComponent<RawImage>().raycastTarget = false;
        }
    }
}
