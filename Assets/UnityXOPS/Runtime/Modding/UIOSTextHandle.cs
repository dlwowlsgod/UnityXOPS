using TMPro;
using UnityEngine;
using XLua;

namespace UnityXOPS.Modding
{
    /// <summary>
    /// Lua가 생성한 OS 폰트 텍스트(TextMeshProUGUI)를 안전한 메서드로만 제어하는 핸들.
    /// 내부 Unity 객체는 직접 노출하지 않는다. XOPSSpriteText용은 UITextHandle, 이쪽은 OS 폰트(가독성)용.
    /// </summary>
    [LuaCallCSharp]
    public class UIOSTextHandle
    {
        private GameObject m_gameObject;
        private TextMeshProUGUI m_text;
        private RectTransform m_rect;

        /// <summary>
        /// 핸들을 생성한다.
        /// </summary>
        /// <param name="gameObject">대상 GameObject</param>
        /// <param name="text">대상 TextMeshProUGUI</param>
        public UIOSTextHandle(GameObject gameObject, TextMeshProUGUI text)
        {
            m_gameObject = gameObject;
            m_text = text;
            m_rect = text.rectTransform;
        }

        /// <summary>
        /// 표시할 문자열을 교체한다.
        /// </summary>
        /// <param name="value">새 텍스트</param>
        public void SetText(string value)
        {
            if (m_text != null)
            {
                m_text.text = value;
            }
        }

        /// <summary>
        /// 위치를 설정한다(피벗 기준 오프셋).
        /// </summary>
        /// <param name="x">X 오프셋(오른쪽 +)</param>
        /// <param name="y">Y 오프셋(위쪽 +)</param>
        public void SetPosition(float x, float y)
        {
            if (m_rect != null)
            {
                m_rect.anchoredPosition = new Vector2(x, y);
            }
        }

        /// <summary>
        /// 요소 rect 크기를 설정한다. 좌측 정렬 텍스트를 버튼처럼 쓸 때 호버 판정 영역을 줄에 맞추는 데 쓴다.
        /// </summary>
        /// <param name="width">rect 너비</param>
        /// <param name="height">rect 높이</param>
        public void SetSize(float width, float height)
        {
            if (m_rect != null)
            {
                m_rect.sizeDelta = new Vector2(width, height);
            }
        }

        /// <summary>
        /// 글자 크기(pt)를 설정한다.
        /// </summary>
        /// <param name="size">글자 크기</param>
        public void SetFontSize(float size)
        {
            if (m_text != null)
            {
                m_text.fontSize = size;
            }
        }

        /// <summary>
        /// 글자 크기 자동 맞춤(Auto Size)을 켜고 끈다. 켜면 rect 안에 들어가도록 min~max 사이에서 크기가 자동 조절되고,
        /// 이때 SetFontSize 값은 무시된다. 끄면 다시 고정 크기(SetFontSize)로 돌아간다.
        /// </summary>
        /// <param name="enabled">자동 맞춤 사용 여부</param>
        /// <param name="min">최소 글자 크기(pt)</param>
        /// <param name="max">최대 글자 크기(pt)</param>
        public void SetAutoSize(bool enabled, float min, float max)
        {
            if (m_text != null)
            {
                m_text.enableAutoSizing = enabled;
                m_text.fontSizeMin = min;
                m_text.fontSizeMax = max;
            }
        }

        /// <summary>
        /// 줄 간격을 설정한다. 글자 크기에 대한 상대값이라 크기를 바꿔도 비율이 유지된다.
        /// </summary>
        /// <param name="spacing">줄 간격(0=기본, 음수=좁게, 양수=넓게)</param>
        public void SetLineSpacing(float spacing)
        {
            if (m_text != null)
            {
                m_text.lineSpacing = spacing;
            }
        }

        /// <summary>
        /// 줄바꿈 방식을 설정한다. "nowrap"=줄바꿈 없음, "normal"=자동 줄바꿈,
        /// "preserve"=공백 유지 줄바꿈, "preservenowrap"=공백 유지+줄바꿈 없음. 알 수 없으면 normal.
        /// </summary>
        /// <param name="mode">줄바꿈 방식 이름(대소문자/구분자 무시)</param>
        public void SetWrappingMode(string mode)
        {
            if (m_text != null)
            {
                m_text.textWrappingMode = ParseWrappingMode(mode);
            }
        }

