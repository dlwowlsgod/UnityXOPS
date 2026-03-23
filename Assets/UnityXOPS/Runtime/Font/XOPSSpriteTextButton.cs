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

        // 현재 눌린 아이템 
        private static XOPSSpriteTextButton s_pressedItem;

        private Vector2 m_originalAnchoredPos;

        /// <summary>
        /// 초기 앵커 위치를 저장하고 기본 색상을 적용한다.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            m_originalAnchoredPos = rectTransform.anchoredPosition;
            FontColor = normalColor; //나중에 따로 다시 호출함. 설계적 미스 ㅋ
        }

        /// <summary>
        /// 포인터가 진입하면 호버 색상으로 변경한다.
        /// </summary>
        public void OnPointerEnter(PointerEventData _)
        {
            if (s_pressedItem == null)
                FontColor = hoverColor;
        }

        /// <summary>
        /// 포인터가 이탈하면 기본 색상으로 복귀한다.
        /// </summary>
        public void OnPointerExit(PointerEventData _)
        {
            if (s_pressedItem == null)
                FontColor = normalColor;
        }

        /// <summary>
        /// 포인터가 눌리면 눌린 색상과 위치 오프셋을 적용한다.
        /// </summary>
        public void OnPointerDown(PointerEventData _)
        {
            s_pressedItem = this;
            FontColor = pressedColor;
            rectTransform.anchoredPosition = m_originalAnchoredPos + new Vector2(movePixelX, movePixelY);
        }

        /// <summary>
        /// 포인터가 떼어지면 위치를 복원하고 포인터 위치에 따라 색상을 결정한다.
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            s_pressedItem = null;
            rectTransform.anchoredPosition = m_originalAnchoredPos;

            // 포인터가 아직 위에 있으면 hover 색으로 복귀
            FontColor = eventData.pointerCurrentRaycast.gameObject == gameObject
                ? hoverColor
                : normalColor;
        }

        /// <summary>
        /// 포인터 클릭 시 OnClick 이벤트를 발생시킨다.
        /// </summary>
        public void OnPointerClick(PointerEventData _) => OnClick?.Invoke();
    }
}
