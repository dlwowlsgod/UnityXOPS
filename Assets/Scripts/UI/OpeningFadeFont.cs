using UnityEngine;
using UnityEngine.UI;

namespace UnityXOPS
{
    /// <summary>
    /// 오프닝의 게임폰트 관련 클래스입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="GameFont">GameFont</see> 클래스를 상속받습니다.
    /// </remarks>
    public class OpeningFadeFont : GameFont
    {
        [SerializeField] 
        private float textAppearStart;
        [SerializeField]
        private float textAppearEnd;
        [SerializeField]
        private float textDisappearStart;
        [SerializeField]
        private float textDisappearEnd;

        private Color _transparentColor;
        private Color _visibleColor;
        private Color _shadowTransparentColor;
        private Color _shadowVisibleColor;
        protected override void Start()
        {
            base.Start();
            var tC = Text.color;
            _transparentColor = new Color(tC.r, tC.g, tC.b, 0.0f);
            _visibleColor = Text.color;
            var stc = ShadowText.color;
            _shadowTransparentColor = new Color(stc.r, stc.g, stc.b, 0.0f);
            _shadowVisibleColor = ShadowText.color;
        }

        private void Update()
        {
            var clock = Clock.Instance.Process;
            if (clock < textAppearStart)
            {
                Text.color = _transparentColor;
                ShadowText.color = _shadowTransparentColor;
            }

            if (clock >= textAppearStart && clock < textAppearEnd)
            {
                var lerp = Mathf.InverseLerp(textAppearStart, textAppearEnd, clock);
                Text.color = Color.Lerp(_transparentColor, _visibleColor, lerp);
                ShadowText.color = Color.Lerp(_shadowTransparentColor, _shadowVisibleColor, lerp);
            }
            
            if (clock >= textAppearEnd && clock < textDisappearStart)
            {
                Text.color = _visibleColor;
                ShadowText.color = _shadowVisibleColor;
            }
            
            if (clock >= textDisappearStart && clock < textDisappearEnd)
            {
                var lerp = Mathf.InverseLerp(textDisappearStart, textDisappearEnd, clock);
                Text.color = Color.Lerp(_visibleColor, _transparentColor, lerp);
                ShadowText.color = Color.Lerp(_shadowVisibleColor, _shadowTransparentColor, lerp);
            }

            if (clock >= textDisappearEnd)
            {
                Text.color = _transparentColor;
                ShadowText.color = _shadowTransparentColor;
            }
            
            Text.ForceMeshUpdate();
            ShadowText.ForceMeshUpdate();
        }
    }
}
