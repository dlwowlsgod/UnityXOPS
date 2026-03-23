using UnityEngine;
using System.Collections;

namespace UnityXOPS
{
    /// <summary>
    /// 알파값과 문자 크기가 동시에 변화하는 스프레드 펄스 애니메이션 기능이 추가된 스프라이트 텍스트 컴포넌트.
    /// </summary>
    public class XOPSSpriteTextPulseSpread : XOPSSpriteText
    {
        // 스프레드 펄스 시작
        /// <summary>
        /// 알파값과 문자 크기가 함께 변화하는 스프레드 펄스 애니메이션을 시작한다.
        /// </summary>
        public void StartPulseSpread(float duration, float alphaFrom, float alphaTo,
                                     float startWidth,  float endWidth,
                                     float startHeight, float endHeight)
        {
            StopAllCoroutines();
            StartCoroutine(PulseSpreadRoutine(duration, alphaFrom, alphaTo, startWidth, endWidth, startHeight, endHeight));
        }

        /// <summary>
        /// 알파와 문자 크기를 시작값에서 끝값으로 duration마다 반복 보간하는 코루틴.
        /// </summary>
        private IEnumerator PulseSpreadRoutine(float duration, float alphaFrom, float alphaTo,
                                               float startWidth,  float endWidth,
                                               float startHeight, float endHeight)
        {
            while (true)
            {
                color      = new Color(color.r, color.g, color.b, alphaFrom);
                CharWidth  = startWidth;
                CharHeight = startHeight;

                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    color      = new Color(color.r, color.g, color.b, Mathf.Lerp(alphaFrom, alphaTo, t));
                    CharWidth  = Mathf.Lerp(startWidth,  endWidth,  t);
                    CharHeight = Mathf.Lerp(startHeight, endHeight, t);
                    yield return null;
                }

                // 다음 프레임에 시작값으로 리셋
                yield return null;
            }
        }
    }
}
