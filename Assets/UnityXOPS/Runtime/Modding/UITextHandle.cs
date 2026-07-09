using UnityEngine;
using XLua;

namespace UnityXOPS.Modding
{
    /// <summary>
    /// Lua가 생성한 스프라이트 폰트 텍스트(XOPSSpriteText)를 안전한 메서드로만 제어하는 핸들.
    /// 내부 Unity 객체는 직접 노출하지 않는다.
    /// </summary>
    [LuaCallCSharp]
    public class UITextHandle
    {
        private GameObject m_gameObject;
        private XOPSSpriteText m_text;
        private RectTransform m_rect;

        /// <summary>
        /// 핸들을 생성한다.
        /// </summary>
        /// <param name="gameObject">대상 GameObject</param>
        /// <param name="text">대상 XOPSSpriteText</param>
        public UITextHandle(GameObject gameObject, XOPSSpriteText text)
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
                m_text.Text = value;
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
        /// 요소 rect 크기를 설정한다. 글자 크기(SetFontSize)와 별개로, 정렬 기준 영역과 호버 판정 영역을 정한다.
        /// 좌측 정렬 텍스트를 버튼처럼 쓸 때 rect를 줄 크기로 넓혀 클릭 판정을 맞추는 데 쓴다.
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
        /// 글자 크기를 설정한다.
        /// </summary>
        /// <param name="width">글자 너비</param>
        /// <param name="height">글자 높이</param>
        public void SetFontSize(float width, float height)
        {
            if (m_text != null)
            {
                m_text.CharWidth = width;
                m_text.CharHeight = height;
            }
        }

        /// <summary>
        /// 색(rgba)을 설정한다.
        /// </summary>
        /// <param name="r">빨강(0~1)</param>
        /// <param name="g">초록(0~1)</param>
        /// <param name="b">파랑(0~1)</param>
        /// <param name="a">알파(0~1)</param>
        public void SetColor(float r, float g, float b, float a)
        {
            if (m_text != null)
            {
                m_text.FontColor = new Color(r, g, b, a);
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
                Color c = m_text.FontColor;
                m_text.FontColor = new Color(c.r, c.g, c.b, a);
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
        /// 현재 마우스 포인터가 이 텍스트 요소 위에 있는지 반환한다(호버 판정).
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
