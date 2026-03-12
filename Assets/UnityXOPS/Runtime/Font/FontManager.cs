using JJLUtility;
using UnityEngine;
using TMPro;
using System.Linq;
using JJLUtility.IO;

namespace UnityXOPS
{
    public class FontManager : SingletonBehavior<FontManager>
    {
        private const string KoreanOSFontPath = "WINDOWS/Fonts/malgun.ttf";
        private const string JapaneseOSFontPath = "WINDOWS/Fonts/YuGothR.ttc";
        private const string EnglishOSFontPath = "WINDOWS/Fonts/segoeui.ttf";

        private string m_koreanOSFontPath;
        private string m_japaneseOSFontPath;
        private string m_englishOSFontPath;

        [SerializeField]
        private TMP_FontAsset OSFont;

        [SerializeField]
        private Texture2D SpriteFontTexture;

        [SerializeField]
        private GameObject OSFontTMPPrefab;
        [SerializeField]
        private GameObject XOPSFontPrefab;

        private void Start()
        {
            var spriteFontTexturePath = SafePath.Combine(Application.streamingAssetsPath, "data/char.dds");
            SpriteFontTexture = ImageLoader.LoadTexture(spriteFontTexturePath);

            string[] fontPathList = Font.GetPathsToOSFonts();
            m_koreanOSFontPath   = fontPathList.FirstOrDefault(path => path.EndsWith(KoreanOSFontPath));
            m_japaneseOSFontPath = fontPathList.FirstOrDefault(path => path.EndsWith(JapaneseOSFontPath));
            m_englishOSFontPath  = fontPathList.FirstOrDefault(path => path.EndsWith(EnglishOSFontPath));

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
            OSFont = TMP_FontAsset.CreateFontAsset(font);
            OSFont.name = font.name;
        }
    }
}