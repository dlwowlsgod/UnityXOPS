using JJLUtility;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;
using System.Collections.Generic;
using System.IO;
using JJLUtility.IO;
using UnityXOPS.Modding;

namespace UnityXOPS
{
    /// <summary>
    /// OS 폰트와 스프라이트 폰트 텍스처를 로드하고 XOPSSpriteText 오브젝트 생성을 담당하는 싱글톤 매니저.
    /// OS 폰트는 font.lua가 정의한 폰트를 우선 쓰고, 부족한 글자는 언어별 기본 폰트가 대체 체인으로 메운다.
    /// </summary>
    public class FontManager : SingletonBehavior<FontManager>
    {
        private const string k_fontScriptPath = "unitydata/ui/font.lua";
        private const string k_defaultFontStyle = "Regular";

        private const int k_samplingPointSize = 90;
        private const int k_atlasPadding = 9;
        private const int k_atlasSize = 1024;

        // config General.language 값. 0이면 OS 언어를 따라간다.
        private const int k_languageAuto = 0;
        private const int k_languageKorean = 1;
        private const int k_languageJapanese = 2;
        private const int k_languageEnglish = 3;

        // 언어별 기본 폰트 계열 이름. 원본 XOPS가 쓰던 Dotum(한국어)/MS Gothic(일본어)을 먼저 잡고,
        // 그 글꼴이 없는 환경(한중일 보조 글꼴 미설치 Windows)을 위해 현행 시스템 글꼴을 뒤에 둔다.
        private static readonly string[] k_koreanFontFamilies = { "Dotum", "MS Gothic", "Malgun Gothic", "Yu Gothic", "Segoe UI" };
        private static readonly string[] k_japaneseFontFamilies = { "MS Gothic", "Dotum", "Yu Gothic", "Malgun Gothic", "Segoe UI" };
        private static readonly string[] k_englishFontFamilies = { "Segoe UI", "MS Gothic", "Dotum", "Yu Gothic", "Malgun Gothic" };

        private readonly List<TMP_FontAsset> m_defaultFonts = new List<TMP_FontAsset>();

        private TMP_FontAsset m_userOSFont;
        private readonly List<TMP_FontAsset> m_userFallbacks = new List<TMP_FontAsset>();
        private readonly List<TMP_FontAsset> m_createdFonts = new List<TMP_FontAsset>();

        private TMP_FontAsset osFont;

        // 스프라이트 시트 페이지. 코드포인트 상위(코드포인트/256)를 키로 삼는 희소 저장이다.
        private readonly Dictionary<int, Texture2D> m_spritePages = new Dictionary<int, Texture2D>();

        private void Start()
        {
            CreateDefaultOSFonts();
            LuaManager.Instance.LoadSandboxedFile(k_fontScriptPath, "font");
            BuildOSFontChain();
        }

        public static TMP_FontAsset OSFont => Instance.osFont;

        /// <summary>
        /// 등록된 스프라이트 시트 페이지를 가져온다.
        /// </summary>
        /// <param name="page">페이지 번호(코드포인트/256)</param>
        /// <returns>그 페이지의 텍스처. 등록돼 있지 않으면 null.</returns>
        public static Texture2D GetSpritePage(int page)
        {
            // 애플리케이션 종료 중에는 Instance가 null을 돌려준다.
            FontManager instance = Instance;
            if (instance == null)
            {
                return null;
            }

            return instance.m_spritePages.TryGetValue(page, out Texture2D texture) ? texture : null;
        }

        /// <summary>
        /// 스프라이트 시트 페이지를 등록한다. 같은 번호를 다시 등록하면 교체된다.
        /// 시트는 16열x16행이라 페이지 하나가 칸 256개를 담는다.
        /// 이미 만들어진 텍스트는 등록을 되짚지 않으므로 font.lua 초기화 시점에만 호출해야 한다.
        /// </summary>
        /// <param name="page">페이지 번호(코드포인트/256). 0이면 ASCII 범위.</param>
        /// <param name="relativePath">StreamingAssets 기준 상대 경로</param>
        internal void SetSpritePage(int page, string relativePath)
        {
            if (page < 0)
            {
                Debugger.LogWarning($"[Font] 페이지 번호는 0 이상이어야 합니다: {page}");
                return;
            }

            string fullPath = SafePath.Combine(Application.streamingAssetsPath, relativePath);
            if (!File.Exists(fullPath))
            {
                Debugger.LogWarning($"[Font] 스프라이트 시트를 찾을 수 없습니다: {fullPath}");
                return;
            }

            Texture2D texture = ImageLoader.LoadTexture(fullPath);
            if (texture == null)
            {
                Debugger.LogWarning($"[Font] 스프라이트 시트를 읽을 수 없습니다: {fullPath}");
                return;
            }

            m_spritePages[page] = texture;
        }