        /// <summary>
        /// 넘침(Overflow) 처리 방식을 설정한다. "overflow"=넘쳐도 그대로, "ellipsis"=말줄임(…),
        /// "masking"=rect 밖 잘라 가림, "truncate"=넘치면 자름. 알 수 없으면 overflow.
        /// </summary>
        /// <param name="mode">넘침 처리 방식 이름(대소문자/구분자 무시)</param>
        public void SetOverflowMode(string mode)
        {
            if (m_text != null)
            {
                m_text.overflowMode = ParseOverflowMode(mode);
            }
        }

        /// <summary>
        /// 색(rgba)을 설정한다. TMP의 color는 버텍스 컬러라 생성 이후 언제든 바꿀 수 있다.
        /// </summary>
        /// <param name="r">빨강(0~1)</param>
        /// <param name="g">초록(0~1)</param>
        /// <param name="b">파랑(0~1)</param>
        /// <param name="a">알파(0~1)</param>
        public void SetColor(float r, float g, float b, float a)
        {
            if (m_text != null)
            {
                m_text.color = new Color(r, g, b, a);
            }
        }

        /// <summary>
        /// rgb는 유지하고 알파만 설정한다. 펄스/페이드 연출용.
        /// </summary>
        /// <param name="a">알파(0~1)</param>
        public void SetAlpha(float a)
        {
            if (m_text != null)
            {
                Color c = m_text.color;
                m_text.color = new Color(c.r, c.g, c.b, a);
            }
        }

        /// <summary>
        /// 표시 여부를 설정한다.
        /// </summary>
        /// <param name="active">표시 여부</param>
        public void SetActive(bool active)
        {
            if (m_gameObject != null)
            {
                m_gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// 현재 마우스 포인터가 이 요소 위에 있는지 반환한다(호버 판정). rect 크기가 0이면 판정 안 됨 — SetSize로 키운다.
        /// </summary>
        /// <returns>포인터가 요소 영역 안이면 true</returns>
        public bool IsHovered()
        {
            return UIOverlayManager.IsPointerOver(m_rect);
        }

        /// <summary>
        /// 마우스 포인터의 이 요소 로컬 X 좌표를 반환한다(드래그/슬라이더용).
        /// </summary>
        /// <returns>로컬 X 좌표</returns>
        public float PointerLocalX()
        {
            return UIOverlayManager.PointerLocal(m_rect).x;
        }

        /// <summary>
        /// 마우스 포인터의 이 요소 로컬 Y 좌표를 반환한다(드래그/슬라이더용).
        /// </summary>
        /// <returns>로컬 Y 좌표</returns>
        public float PointerLocalY()
        {
            return UIOverlayManager.PointerLocal(m_rect).y;
        }

        /// <summary>
        /// 줄바꿈 방식 이름을 TextWrappingModes로 파싱한다(대소문자/구분자 무시).
        /// </summary>
        /// <param name="mode">줄바꿈 방식 이름</param>
        /// <returns>대응하는 TextWrappingModes. 알 수 없으면 Normal.</returns>
        private static TextWrappingModes ParseWrappingMode(string mode)
        {
            switch (Normalize(mode))
            {
                case "nowrap": return TextWrappingModes.NoWrap;
                case "preserve":
                case "preservewhitespace": return TextWrappingModes.PreserveWhitespace;
                case "preservenowrap":
                case "preservewhitespacenowrap": return TextWrappingModes.PreserveWhitespaceNoWrap;
                default: return TextWrappingModes.Normal;
            }
        }

        /// <summary>
        /// 넘침 처리 방식 이름을 TextOverflowModes로 파싱한다(대소문자/구분자 무시).
        /// </summary>
        /// <param name="mode">넘침 처리 방식 이름</param>
        /// <returns>대응하는 TextOverflowModes. 알 수 없으면 Overflow.</returns>
        private static TextOverflowModes ParseOverflowMode(string mode)
        {
            switch (Normalize(mode))
            {
                case "ellipsis": return TextOverflowModes.Ellipsis;
                case "masking": return TextOverflowModes.Masking;
                case "truncate": return TextOverflowModes.Truncate;
                default: return TextOverflowModes.Overflow;
            }
        }

        /// <summary>
        /// 문자열을 소문자로 바꾸고 공백/밑줄/하이픈을 제거해 파싱 비교용으로 정규화한다.
        /// </summary>
        /// <param name="value">원본 문자열</param>
        /// <returns>정규화된 문자열. null이면 빈 문자열.</returns>
        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            return value.Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "");
        }

        /// <summary>
        /// 요소를 파괴한다.
        /// </summary>
        public void Destroy()
        {
            if (m_gameObject == null)
            {
                return;
            }

            Object.Destroy(m_gameObject);
            m_gameObject = null;
            m_text = null;
            m_rect = null;
        }
    }
}
