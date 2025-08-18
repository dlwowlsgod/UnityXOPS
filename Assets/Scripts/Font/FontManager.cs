using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Text;

namespace UnityXOPS
{
    /// <summary>
    /// Provides management and utility functions for handling font-related operations in the application.
    /// </summary>
    /// <remarks>
    /// Inherits from the Singleton class and ensures a single instance of the FontManager exists during runtime.
    /// </remarks>
    public class FontManager : Singleton<FontManager>
    {
        private Font _osFont;

        [SerializeField] 
        private TMP_FontAsset osFontAsset;
        
        [SerializeField]
        private TMP_SpriteAsset gameFontSpriteAsset;
        
        public TMP_FontAsset OSFont => osFontAsset;
        public TMP_SpriteAsset GameFont => gameFontSpriteAsset;

        private void Start()
        {
            var osFontPaths = Font.GetPathsToOSFonts();

            /*
            OS폰트를 불러오는 작업입니다.
            원본 엑옵의 경우 당연히 시스템 폰트를 씁니다.
            유니티의 경우 TMP라고 하는 강력한 폰트 시스템을 사용하는데,
            유니티에서 버려진 Font 시스템에 OS폰트 위치를 가져오는 함수가 있습니다.
            이를 기반으로 마이크로소프트의 "국가별 글꼴" 을 통해서 언어마다 정해진 글꼴을 불러옵니다.
            그리고 TMP를 그 글꼴로 생성합니다.
            TMP는 Dynamic 모드로 자동 설정되기에, 입력된 글자에 따라서 아틀라스를 게임 엔진이 자동으로 업뎃합니다.
            문제는 다른 OS에서의 상황인데... 그건 나중에 처리하죠
            */
            _osFont = PrivateProfileReader.ImportLanguage switch
            {
                "jp" => new Font(osFontPaths.FirstOrDefault(p => p.Contains("YuGothR.ttc"))),
                "kr" => new Font(osFontPaths.FirstOrDefault(p => p.Contains("malgun.ttf"))),
                _ => new Font(osFontPaths.FirstOrDefault(p => p.Contains("segoeui.ttf")))
            };

            osFontAsset = TMP_FontAsset.CreateFontAsset(_osFont);
            
            /*
            게임폰트를 불러오는 작업입니다.
            원본 엑옵의 경우 이 폰트를 char.dds에 저장하죠?
            이걸 TMP에 이식하려면 offset, padding, 넓이 등등 100개가 넘는 문자에
            그 값을 전부 정하고 확인해야 합니다. 깔끔하게 처리를 실패할 확률도 매우 높고 상당히 귀찮죠
            SpriteAsset에 이걸 때려박으면 유니티 엔진은 이 글꼴을 "이모티콘" 이라고 취급합니다.
            하지만 TMP와 별 다를게 없이 사이즈, 패딩 등등을 알아서 처리해주죠.
            제가 생각해봐도 상당히 좋은 꼼수입니다. LLM은 절대 생각 못하는 그런 꼼수
            문제는 이걸 폰트로 쓰려면 상당히 긴 Rich Text를 이용해야 하는데
            ABCD를 입력하려면 <sprite index=(A Hex값) color=#ffffff> 이런 식으로요...
            이것도 상당히 귀찮지만 이건 코드로 처리하면 실패할 일이 없어집니다.
            어쨌든 이건 이런 기능이다 라는 설명
            */
            var gameFontPath = Path.Combine(Application.streamingAssetsPath, "data", "char.dds");
            var gameFontTexture = ImageReader.LoadTexture(gameFontPath);

            if (gameFontTexture)
            {
                var runtimeSpriteAsset = Instantiate(gameFontSpriteAsset);
                runtimeSpriteAsset.material = Instantiate(runtimeSpriteAsset.material);
                runtimeSpriteAsset.material.mainTexture = gameFontTexture;
                runtimeSpriteAsset.spriteSheet = gameFontTexture;
                gameFontSpriteAsset = runtimeSpriteAsset;
            } 
        }

        /// <summary>
        /// Creates a text object with the specified properties and adds it as a child to the specified parent transform.
        /// </summary>
        /// <param name="pivot">The pivot point of the RectTransform.</param>
        /// <param name="anchorMin">The minimum anchor point of the RectTransform.</param>
        /// <param name="anchorMax">The maximum anchor point of the RectTransform.</param>
        /// <param name="anchoredPosition">The anchored position of the RectTransform.</param>
        /// <param name="sizeDelta">The size delta of the RectTransform.</param>
        /// <param name="alignment">The text alignment options for the TextMeshProUGUI component.</param>
        /// <param name="text">The text content to be displayed.</param>
        /// <param name="color">The color of the text.</param>
        /// <param name="gameText">A boolean indicating whether the text is game-related or not.</param>
        /// <param name="parent">The parent transform under which the text object will be added.</param>
        /// <returns>Returns the created GameObject representing the text.</returns>
        public GameObject CreateText(Vector2 pivot, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 sizeDelta, TextAlignmentOptions alignment,
            string text, string colorHex, bool gameText, Transform parent)
        {
            var scale = Screen.height / 540f;
            
            var obj = new GameObject("tmpObject");
            obj.transform.SetParent(parent);

            var rt = obj.AddComponent<RectTransform>();
            rt.pivot = pivot;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
            
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.font = OSFont;
            tmp.spriteAsset = GameFont;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = rt.sizeDelta.y * 0.748f * scale;
            tmp.fontSizeMax = rt.sizeDelta.y * 0.752f * scale;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.alignment = alignment;
            if (gameText)
            {
                tmp.text = NormalTextToGameText(text, colorHex);
            }
            else
            {
                tmp.text = text;
            }
            
            return obj;
        }
        
        private static string NormalTextToGameText(string text, string colorHex)
        {
            var sb = new StringBuilder();
            foreach (var c in text)
            {
                switch (c)
                {
                    case >= ' ' and <= '~':
                        break;
                    case '\n' or '\r':
                        sb.Append("<br>");
                        continue;
                }

                var textIndex = (int)c;
                var toRichText = $"<sprite index={textIndex} color=#{colorHex}>";
                sb.Append(toRichText);
            }
            
            return sb.ToString();
        }
    }
}