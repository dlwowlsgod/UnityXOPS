using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityXOPS
{
    /// <summary>
    /// 메인 메뉴의 스크롤을 담당하는 UI 클래스입니다.
    /// </summary>
    public class ScrollBar : MonoBehaviour
    {
        [SerializeField]
        private Color outlineColor;
        [SerializeField]
        private Color outlineHoverColor;
        [SerializeField]
        private Color outlinePressColor;
        [SerializeField]
        private Color insideColor;
        [SerializeField]
        private Color insideHoverColor;
        [SerializeField]
        private Color insidePressColor;

        private RectTransform _rect;
        private RectTransform _bar;
        private RawImage _outline;
        private RawImage _inside;

        private bool _isHover;
        private bool _isPress;
        private Vector2 _lastMousePosition;

        private float _min;
        private float _max;
        
        public Vector2 MinMax => new Vector2(_min, _max);
        
        public event Action<float> OnScroll;
        
        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _bar = transform.parent.GetComponent<RectTransform>();
            _outline = GetComponent<RawImage>();
            _inside = transform.GetChild(0).GetComponent<RawImage>();
            
            _min = 0f;
            _max = 300f;
            
            _outline.color = outlineColor;
            _inside.color = insideColor;

            var officialMissionCount = ParameterManager.Instance.officialMissionParameters.Count;
            UpdateScrollSizeAndConstraint(officialMissionCount);
        }

        private void Update()
        {
            //마우스 왼쪽버튼 놓을 시 드래그 해제
            if (Input.GetMouseButtonUp(0))
            {
                _isPress = false;
            }

            if (_isPress)
            {
                var currentMousePosition = Input.mousePosition;
                var mouseDeltaY = currentMousePosition.y - _lastMousePosition.y;

                var anchoredPosition = _rect.anchoredPosition;
                var newY = anchoredPosition.y + mouseDeltaY;
                
                //위치 제한
                newY = Mathf.Clamp(newY, -_max, _min);
                _rect.anchoredPosition = new Vector2(anchoredPosition.x, newY);
                
                _lastMousePosition = currentMousePosition;
                
                if (_max > 0)
                {
                    float percentage = -_rect.anchoredPosition.y / _max;
                    OnScroll?.Invoke(percentage);
                }

            }

            if (_isPress)
            {
                _outline.color = outlinePressColor;
                _inside.color = insidePressColor;
            }
            else if (_isHover)
            {
                _outline.color = outlineHoverColor;
                _inside.color = insideHoverColor;
            }
            else
            {
                _outline.color = outlineColor;
                _inside.color = insideColor;
            }
        }

        /// <summary>
        /// 외부에서 스크롤바의 크기와 이동 범위를 설정하는 함수입니다.
        /// </summary>
        /// <param name="menuCount">이 스크롤 박스의 아이템 개수</param>
        public void UpdateScrollSizeAndConstraint(int menuCount)
        {
            if (menuCount < 9)
            {
                transform.parent.gameObject.SetActive(false);
                return;
            }
            
            transform.parent.gameObject.SetActive(true);
            var handleRatio = (float)8 / menuCount;
            _rect.sizeDelta = new Vector2(_rect.sizeDelta.x, _bar.sizeDelta.y * handleRatio);

            _min = 0f;
            _max = _bar.sizeDelta.y - _rect.sizeDelta.y;
        }
        
        /// <summary>
        /// 외부에서 스크롤바의 위치를 비율로 설정하는 함수입니다.
        /// </summary>
        /// <param name="percentage">스크롤 위치 비율 (0.0 to 1.0)</param>
        public void SetScrollPosition(float percentage)
        {
            if (_isPress) return;

            percentage = Mathf.Clamp01(percentage);
            var newY = Mathf.Lerp(_min, -_max, percentage);
            _rect.anchoredPosition = new Vector2(_rect.anchoredPosition.x, newY);
        }


        public void PointerEnter()
        {
            _isHover = true;
        }
        
        public void PointerExit()
        {
            _isHover = false;
        }

        public void PointerDown()
        {
            _isPress = true;
            _lastMousePosition = Input.mousePosition;
        }
        
        public void PointerUp()
        {
            _isPress = false;
        }
    }
}