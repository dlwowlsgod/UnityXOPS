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
