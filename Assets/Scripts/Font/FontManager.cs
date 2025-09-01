using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Text;
using Palmmedia.ReportGenerator.Core.Reporting.Builders;

namespace UnityXOPS
{
    /// <summary>
    /// UnityXOPS에서 OS에서 기본 사용하는 폰트와, 게임의 char.dds를 불러오는 클래스입니다.
    /// </summary>
    /// <remarks>
    /// <see cref="Singleton{T}">Singleton</see> 클래스입니다.
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
            //윈도우에 있는 모든 OS파일을 불러옵니다.
            //기본 폰트는 MS의 기본 설정을 따릅니다.
            //ex) 한국어는 맑은 고딕, 일본어는 유 고딕입니다.
            var osFontPaths = Font.GetPathsToOSFonts();
            
            //언어 설정에 따라 OS 폰트를 찾아 지정합니다.
            _osFont = ProfileManager.Instance.GetProfileValue("General", "ANSIEncoding", "kr") switch
            {
                "jp" => new Font(osFontPaths.FirstOrDefault(p => p.Contains("YuGothR.ttc"))),
                "kr" => new Font(osFontPaths.FirstOrDefault(p => p.Contains("malgun.ttf"))),
                _ => new Font(osFontPaths.FirstOrDefault(p => p.Contains("segoeui.ttf")))
            };

            //OS 폰트를 TMP 폰트화합니다.
            //런타임에 생성한 TMP Asset은 Dynamic이기 때문에
            //자동으로 없는 문자는 동적으로 Atlas에 포함합니다.
            //큰 성능 저하는 적어도 윈도우 플랫폼에선 없습니다.
            osFontAsset = TMP_FontAsset.CreateFontAsset(_osFont);
            
            var gameFontPath = Path.Combine(Application.streamingAssetsPath, "data", "char.dds");
            var gameFontTexture = ImageManager.Instance.LoadImage(gameFontPath);

            //char.dds를 SpriteAsset으로 불러옵니다.
            //char.dds는 32x32픽셀의 글자 문자들의 집합 이미지이기 때문에
            //Sprite로 불러오는 것이 더 적합합니다.
            //TMP로 불러올 경우 오버헤드와 덮어씌울 때의 불편함이 매우 큽니다.
            if (gameFontTexture)
            {
                var runtimeSpriteAsset = Instantiate(gameFontSpriteAsset);
                runtimeSpriteAsset.material = Instantiate(runtimeSpriteAsset.material);
                runtimeSpriteAsset.material.mainTexture = gameFontTexture;
                runtimeSpriteAsset.spriteSheet = gameFontTexture;
                gameFontSpriteAsset = runtimeSpriteAsset;
            } 
        }
        
    }
}