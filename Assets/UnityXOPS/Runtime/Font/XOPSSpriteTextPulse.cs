using UnityEngine;
using System.Collections;

namespace UnityXOPS
{
    /// <summary>
    /// 알파값이 주기적으로 변화하는 펄스 애니메이션 기능이 추가된 스프라이트 텍스트 컴포넌트.
    /// </summary>
    public class XOPSSpriteTextPulse : XOPSSpriteText
    {
        // 펄스 시작
        /// <summary>
        /// 알파값이 alphaFrom에서 alphaTo로 duration마다 반복되는 펄스 애니메이션을 시작한다.
        /// </summary>
        public void StartPulse(float duration, float alphaFrom, float alphaTo)
        {
            StopAllCoroutines();
            StartCoroutine(PulseRoutine(duration, alphaFrom, alphaTo));
        }

        // 펄스 중지
        /// <summary>
        /// 실행 중인 펄스 애니메이션을 중지한다.
        /// </summary>
        public void StopPulse()
        {
            StopAllCoroutines();
        }

        /// <summary>
        /// 알파값을 alphaFrom에서 alphaTo로 duration 시간에 걸쳐 반복 변화시키는 코루틴.
        /// </summary>
        private IEnumerator PulseRoutine(float duration, float alphaFrom, float alphaTo)
        {
            while (true)
            {
                color = new Color(color.r, color.g, color.b, alphaFrom);

                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    color = new Color(color.r, color.g, color.b, Mathf.Lerp(alphaFrom, alphaTo, t));
                    yield return null;
                }

                // 다음 프레임에 alphaFrom으로 리셋
                yield return null;
            }
        }
    }
}
