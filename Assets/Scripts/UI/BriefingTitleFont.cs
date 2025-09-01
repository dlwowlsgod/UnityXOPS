using UnityEngine;

namespace UnityXOPS
{
    public class BriefingTitleFont : GameFont
    {
        [SerializeField] 
        private float blinkTime;
        [SerializeField]
        private float minAlpha;
        [SerializeField]
        private float maxAlpha;

        protected virtual void Update()
        {
            var clock = Clock.Instance.Process % blinkTime;
            var ratio = clock / blinkTime;
            var timeLerp = Mathf.InverseLerp(0f, blinkTime, ratio);
            var alpha = Mathf.Lerp(maxAlpha, minAlpha, ratio);
            
            Text.color = new Color(Text.color.r, Text.color.g, Text.color.b, alpha);
            Text.text = fontText;
            Text.text = CharacterToRichText(Text);
            ShadowText.color = new Color(ShadowText.color.r, ShadowText.color.g, ShadowText.color.b, alpha);
            ShadowText.text = fontText;
            ShadowText.text = CharacterToRichText(ShadowText);
        }
    }
}