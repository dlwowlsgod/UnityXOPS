using TMPro;
using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 게임폰트를 담당하는 클래스입니다.
    /// </summary>
    /// <remarks>
    /// 이 클래스가 덮어씌우는 건 폰트와 텍스트입니다. 이외의 설정은 에디터에서 다뤄야 합니다.
    /// </remarks>
    public class GameFont : MonoBehaviour
    {
        [SerializeField]
        protected GameObject textObject;
        [SerializeField]
        protected GameObject shadowTextObject;
        [SerializeField]
        protected string fontText;
        [SerializeField]
        protected Color fontColor;
        [SerializeField]
        protected TextAlignmentOptions alignment;
        [SerializeField]
        protected Vector4 margin;
        
        protected TextMeshProUGUI Text;
        protected TextMeshProUGUI ShadowText;

        private void Awake()
        {
            Text = textObject.GetComponent<TextMeshProUGUI>();
            ShadowText = shadowTextObject.GetComponent<TextMeshProUGUI>();
        }

        protected virtual void Start()
        {
            Text.font = FontManager.Instance.OSFont;
            Text.fontSizeMin = 10f;
            Text.fontSizeMax = 400f;
            Text.spriteAsset = FontManager.Instance.GameFont;
            Text.color = fontColor;
            Text.text = fontText;
            Text.text = CharacterToRichText(Text);
            Text.alignment = alignment;
            Text.margin = margin;
            
            ShadowText.font = FontManager.Instance.OSFont;
            ShadowText.fontSizeMin = 10f;
            ShadowText.fontSizeMax = 400f;
            ShadowText.spriteAsset = FontManager.Instance.GameFont;
            ShadowText.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            ShadowText.text = fontText;
            ShadowText.text = CharacterToRichText(ShadowText);
            ShadowText.alignment = alignment;
            ShadowText.margin = margin;
            
            Text.ForceMeshUpdate();
            ShadowText.ForceMeshUpdate();
        }

        /// <summary>
        /// 일반 string 텍스트를 게임 폰트에 맞게 변환합니다.
        /// </summary>
        /// <param name="tmp">TMP</param>
        /// <returns>변환된 텍스트</returns>
        protected string CharacterToRichText(TextMeshProUGUI tmp)
        {
            var str = "";
            var color = ColorUtility.ToHtmlStringRGB(tmp.color);
            foreach (var c in tmp.text)
            {
                if (c == '\n')
                {
                    str += "<br>";
                    continue;
                }
                
                if (c < ' ' || c > '~')
                {
                    continue;
                }
                
                str += $"<sprite index={(int)c} color=#{color}>";
            }

            return str;
        }
    }
}