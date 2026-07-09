using JJLUtility;
using UnityEngine;
using TMPro;
using System.Linq;
using JJLUtility.IO;

namespace UnityXOPS
{
    /// <summary>
    /// OS 폰트와 스프라이트 폰트 텍스처를 로드하고 XOPSSpriteText 오브젝트 생성을 담당하는 싱글톤 매니저.
    /// </summary>
    public class FontManager : SingletonBehavior<FontManager>
    {
        private const string k_koreanOSFontPath = "malgun.ttf";
        private const string k_japaneseOSFontPath = "YuGothR.ttc";
        private const string k_englishOSFontPath = "segoeui.ttf";

        private string m_koreanOSFontPath;
        private string m_japaneseOSFontPath;
        private string m_englishOSFontPath;

        [SerializeField]
        private TMP_FontAsset osFont;

        [SerializeField]
        private Texture2D spriteFontTexture;

        private void Start()
        {
            var spriteFontTexturePath = SafePath.Combine(Application.streamingAssetsPath, "data/char.dds");
            spriteFontTexture = ImageLoader.LoadTexture(spriteFontTexturePath);

            string[] fontPathList = Font.GetPathsToOSFonts();
            m_koreanOSFontPath = fontPathList.FirstOrDefault(path => path.EndsWith(k_koreanOSFontPath));
            m_japaneseOSFontPath = fontPathList.FirstOrDefault(path => path.EndsWith(k_japaneseOSFontPath));
            m_englishOSFontPath = fontPathList.FirstOrDefault(path => path.EndsWith(k_englishOSFontPath));

            string selectedPath = Application.systemLanguage switch
            {
                SystemLanguage.Korean => m_koreanOSFontPath,
                SystemLanguage.Japanese => m_japaneseOSFontPath,
                _ => m_englishOSFontPath,
            };

            if (string.IsNullOrEmpty(selectedPath))
            {
                Debugger.LogWarning("Failed to find OS Font.", Instance, nameof(FontManager));
                return;
            }

            Font font = new Font(selectedPath);
            font.name = System.IO.Path.GetFileNameWithoutExtension(selectedPath);
            osFont = TMP_FontAsset.CreateFontAsset(font);
            osFont.name = font.name;
        }

        public static TMP_FontAsset OSFont => Instance.osFont;
        public static Texture2D SpriteFont => Instance.spriteFontTexture;

        /// <summary>
        /// 지정된 파라미터로 XOPSSpriteText 파생 컴포넌트를 생성하고 RectTransform을 설정한 뒤 반환한다.
        /// </summary>
        /// <typeparam name="T">생성할 XOPSSpriteText 파생 타입.</typeparam>
        /// <returns>생성되어 RectTransform과 텍스트 속성이 설정된 XOPSSpriteText 파생 컴포넌트.</returns>
        public static T CreateSpriteText<T>(Transform root, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 fontSize, Color32 color, TextAnchor alignment, float spacing) where T : XOPSSpriteText
        {
            var obj = new GameObject();
            obj.transform.SetParent(root, false);
            var spriteText = obj.AddComponent<T>();

            var rectTransform = obj.GetComponent<RectTransform>();
            rectTransform.pivot = AlignmentToPivot(alignment);
            rectTransform.anchoredPosition = position;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.sizeDelta = size;
            spriteText.CharTexture = SpriteFont;
            spriteText.Text = text;
            spriteText.CharWidth = fontSize.x;
            spriteText.CharHeight = fontSize.y;
            spriteText.FontColor = color;
            spriteText.Alignment = alignment;
            spriteText.Spacing = spacing;

            return spriteText;
        }

        /// <summary>
        /// 지정 파라미터로 OS 폰트(TMP) 텍스트 UI를 생성하고 RectTransform을 설정한 뒤 반환한다.
        /// 스프라이트 폰트(XOPSSpriteText) 대신 가독성 좋은 OS 폰트를 쓸 때 사용한다.
        /// rect는 0 크기로 두고 TMP alignment가 기준점에서의 정렬 방향을 결정한다(XOPSSpriteText와 동일 감각).
        /// </summary>
        /// <param name="root">부모 Transform.</param>
        /// <param name="text">표시할 문자열.</param>
        /// <param name="anchorMin">앵커 최소.</param>
        /// <param name="anchorMax">앵커 최대.</param>
        /// <param name="position">anchoredPosition.</param>
        /// <param name="size">rect sizeDelta(보통 0 — 히트 영역이 필요하면 호출 측이 키운다).</param>
        /// <param name="fontSize">글자 크기(pt).</param>
        /// <param name="color">글자 색.</param>
        /// <param name="alignment">글자 정렬 기준점.</param>
        /// <returns>설정이 끝난 TextMeshProUGUI.</returns>
        public static TextMeshProUGUI CreateOSFont(Transform root, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, float fontSize, Color32 color, TextAnchor alignment)
        {
            var obj = new GameObject();
            obj.transform.SetParent(root, false);
            var tmp = obj.AddComponent<TextMeshProUGUI>();

            var rectTransform = tmp.rectTransform;
            rectTransform.pivot = AlignmentToPivot(alignment);
            rectTransform.anchoredPosition = position;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.sizeDelta = size;

            tmp.font = OSFont;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = ToTMPAlignment(alignment);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;

            return tmp;
        }

        // TextAnchor(9지점) → RectTransform pivot.
        private static Vector2 AlignmentToPivot(TextAnchor alignment)
        {
            return alignment switch
            {
                TextAnchor.LowerLeft => new Vector2(0f, 0f),
                TextAnchor.LowerCenter => new Vector2(0.5f, 0f),
                TextAnchor.LowerRight => new Vector2(1f, 0f),
                TextAnchor.MiddleLeft => new Vector2(0f, 0.5f),
                TextAnchor.MiddleCenter => new Vector2(0.5f, 0.5f),
                TextAnchor.MiddleRight => new Vector2(1f, 0.5f),
                TextAnchor.UpperLeft => new Vector2(0f, 1f),
                TextAnchor.UpperCenter => new Vector2(0.5f, 1f),
                TextAnchor.UpperRight => new Vector2(1f, 1f),
                _ => new Vector2(0f, 0f),
            };
        }

        // TextAnchor(9지점) → TMP TextAlignmentOptions.
        private static TextAlignmentOptions ToTMPAlignment(TextAnchor alignment)
        {
            return alignment switch
            {
                TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
                TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
                TextAnchor.MiddleRight => TextAlignmentOptions.Right,
                TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                _ => TextAlignmentOptions.TopLeft,
            };
        }
    }
}