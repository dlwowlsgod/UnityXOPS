using UnityEngine;
using UnityEngine.UI;

namespace UnityXOPS
{
    /// <summary>
    /// 마우스 포인터 위치에 따라 수평·수직 십자선 UI를 갱신하는 컴포넌트.
    /// </summary>
    public class MainmenuMousePointer : MonoBehaviour
    {
        [SerializeField]
        private RectTransform horizontal, vertical, canvasRect;
        [SerializeField]
        private Color32 color;

        /// <summary>
        /// 십자선 RawImage에 지정 색상을 적용한다.
        /// </summary>
        private void Start()
        {
            horizontal.GetComponent<RawImage>().color = color;
            vertical.GetComponent<RawImage>().color = color;
        }

        /// <summary>
        /// 매 프레임마다 마우스 스크린 좌표를 캔버스 로컬 좌표로 변환해 십자선 위치를 갱신한다.
        /// </summary>
        private void Update()
        {
            Vector2 screenPos = InputManager.Mouse.position.value;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out var localPoint);

            horizontal.anchoredPosition = new Vector2(0f, localPoint.y);
            vertical.anchoredPosition = new Vector2(localPoint.x, 0f);
        }
    }
}