        /// <summary>
        /// 설정 언어에 맞는 기본 폰트 목록을 계열 이름으로 찾아 만들어 둔다. 대체 체인의 마지막 보루로 쓰인다.
        /// 설치돼 있지 않은 계열은 건너뛰므로, 목록 전체가 없는 극단적 환경에서만 비어 있게 된다.
        /// </summary>
        private void CreateDefaultOSFonts()
        {
            m_defaultFonts.Clear();
            foreach (string family in LanguageFontFamilies())
            {
                TMP_FontAsset font = CreateSystemFontAsset(family, k_defaultFontStyle);
                if (font != null)
                {
                    m_defaultFonts.Add(font);
                }
            }
        }

        /// <summary>
        /// 설정된 언어에 대응하는 기본 폰트 계열 이름 목록을 반환한다.
        /// config의 language가 0(자동, 기본)이면 OS 언어를 따르고, 그 외에는 지정한 언어를 쓴다.
        /// </summary>
        /// <returns>우선순위 순으로 늘어놓은 폰트 계열 이름 배열.</returns>
        private static string[] LanguageFontFamilies()
        {
            int language = ConfigManager.Instance.GetInt(ConfigManager.SectionGeneral, ConfigManager.KeyLanguage, k_languageAuto);

            if (language == k_languageAuto)
            {
                language = Application.systemLanguage switch
                {
                    SystemLanguage.Korean => k_languageKorean,
                    SystemLanguage.Japanese => k_languageJapanese,
                    _ => k_languageEnglish,
                };
            }

            return language switch
            {
                k_languageKorean => k_koreanFontFamilies,
                k_languageJapanese => k_japaneseFontFamilies,
                _ => k_englishFontFamilies,
            };
        }

        /// <summary>
        /// 우선순위대로 폰트를 늘어놓아 대표 OS 폰트와 그 대체 체인을 구성한다.
        /// 순서는 유저 지정 폰트 → 그 폰트에 붙은 대체 폰트 → 유저가 추가한 대체 폰트 → 언어별 기본 폰트 목록이다.
        /// 유저가 대표 폰트를 지정하지 않았으면 대체 폰트가 승격되지 않도록 언어별 기본 폰트를 대표로 삼는다.
        /// </summary>
        private void BuildOSFontChain()
        {
            var ordered = new List<TMP_FontAsset>();
            TMP_FontAsset head = m_userOSFont != null ? m_userOSFont : LanguageDefaultFont();

            AddUnique(ordered, head);
            if (head != null && head.fallbackFontAssetTable != null)
            {
                foreach (TMP_FontAsset font in head.fallbackFontAssetTable)
                {
                    AddUnique(ordered, font);
                }
            }

            foreach (TMP_FontAsset font in m_userFallbacks)
            {
                AddUnique(ordered, font);
            }

            foreach (TMP_FontAsset font in m_defaultFonts)
            {
                AddUnique(ordered, font);
            }

            if (ordered.Count == 0)
            {
                Debugger.LogWarning("[Font] OS 폰트를 하나도 찾지 못했습니다. TMP 기본 폰트로 대체합니다.");
                osFont = TMP_Settings.defaultFontAsset;
                return;
            }

            osFont = ordered[0];
            osFont.fallbackFontAssetTable = ordered.GetRange(1, ordered.Count - 1);
        }

        /// <summary>
        /// 유저가 대표 폰트를 지정하지 않았을 때 쓸 기본 폰트를 반환한다.
        /// </summary>
        /// <returns>설정 언어의 첫 번째 기본 폰트. 하나도 못 찾았으면 null.</returns>
        private TMP_FontAsset LanguageDefaultFont()
        {
            return m_defaultFonts.Count > 0 ? m_defaultFonts[0] : null;
        }

        /// <summary>
        /// 폰트를 목록 끝에 추가한다. null이거나 이미 들어 있으면 무시한다.
        /// </summary>
        /// <param name="list">대상 목록</param>
        /// <param name="font">추가할 폰트</param>
        private static void AddUnique(List<TMP_FontAsset> list, TMP_FontAsset font)
        {
            if (font != null && !list.Contains(font))
            {
                list.Add(font);
            }
        }

        /// <summary>
        /// 폰트 파일 경로로 동적 아틀라스 TMP 폰트 에셋을 만들고 매니저가 참조를 보유한다.
        /// 어디에도 연결되지 않은 폰트가 씬 전환 시 언로드되지 않게 하려면 이 경로로만 생성해야 한다.
        /// </summary>
        /// <param name="path">폰트 파일 절대 경로</param>
        /// <param name="faceIndex">파일 안의 서체 번호(.ttc 대응)</param>
        /// <returns>생성된 폰트 에셋. 경로가 비었거나 읽기에 실패하면 null.</returns>
        private TMP_FontAsset CreateDynamicFontAsset(string path, int faceIndex)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(path, faceIndex, k_samplingPointSize, k_atlasPadding, GlyphRenderMode.SDFAA, k_atlasSize, k_atlasSize);
            if (font == null)
            {
                return null;
            }

