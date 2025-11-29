using UnityEngine;
using TMPro;
using System.IO;
using System.Linq;

namespace UnityXOPS
{
    public static class FontLoader
    {
        private const string EnglishFontPath = "WINDOWS/Fonts/segoeui.ttf";
        private const string KoreanFontPath = "WINDOWS/Fonts/malgun.ttf";
        private const string JapaneseFontPath = "WINDOWS/Fonts/YuGothR.ttc";
        private const string ChineseSimplifiedFontPath = "WINDOWS/Fonts/msyh.ttc";
        private const string ChineseTraditionalFontPath = "WINDOWS/Fonts/msjh.ttc";
        
        public static TMP_FontAsset OSFont;
        public static TMP_SpriteAsset GameSprite;
        
        public static void Initialize()
        {
            var lang = ProfileLoader.GetProfileValue("Common", "Language", "en");
            switch (lang)
            {
                case "kr":
                    OSFont = LoadOSFont(KoreanFontPath);
                    break;
                case "jp":
                    OSFont = LoadOSFont(JapaneseFontPath);
                    break;
                case "cn_s":
                    OSFont = LoadOSFont(ChineseSimplifiedFontPath);
                    break;
                case "cn_t":
                    OSFont = LoadOSFont(ChineseTraditionalFontPath);
                    break;
                default: //case "en":
                    OSFont = LoadOSFont(EnglishFontPath);
                    break;
            }
            
            TMP_SpriteAsset temp = Resources.Load<TMP_SpriteAsset>("Graphic/char_sprite_tmp");
            
            var useInternalCharDDS = ProfileLoader.GetProfileValue("Stream", "UseInternalCharDDS", "true");
            Texture2D spriteImage;

            if (useInternalCharDDS == "true")
            {
                spriteImage = Resources.Load<Texture2D>("Graphic/char");
            }
            else
            {
                var path = Path.Combine(Application.streamingAssetsPath, "data/char.dds");
                spriteImage = ImageLoader.LoadImage(path);
            }

            GameSprite = Object.Instantiate(temp);
            GameSprite.material = Object.Instantiate(GameSprite.material);
            GameSprite.spriteSheet = spriteImage;
            GameSprite.material.mainTexture = spriteImage;
            GameSprite.name = "GameFont";
        }
        
        private static TMP_FontAsset LoadOSFont(string path)
        {
            var fontPaths = Font.GetPathsToOSFonts();
            
            var fontPath = fontPaths.FirstOrDefault(f => f.Contains(path));
            
            var font = new Font(fontPath);
            
            var fontAsset = TMP_FontAsset.CreateFontAsset(font);
            fontAsset.name = font.name;

            return fontAsset;
        }
    }
}
