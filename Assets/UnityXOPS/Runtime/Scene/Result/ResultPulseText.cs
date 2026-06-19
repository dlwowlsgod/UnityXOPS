using UnityEngine;
using System.Collections;

namespace UnityXOPS
{
    /// <summary>
    /// 결과 씬에서 RESULT 텍스트 펄스 애니메이션을 실행하는 컴포넌트.
    /// </summary>
    public class ResultPulseText : MonoBehaviour
    {
        [SerializeField]
        private RectTransform resultRoot;

        [SerializeField]
        private Vector2 resultFontSize;
        [SerializeField]
        private Color32 resultColor;
        [SerializeField]
        private float resultDuration, resultStartAlpha, resultEndAlpha;

        private void Start()
        {
            var resultText = FontManager.CreateSpriteText<XOPSSpriteTextPulse>(
                resultRoot, "RESULT", new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero,
                resultFontSize, resultColor, TextAnchor.UpperCenter, 0);

            StartCoroutine(PulseRoutine(resultText));
        }

        /// <summary>
        /// 한 프레임 대기 후 인자 text 스프라이트에 결과 펄스를 시작하는 코루틴.
        /// </summary>
        private IEnumerator PulseRoutine(XOPSSpriteTextPulse text)
        {
            yield return null;
            text.StartPulse(resultDuration, resultStartAlpha, resultEndAlpha);
        }
    }
}