            font.name = Path.GetFileNameWithoutExtension(path);
            ApplyDynamicAtlasPolicy(font);
            m_createdFonts.Add(font);
            return font;
        }

        /// <summary>
        /// 동적 아틀라스 정책을 강제한다. 아틀라스가 가득 차면 텍스처를 늘리고, 글자 붙임/커닝 정보를 함께 읽는다.
        /// </summary>
        /// <param name="font">정책을 적용할 폰트 에셋</param>
        private static void ApplyDynamicAtlasPolicy(TMP_FontAsset font)
        {
            font.isMultiAtlasTexturesEnabled = true;
            font.getFontFeatures = true;
        }

        /// <summary>
        /// StreamingAssets 안의 폰트 파일로 모드용 TMP 폰트를 만든다.
        /// </summary>
        /// <param name="relativePath">StreamingAssets 기준 상대 경로</param>
        /// <param name="faceIndex">파일 안의 서체 번호(.ttc 대응)</param>
        /// <returns>폰트 핸들. 실패해도 핸들은 반환되며 이때 IsValid()가 false다.</returns>
        internal TMPFontHandle CreateUserFontFromFile(string relativePath, int faceIndex)
        {
            string fullPath = SafePath.Combine(Application.streamingAssetsPath, relativePath);
            if (!File.Exists(fullPath))
            {
                Debugger.LogWarning($"[Font] 폰트 파일을 찾을 수 없습니다: {fullPath}");
                return new TMPFontHandle(null);
            }

            TMP_FontAsset font = CreateDynamicFontAsset(fullPath, faceIndex);
            if (font == null)
            {
                Debugger.LogWarning($"[Font] 폰트를 읽을 수 없습니다: {fullPath}");
            }

            return new TMPFontHandle(font);
        }

        /// <summary>
        /// 시스템에 설치된 폰트를 이름으로 찾아 모드용 TMP 폰트를 만든다.
        /// TMP가 내부에서 잡는 아틀라스 크기·패딩·렌더 모드가 k_atlasSize/k_atlasPadding/SDFAA와 같은 값이라
        /// CreateUserFontFromFile로 만든 폰트와 결과가 일치한다. 아틀라스 모드만 DynamicOS로 잡힌다.
        /// </summary>
        /// <param name="familyName">폰트 계열 이름</param>
        /// <param name="styleName">스타일 이름</param>
        /// <returns>폰트 핸들. 실패해도 핸들은 반환되며 이때 IsValid()가 false다.</returns>
        internal TMPFontHandle CreateUserFontFromOS(string familyName, string styleName)
        {
            TMP_FontAsset font = CreateSystemFontAsset(familyName, styleName);
            if (font == null)
            {
                Debugger.LogWarning($"[Font] 설치된 폰트를 찾을 수 없습니다: {familyName} {styleName}");
            }

            return new TMPFontHandle(font);
        }

        /// <summary>
        /// 시스템에 설치된 폰트를 계열 이름과 스타일 이름으로 찾아 폰트 에셋을 만들고 매니저가 참조를 보유한다.
        /// TMP가 OS 폰트 시스템에 직접 질의하므로 .ttc처럼 서체가 여러 개인 파일에서도 스타일에 맞는 서체가 선택된다.
        /// </summary>
        /// <param name="familyName">폰트 계열 이름</param>
        /// <param name="styleName">스타일 이름</param>
        /// <returns>생성된 폰트 에셋. 해당 폰트가 설치돼 있지 않으면 null.</returns>
        private TMP_FontAsset CreateSystemFontAsset(string familyName, string styleName)
        {
            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(familyName, styleName, k_samplingPointSize);
            if (font == null)
            {
                return null;
            }

            font.name = $"{familyName} {styleName}".Trim();
            ApplyDynamicAtlasPolicy(font);
            m_createdFonts.Add(font);
            return font;
        }

        /// <summary>
        /// 게임 전체가 쓸 기본 OS 폰트를 지정한다.
        /// </summary>
        /// <param name="font">기본으로 쓸 폰트 핸들. null이면 무시한다.</param>
        internal void SetUserOSFont(TMPFontHandle font)
        {
            if (font != null && font.Asset != null)
            {
                m_userOSFont = font.Asset;
            }
        }

        /// <summary>
        /// 기본 OS 폰트 뒤에 붙일 대체 폰트를 추가한다. 추가한 순서가 곧 탐색 순서다.
        /// </summary>
        /// <param name="font">대체 폰트 핸들. null이면 무시한다.</param>
        internal void AddUserFallback(TMPFontHandle font)
        {
            if (font != null && font.Asset != null && !m_userFallbacks.Contains(font.Asset))
            {
                m_userFallbacks.Add(font.Asset);
            }
        }

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