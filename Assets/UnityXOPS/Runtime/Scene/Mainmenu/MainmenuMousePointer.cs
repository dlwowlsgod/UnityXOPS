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

        private void Start()
        {
            horizontal.GetComponent<RawImage>().color = color;
            vertical.GetComponent<RawImage>().color = color;
        }

        private void Update()
        {
            Vector2 screenPos = InputManager.Mouse.position.value;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out var localPoint);

            horizontal.anchoredPosition = new Vector2(0f, localPoint.y);
            vertical.anchoredPosition = new Vector2(localPoint.x, 0f);
        }
    }
}
