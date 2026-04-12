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
        private float fadeInTime  = 1f;
        [SerializeField]
        private float fadeOutTime = 1f;

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
            fadeRawImage.FadeOut(fadeOutTime);
        }
    }
}
