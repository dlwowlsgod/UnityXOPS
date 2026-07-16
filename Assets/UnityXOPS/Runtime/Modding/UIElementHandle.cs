using UnityEngine;
using UnityEngine.UI;
using XLua;

namespace UnityXOPS.Modding
{
    /// <summary>
    /// Lua가 생성한 UI 이미지 요소(RawImage)를 안전한 메서드로만 제어하는 핸들.
    /// 내부 Unity 객체는 직접 노출하지 않는다.
    /// </summary>
    [LuaCallCSharp]
    public class UIElementHandle
    {
        private GameObject m_gameObject;
        private RawImage m_image;
        private RectTransform m_rect;

        /// <summary>
        /// 핸들을 생성한다.
        /// </summary>
        /// <param name="gameObject">대상 GameObject</param>
        /// <param name="image">대상 RawImage</param>
        public UIElementHandle(GameObject gameObject, RawImage image)
        {
            m_gameObject = gameObject;
            m_image = image;
            m_rect = image.rectTransform;
        }

        /// <summary>
        /// 위치를 설정한다(레이어 중심 기준).
        /// </summary>
        /// <param name="x">중심 기준 X</param>
        /// <param name="y">중심 기준 Y</param>
        public void SetPosition(float x, float y)
        {
            if (m_rect != null)
            {
                m_rect.anchoredPosition = new Vector2(x, y);
            }
        }

        /// <summary>
        /// Z축 회전(도)을 설정한다. 기울어진 막대(스코프 조준선 등)를 그릴 때 쓴다.
        /// </summary>
        /// <param name="degrees">회전 각도(도). 양수면 반시계 방향</param>
        public void SetRotation(float degrees)
        {
            if (m_rect != null)
            {
                m_rect.localRotation = Quaternion.Euler(0f, 0f, degrees);
            }
        }

