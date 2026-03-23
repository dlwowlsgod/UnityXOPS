using UnityEngine;
using System.Collections;

namespace UnityXOPS
{
    /// <summary>
    /// 브리핑 씬에서 BRIEFING 텍스트 펄스와 클릭 유도 텍스트 스프레드 펄스 애니메이션을 실행하는 컴포넌트.
    /// </summary>
    public class BriefingPulseTextSequence : MonoBehaviour
    {
        [SerializeField]
        private RectTransform briefingRoot, clickToNextRoot;

        [SerializeField]
        private Vector2 briefingFontSize, clickToNextStartFontSize, clickToNextEndFontSize;
        [SerializeField]
        private Color32 briefingColor, clickToNextColor;
        [SerializeField]
        private float briefingDuration, briefingStartAlpha, briefingEndAlpha, 
            clickToNextDuration, clickToNextStartAlpha, clickToNextEndAlpha;

        /// <summary>
        /// 브리핑 및 클릭 유도 텍스트를 생성하고 펄스 애니메이션 코루틴을 시작한다.
        /// </summary>
        private void Start()
        {
            var briefingText = FontManager.CreateSpriteText<XOPSSpriteTextPulse>(
                briefingRoot, "BRIEFING", new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero,
                briefingFontSize, briefingColor, TextAnchor.UpperCenter, 0);

            var constantClickToNextText = FontManager.CreateSpriteText<XOPSSpriteText>(
                clickToNextRoot, "LEFT CLICK TO BEGIN", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                Vector2.zero, clickToNextStartFontSize, clickToNextColor, TextAnchor.MiddleCenter, 0);
            var pulseClickToNextText = FontManager.CreateSpriteText<XOPSSpriteTextPulseSpread>(
                clickToNextRoot, "LEFT CLICK TO BEGIN", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                Vector2.zero, clickToNextStartFontSize, clickToNextColor, TextAnchor.MiddleCenter, 0);

            StartCoroutine(PulseRoutine(briefingText));
            StartCoroutine(PulseSpreadRoutine(pulseClickToNextText));
        }

        /// <summary>
        /// 한 프레임 대기 후 브리핑 텍스트 펄스를 시작하는 코루틴.
        /// </summary>
        private IEnumerator PulseRoutine(XOPSSpriteTextPulse text)
        {
            yield return null;
            text.StartPulse(briefingDuration, briefingStartAlpha, briefingEndAlpha);
        }
        /// <summary>
        /// 한 프레임 대기 후 클릭 유도 텍스트 스프레드 펄스를 시작하는 코루틴.
        /// </summary>
        private IEnumerator PulseSpreadRoutine(XOPSSpriteTextPulseSpread text)
        {
            yield return null;
            text.StartPulseSpread(clickToNextDuration, clickToNextStartAlpha, clickToNextEndAlpha,
                clickToNextStartFontSize.x, clickToNextStartFontSize.y, clickToNextEndFontSize.x, clickToNextEndFontSize.y);
        }
    }
}
