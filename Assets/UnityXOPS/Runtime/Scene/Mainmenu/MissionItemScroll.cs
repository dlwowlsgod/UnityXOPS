using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace UnityXOPS
{
    /// <summary>
    /// 드래그 가능한 스크롤바 UI로 미션 목록의 스크롤 위치를 제어하는 컴포넌트.
    /// </summary>
    public class MissionItemScroll : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        IDragHandler
    {
        [SerializeField]
        private Color32 outlineNormalColor, outlineHoverColor, outlinePressedColor, normalColor, hoverColor, pressedColor;
        [SerializeField]
        private RawImage outlineRawImage, rawImage;

        public event Action<int> OnScrollIndexChanged;

        public RawImage ScrollAreaRawImage { get; private set; }
        public RawImage ScrollbarRawImage  { get; private set; }

        private RectTransform m_trackRect;
        private RectTransform m_scrollbarRect;
        private int m_maxIndex;
        private int m_lastFiredIndex; 
        private float m_floatPosition;  
        private float m_trackHeight;
        private float m_barHeight;
        private float m_grabOffset;

        /// <summary>
        /// RawImage 및 RectTransform 참조를 캐싱한다.
        /// </summary>
        private void Awake()
        {
            ScrollAreaRawImage = GetComponent<RawImage>();
            ScrollbarRawImage  = outlineRawImage;
            m_trackRect = GetComponent<RectTransform>();
            m_scrollbarRect = outlineRawImage.GetComponent<RectTransform>();
        }

        /// <summary>
        /// 스크롤바 초기 색상을 설정한다.
        /// </summary>
        private void Start()
        {
            outlineRawImage.color = outlineNormalColor;
            rawImage.color = normalColor;
        }

        /// <summary>
        /// 아이템 총 수와 표시 수를 기반으로 스크롤바 크기와 최대 인덱스를 초기화한다.
        /// </summary>
        public void Initialize(int totalItems, int visibleItems, int maxIndex)
        {
            m_maxIndex = maxIndex;
            m_trackHeight = m_trackRect.sizeDelta.y;
            m_barHeight = m_trackHeight * visibleItems / totalItems;
            m_scrollbarRect.sizeDelta = new Vector2(m_scrollbarRect.sizeDelta.x, m_barHeight);
        }

        // 외부(Up/Down 버튼)에서 정수 인덱스로 위치 지정
        /// <summary>
        /// 외부에서 정수 인덱스로 스크롤 위치를 직접 지정한다.
        /// </summary>
        public void SetScrollPosition(int index)
        {
            m_floatPosition = index;
            m_lastFiredIndex = index;
            ApplyFloatPosition();
        }

        // float 위치를 스크롤바 anchoredPosition에 반영
        /// <summary>
        /// 현재 float 스크롤 위치를 스크롤바 anchoredPosition에 반영한다.
        /// </summary>
        private void ApplyFloatPosition()
        {
            float trackRange = m_trackHeight - m_barHeight;
            float t = m_maxIndex == 0 ? 0f : m_floatPosition / m_maxIndex;
            m_scrollbarRect.anchoredPosition = new Vector2(
                m_scrollbarRect.anchoredPosition.x,
                -trackRange * t
            );
        }

        // 로컬 포인터 좌표 → 연속적인 float 위치
        /// <summary>
        /// 트랙 내 로컬 포인터 좌표를 연속적인 float 스크롤 위치 값으로 변환한다.
        /// </summary>
        private float GetFloatFromLocal(Vector2 local)
        {
            float fromTop = m_trackRect.rect.yMax - local.y - m_grabOffset;
            float trackRange = m_trackHeight - m_barHeight;
            float normalized = Mathf.Clamp01(fromTop / trackRange);
            return normalized * m_maxIndex;
        }

        /// <summary>
        /// 포인터가 진입하면 호버 색상으로 변경한다.
        /// </summary>
        public void OnPointerEnter(PointerEventData _)
        {
            outlineRawImage.color = outlineHoverColor;
            rawImage.color = hoverColor;
        }

        /// <summary>
        /// 포인터가 이탈하면 기본 색상으로 복귀한다.
        /// </summary>
        public void OnPointerExit(PointerEventData _)
        {
            outlineRawImage.color = outlineNormalColor;
            rawImage.color = normalColor;
        }

        /// <summary>
        /// 포인터가 눌리면 눌린 색상을 적용하고 그랩 오프셋을 계산한 후 스크롤 위치를 갱신한다.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            outlineRawImage.color = outlinePressedColor;
            rawImage.color = pressedColor;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_trackRect, eventData.position, eventData.pressEventCamera, out Vector2 local);

            // 현재 스크롤바 상단의 트랙 상단으로부터 거리
            float trackRange = m_trackHeight - m_barHeight;
            float barTopFromTop = m_maxIndex == 0 ? 0f : m_floatPosition / m_maxIndex * trackRange;
            float clickFromTop = m_trackRect.rect.yMax - local.y;

            m_grabOffset = Mathf.Clamp(clickFromTop - barTopFromTop, 0f, m_barHeight);

            UpdateFromLocal(local);
        }

        /// <summary>
        /// 포인터가 떼어지면 포인터 위치에 따라 색상을 결정한다.
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            bool stillOver = eventData.pointerCurrentRaycast.gameObject == outlineRawImage.gameObject;
            outlineRawImage.color = stillOver ? outlineHoverColor : outlineNormalColor;
            rawImage.color = stillOver ? hoverColor : normalColor;
        }

        /// <summary>
        /// 드래그 중 포인터 좌표로 스크롤 위치를 갱신한다.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_trackRect, eventData.position, eventData.pressEventCamera, out Vector2 local);

            UpdateFromLocal(local);
        }

        /// <summary>
        /// 로컬 좌표로 float 위치를 계산하고 변경된 경우 OnScrollIndexChanged 이벤트를 발생시킨다.
        /// </summary>
        private void UpdateFromLocal(Vector2 local)
        {
            m_floatPosition = GetFloatFromLocal(local);
            ApplyFloatPosition();

            int index = Mathf.Clamp(Mathf.FloorToInt(m_floatPosition), 0, m_maxIndex);
            if (index != m_lastFiredIndex)
            {
                m_lastFiredIndex = index;
                OnScrollIndexChanged?.Invoke(index);
            }
        }
    }
}