        /// <summary>
        /// 크기를 설정한다.
        /// </summary>
        /// <param name="width">너비</param>
        /// <param name="height">높이</param>
        public void SetSize(float width, float height)
        {
            if (m_rect != null)
            {
                m_rect.sizeDelta = new Vector2(width, height);
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
            if (m_image != null)
            {
                m_image.color = new Color(r, g, b, a);
            }
        }

        /// <summary>
        /// rgb는 유지하고 알파만 설정한다. 펄스/페이드 연출용.
        /// </summary>
        /// <param name="a">알파(0~1)</param>
        public void SetAlpha(float a)
        {
            if (m_image != null)
            {
                Color c = m_image.color;
                m_image.color = new Color(c.r, c.g, c.b, a);
            }
        }

        /// <summary>
        /// 텍스처를 교체한다. 경로가 비면 텍스처 없는 패널(색만)이 된다.
        /// </summary>
        /// <param name="texturePath">streamingAssets 기준 이미지 경로</param>
        public void SetTexture(string texturePath)
        {
            if (m_image == null)
            {
                return;
            }

            // 텍스처는 ImageLoader 캐시가 소유하므로 이전 텍스처를 Destroy하지 않는다.
            m_image.texture = UIOverlayManager.LoadTexture(texturePath);
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
        /// 이 요소를 부모로 하는 자식 이미지/패널(RawImage)을 생성해 핸들을 반환한다.
        /// 앵커/스트레치가 이 요소 기준으로 계산되므로, 부모가 움직이거나 크기가 바뀌면 자식이 따라간다(중첩 UI).
        /// </summary>
        /// <param name="pivot">앵커/피벗 기준 지점 이름("StretchTop", "center" 등). 대소문자/구분자 무시</param>
        /// <param name="texturePath">streamingAssets 기준 이미지 경로(빈 문자열=패널)</param>
        /// <param name="x">기준 지점 기준 X 오프셋(스트레치 축이면 sizeDelta 보정)</param>
        /// <param name="y">기준 지점 기준 Y 오프셋(스트레치 축이면 sizeDelta 보정)</param>
        /// <param name="width">너비(스트레치 축이면 부모 대비 증감량)</param>
        /// <param name="height">높이(스트레치 축이면 부모 대비 증감량)</param>
        /// <param name="r">빨강(0~1)</param>
        /// <param name="g">초록(0~1)</param>
        /// <param name="b">파랑(0~1)</param>
        /// <param name="a">알파(0~1)</param>
        /// <returns>자식 요소 제어 핸들. 이 요소가 이미 파괴됐으면 null</returns>
        public UIElementHandle CreateChildImage(string pivot, string texturePath, float x, float y, float width, float height, float r, float g, float b, float a)
        {
            if (m_rect == null)
            {
                return null;
            }
            return UIOverlayManager.Instance.CreateImageUnder(m_rect, UIOverlayManager.ParsePivot(pivot), texturePath, x, y, width, height, r, g, b, a);
        }

        /// <summary>
        /// 이 요소를 부모로 하는 자식 스프라이트 텍스트를 생성해 핸들을 반환한다.
        /// 컨테이너(투명 패널) 밑에 묶어 두면 컨테이너 하나의 SetActive로 그룹 전체를 토글할 수 있다(탭/패널 전환).
        /// </summary>
        /// <param name="pivot">UI 요소 기준점 이름("center", "top_left" 등). 대소문자/구분자 무시</param>
        /// <param name="alignment">글자 정렬 기준점 이름("center", "left" 등). 대소문자/구분자 무시</param>
        /// <param name="text">표시할 문자열</param>
        /// <param name="x">기준 지점 기준 X 오프셋(오른쪽 +)</param>
        /// <param name="y">기준 지점 기준 Y 오프셋(위쪽 +)</param>
        /// <param name="fontWidth">글자 너비</param>
        /// <param name="fontHeight">글자 높이</param>
        /// <param name="spacing">글자 간격</param>
        /// <param name="r">빨강(0~1)</param>
        /// <param name="g">초록(0~1)</param>
        /// <param name="b">파랑(0~1)</param>
        /// <param name="a">알파(0~1)</param>
        /// <returns>자식 텍스트 제어 핸들. 이 요소가 이미 파괴됐으면 null</returns>
        public UITextHandle CreateChildText(string pivot, string alignment, string text, float x, float y, float fontWidth, float fontHeight, float spacing, float r, float g, float b, float a)
        {
            if (m_rect == null)
            {
                return null;
            }
            return UIOverlayManager.Instance.CreateTextUnder(m_rect, UIOverlayManager.ParsePivot(pivot), UIOverlayManager.ParsePivot(alignment), text, x, y, fontWidth, fontHeight, spacing, r, g, b, a);
        }

        /// <summary>
        /// 이 요소를 부모로 하는 자식 OS 폰트(TMP) 텍스트를 생성해 핸들을 반환한다. 스프라이트 폰트 대신 가독성 텍스트용.
        /// CreateChildText와 달리 글자 크기가 스칼라(pt) 하나다(w/h가 아님).
        /// </summary>
        /// <param name="pivot">UI 요소 기준점 이름("center", "top_left" 등). 대소문자/구분자 무시</param>
        /// <param name="alignment">글자 정렬 기준점 이름("center", "left" 등). 대소문자/구분자 무시</param>
        /// <param name="text">표시할 문자열</param>
        /// <param name="x">기준 지점 기준 X 오프셋(오른쪽 +)</param>
        /// <param name="y">기준 지점 기준 Y 오프셋(위쪽 +)</param>
        /// <param name="fontSize">글자 크기(pt)</param>
        /// <param name="r">빨강(0~1)</param>
        /// <param name="g">초록(0~1)</param>
        /// <param name="b">파랑(0~1)</param>
        /// <param name="a">알파(0~1)</param>
        /// <returns>자식 OS 폰트 텍스트 제어 핸들. 이 요소가 이미 파괴됐으면 null</returns>
        public UIOSTextHandle CreateChildOSText(string pivot, string alignment, string text, float x, float y, float fontSize, float r, float g, float b, float a)
        {
            if (m_rect == null)
            {
                return null;
            }
            return UIOverlayManager.Instance.CreateOSTextUnder(m_rect, UIOverlayManager.ParsePivot(pivot), UIOverlayManager.ParsePivot(alignment), text, x, y, fontSize, r, g, b, a);
        }

        /// <summary>
        /// 현재 마우스 포인터가 이 요소 위에 있는지 반환한다(호버 판정).
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
        /// 요소를 파괴한다. 텍스처는 ImageLoader 캐시가 소유하므로 함께 해제하지 않는다.
        /// </summary>
        public void Destroy()
        {
            if (m_gameObject == null)
            {
                return;
            }

            Object.Destroy(m_gameObject);
            m_gameObject = null;
            m_image = null;
            m_rect = null;
        }
    }
}
