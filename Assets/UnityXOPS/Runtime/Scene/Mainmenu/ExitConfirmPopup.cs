using UnityEngine;
using UnityEngine.UI;

namespace UnityXOPS
{
    /// <summary>
    /// 게임 종료 확인 팝업을 구성하고 종료·취소 버튼의 클릭 이벤트를 처리하는 컴포넌트.
    /// </summary>
    public class ExitConfirmPopup : MonoBehaviour
    {
        [SerializeField]
        private Transform root, labelRoot, quitRoot, abortRoot;

        [SerializeField]
        private Color32 normalColor, hoverColor, pressedColor, shadowColor;

        [SerializeField]
        private Vector2 labelSize, buttonSize;

        private const string k_label = "Do you want to quit the game?";
        private const string k_quitText = "QUIT";
        private const string k_abortText = "ABORT";

        private void Start()
        {
            Vector2 quitButtonSize = new Vector2(labelSize.x * k_quitText.Length, labelSize.y + 2);
            Vector2 abortButtonSize = new Vector2(labelSize.x * k_abortText.Length, labelSize.y + 2);

            FontManager.CreateSpriteText<XOPSSpriteText>(
                labelRoot, k_label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1, -1),
                labelSize, labelSize, shadowColor, TextAnchor.MiddleCenter, 0);
            FontManager.CreateSpriteText<XOPSSpriteText>(
                labelRoot, k_label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                labelSize, labelSize, normalColor, TextAnchor.MiddleCenter, 0);

            var quitButtonText = CreateButton(quitRoot, k_quitText, quitButtonSize);
            var abortButtonText = CreateButton(abortRoot, k_abortText, abortButtonSize);

            quitButtonText.OnClick += OnQuitButtonPressed;
            abortButtonText.OnClick += OnAbortButtonPressed;
        }

        /// <summary>
        /// 지정 루트(parent)에 그림자+버튼 2겹 스프라이트 텍스트를 생성하고 색/오프셋을 설정해 버튼을 반환한다.
        /// text=표시 문자열, buttonAreaSize=버튼 영역 크기.
        /// </summary>
        private XOPSSpriteTextButton CreateButton(Transform parent, string text, Vector2 buttonAreaSize)
        {
            FontManager.CreateSpriteText<XOPSSpriteText>(
                parent, text, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1, -1),
                buttonAreaSize, buttonSize, shadowColor, TextAnchor.MiddleCenter, 0);
            var button = FontManager.CreateSpriteText<XOPSSpriteTextButton>(
                parent, text, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                buttonAreaSize, buttonSize, normalColor, TextAnchor.MiddleCenter, 0);
            button.NormalColor = normalColor;
            button.HoverColor = hoverColor;
            button.PressedColor = pressedColor;
            button.MovePixelX = 1f;
            button.MovePixelY = -1f;
            return button;
        }

        /// <summary>
        /// 에디터에서는 플레이 모드를 종료하고, 빌드에서는 애플리케이션을 종료한다.
        /// </summary>
        public void OnQuitButtonPressed()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// 팝업 루트 오브젝트를 비활성화하여 팝업을 닫는다.
        /// </summary>
        public void OnAbortButtonPressed()
        {
            root.gameObject.SetActive(false);
        }
    }
}
