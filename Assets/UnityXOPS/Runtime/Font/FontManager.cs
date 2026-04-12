using JJLUtility;
using UnityEngine;
using TMPro;
using System.Linq;
using JJLUtility.IO;
using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer;

namespace UnityXOPS
{
    /// <summary>
    /// OS 폰트와 스프라이트 폰트 텍스처를 로드하고 XOPSSpriteText 오브젝트 생성을 담당하는 싱글톤 매니저.
    /// </summary>
    public class FontManager : SingletonBehavior<FontManager>
    {
        private const string k_koreanOSFontPath = "WINDOWS/Fonts/malgun.ttf";
        private const string k_japaneseOSFontPath = "WINDOWS/Fonts/YuGothR.ttc";
        private const string k_englishOSFontPath = "WINDOWS/Fonts/segoeui.ttf";

        private string m_koreanOSFontPath;
        private string m_japaneseOSFontPath;
        private string m_englishOSFontPath;

        [SerializeField]
        private TMP_FontAsset osFont;

        [SerializeField]
        private Texture2D SpriteFontTexture;

        /// <summary>
        /// 스프라이트 폰트 텍스처와 시스템 언어에 맞는 OS TMP 폰트 에셋을 초기화한다.
        /// </summary>
        private void Start()
        {
            var spriteFontTexturePath = SafePath.Combine(Application.streamingAssetsPath, "data/char.dds");
            SpriteFontTexture = ImageLoader.LoadTexture(spriteFontTexturePath);

            string[] fontPathList = Font.GetPathsToOSFonts();
            m_koreanOSFontPath   = fontPathList.FirstOrDefault(path => path.EndsWith(k_koreanOSFontPath));
            m_japaneseOSFontPath = fontPathList.FirstOrDefault(path => path.EndsWith(k_japaneseOSFontPath));
            m_englishOSFontPath  = fontPathList.FirstOrDefault(path => path.EndsWith(k_englishOSFontPath));

            string selectedPath = Application.systemLanguage switch
            {
                SystemLanguage.Korean   => m_koreanOSFontPath,
                SystemLanguage.Japanese => m_japaneseOSFontPath,
                _                       => m_englishOSFontPath,
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
        
        /// <summary>
        /// 지정된 파라미터로 XOPSSpriteText 파생 컴포넌트를 생성하고 RectTransform을 설정한 뒤 반환한다.
        /// </summary>
        /// <typeparam name="T">생성할 XOPSSpriteText 파생 타입.</typeparam>
        public static T CreateSpriteText<T>(Transform root, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 fontSize, Color32 color, TextAnchor alignment, float spacing) where T : XOPSSpriteText
        {
            var obj = new GameObject();
            obj.transform.SetParent(root, false);
            var spriteText = obj.AddComponent<T>();

            var rectTransform = obj.GetComponent<RectTransform>();
            rectTransform.pivot = alignment switch
            {
                TextAnchor.LowerLeft   => new Vector2(0f,   0f),
                TextAnchor.LowerCenter => new Vector2(0.5f, 0f),
                TextAnchor.LowerRight  => new Vector2(1f,   0f),
                TextAnchor.MiddleLeft  => new Vector2(0f,   0.5f),
                TextAnchor.MiddleCenter=> new Vector2(0.5f, 0.5f),
                TextAnchor.MiddleRight => new Vector2(1f,   0.5f),
                TextAnchor.UpperLeft   => new Vector2(0f,   1f),
                TextAnchor.UpperCenter => new Vector2(0.5f, 1f),
                TextAnchor.UpperRight  => new Vector2(1f,   1f),
                _                      => new Vector2(0f,   0f),
            };
            rectTransform.anchoredPosition = position;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.sizeDelta = size;
            spriteText.CharTexture = Instance.SpriteFontTexture;
            spriteText.Text = text;
            spriteText.CharWidth = fontSize.x;
            spriteText.CharHeight = fontSize.y;
            spriteText.FontColor = color;
            spriteText.Alignment = alignment;
            spriteText.Spacing = spacing;

            return spriteText;
        }
    }
}