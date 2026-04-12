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

        /// <summary>
        /// 종료·취소 버튼 텍스트를 생성하고 클릭 이벤트를 등록한다.
        /// </summary>
        private void Start()
        {
            Vector2 quitButtonSize = new Vector2(labelSize.x * k_quitText.Length, labelSize.y + 2);
            Vector2 abortButtonSize = new Vector2(labelSize.x * k_abortText.Length, labelSize.y + 2);

            var labelShadowText = FontManager.CreateSpriteText<XOPSSpriteText>(
                labelRoot, k_label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1, -1),
                labelSize, labelSize, shadowColor, TextAnchor.MiddleCenter, 0);
            var labelText = FontManager.CreateSpriteText<XOPSSpriteText>(
                labelRoot, k_label, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                labelSize, labelSize, normalColor, TextAnchor.MiddleCenter, 0);

            var quitShadowText = FontManager.CreateSpriteText<XOPSSpriteText>(
                quitRoot, k_quitText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1, -1),
                quitButtonSize, buttonSize, shadowColor, TextAnchor.MiddleCenter, 0);
            var quitButtonText = FontManager.CreateSpriteText<XOPSSpriteTextButton>(
                quitRoot, k_quitText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                quitButtonSize, buttonSize, normalColor, TextAnchor.MiddleCenter, 0);
            quitButtonText.NormalColor = normalColor;
            quitButtonText.HoverColor = hoverColor;
            quitButtonText.PressedColor = pressedColor;
            quitButtonText.MovePixelX = 1f;
            quitButtonText.MovePixelY = -1f;

            var abortShadowText = FontManager.CreateSpriteText<XOPSSpriteText>(
                abortRoot, k_abortText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1, -1),
                abortButtonSize, buttonSize, shadowColor, TextAnchor.MiddleCenter, 0);
            var abortButtonText = FontManager.CreateSpriteText<XOPSSpriteTextButton>(
                abortRoot, k_abortText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                abortButtonSize, buttonSize, normalColor, TextAnchor.MiddleCenter, 0);
            abortButtonText.NormalColor = normalColor;
            abortButtonText.HoverColor = hoverColor;
            abortButtonText.PressedColor = pressedColor;
            abortButtonText.MovePixelX = 1f;
            abortButtonText.MovePixelY = -1f;

            quitButtonText.OnClick += () => OnQuitButtonPressed();
            abortButtonText.OnClick += () => OnAbortButtonPressed();
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
