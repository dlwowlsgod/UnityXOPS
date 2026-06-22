using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace UnityXOPS
{
    /// <summary>
    /// 마우스 호버/클릭 상태에 따라 색상과 위치가 변화하는 버튼 기능이 추가된 스프라이트 텍스트 컴포넌트.
    /// </summary>
    public class XOPSSpriteTextButton : XOPSSpriteText,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField]
        private Color32 normalColor, hoverColor, pressedColor;
        [SerializeField]
        private float movePixelX, movePixelY;

        public event Action OnClick;

        public Color32 NormalColor
        {
            get => normalColor;
            set => normalColor = value;
        }
        public Color32 HoverColor
        {
            get => hoverColor;
            set => hoverColor = value;
        }
        public Color32 PressedColor
        {
            get => pressedColor;
            set => pressedColor = value;
        }
        public float MovePixelX
        {
            get => movePixelX;
            set => movePixelX = value;
        }
        public float MovePixelY
        {
            get => movePixelY;
            set => movePixelY = value;
        }

        private static XOPSSpriteTextButton s_pressedItem;

        private Vector2 m_originalAnchoredPos;

        protected override void Start()
        {
            base.Start();
            m_originalAnchoredPos = rectTransform.anchoredPosition;
            FontColor = normalColor;
        }

        public void OnPointerEnter(PointerEventData _)
        {
            if (s_pressedItem == null)
                FontColor = hoverColor;
        }

        public void OnPointerExit(PointerEventData _)
        {
            if (s_pressedItem == null)
                FontColor = normalColor;
        }

        public void OnPointerDown(PointerEventData _)
        {
            s_pressedItem = this;
            FontColor = pressedColor;
            rectTransform.anchoredPosition = m_originalAnchoredPos + new Vector2(movePixelX, movePixelY);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            s_pressedItem = null;
            rectTransform.anchoredPosition = m_originalAnchoredPos;

            FontColor = eventData.pointerCurrentRaycast.gameObject == gameObject
                ? hoverColor
                : normalColor;
        }

        public void OnPointerClick(PointerEventData _) => OnClick?.Invoke();
    }
}
