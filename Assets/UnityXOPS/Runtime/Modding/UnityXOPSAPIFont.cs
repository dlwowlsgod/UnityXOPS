using XLua;

namespace UnityXOPS.Modding
{
    public partial class UnityXOPSAPI
    {
        private FontAPI m_font;
        public FontAPI Font => m_font ??= new FontAPI();
    }

    /// <summary>
    /// 모드에 OS 폰트(TMP) 정의를 제공하는 API 그룹. Lua에서는 XOPS.Font 로 접근한다.
    /// font.lua에서 게임 시작 시 1회만 호출되며, 만들어진 폰트는 아틀라스가 항상 동적으로 고정된다.
    /// </summary>
    [LuaCallCSharp]
    public class FontAPI
    {
        /// <summary>
        /// StreamingAssets 안의 폰트 파일로 TMP 폰트를 만든다. 모드에 동봉한 폰트를 쓸 때 사용한다.
        /// </summary>
        /// <param name="relativePath">StreamingAssets 기준 상대 경로(예: "addon/myfont.ttf")</param>
        /// <param name="faceIndex">파일 안의 서체 번호. 보통 0이며, 여러 서체가 든 .ttc에서만 1 이상을 쓴다.</param>
        /// <returns>폰트 핸들. 파일이 없거나 읽을 수 없어도 핸들은 반환되며 이때 IsValid()가 false다.</returns>
        public TMPFontHandle CreateFromFile(string relativePath, int faceIndex)
        {
            return FontManager.Instance.CreateUserFontFromFile(relativePath, faceIndex);
        }

        /// <summary>
        /// 시스템에 설치된 폰트를 이름으로 찾아 TMP 폰트를 만든다.
        /// </summary>
        /// <param name="familyName">폰트 계열 이름(예: "Malgun Gothic", "Segoe UI")</param>
        /// <param name="styleName">스타일 이름(예: "Regular", "Bold", "Italic")</param>
        /// <returns>폰트 핸들. 해당 폰트가 설치돼 있지 않아도 핸들은 반환되며 이때 IsValid()가 false다.</returns>
        public TMPFontHandle CreateFromOS(string familyName, string styleName)
        {
            return FontManager.Instance.CreateUserFontFromOS(familyName, styleName);
        }

        /// <summary>
        /// 게임 전체가 쓸 기본 OS 폰트를 지정한다. 지정하지 않으면 OS 언어에 맞는 기본 폰트가 쓰인다.
        /// </summary>
        /// <param name="font">기본으로 쓸 폰트 핸들</param>
        public void SetOSFont(TMPFontHandle font)
        {
            FontManager.Instance.SetUserOSFont(font);
        }

        /// <summary>
        /// 스프라이트 시트 폰트의 페이지를 등록한다. 등록하지 않은 페이지의 글자는 0번 페이지의 0번 셀로 그려진다.
        /// 시트는 16열x16행이라 페이지 하나가 글자 256자를 담고, 페이지 번호는 글자 코드를 256으로 나눈 몫이다.
        /// </summary>
        /// <param name="page">페이지 번호. 0이면 ASCII 범위(글자 코드 0~255).</param>
        /// <param name="relativePath">StreamingAssets 기준 상대 경로(예: "data/char.dds")</param>
        public void SetSpritePage(int page, string relativePath)
        {
            FontManager.Instance.SetSpritePage(page, relativePath);
        }

        /// <summary>
        /// 기본 폰트에 없는 글자를 대신 그릴 대체(Fallback) 폰트를 추가한다. 추가한 순서대로 먼저 찾는다.
        /// 여기서 넣은 폰트들 뒤에 언어별 기본 폰트가 자동으로 붙으므로, 빠진 글자는 결국 기본 폰트가 메운다.
        /// </summary>
        /// <param name="font">대체 폰트 핸들</param>
        public void AddFallback(TMPFontHandle font)
        {
            FontManager.Instance.AddUserFallback(font);
        }
    }
}
