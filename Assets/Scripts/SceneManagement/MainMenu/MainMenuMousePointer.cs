using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// Handles mouse pointer behavior and interactions within the main menu.
    /// </summary>
    public class MainMenuMousePointer : MonoBehaviour
    {
        [SerializeField] 
        private GameObject horizontal;
        [SerializeField] 
        private GameObject vertical;

        private RectTransform _horizontalRect;
        private RectTransform _verticalRect;
        private RectTransform _parentRect;

        private Canvas _canvas;
        
        private void Start()
        {
            _horizontalRect = horizontal.GetComponent<RectTransform>();
            _verticalRect = vertical.GetComponent<RectTransform>();

            _canvas = GetComponent<Canvas>();
            _parentRect = GetComponent<RectTransform>();

            Cursor.visible = false;
        }

        private void Update()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, Input.mousePosition, null, out var localPoint);
            
            _horizontalRect.anchoredPosition = new Vector2(0, localPoint.y);
            _verticalRect.anchoredPosition = new Vector2(localPoint.x, 0);
        }

        private void OnDestroy()
        {
            Cursor.visible = true;
        }
    }
}