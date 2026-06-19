using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 공식 미션과 애드온 미션 탭 간의 전환을 처리하는 컴포넌트.
    /// </summary>
    public class MissionSwitch : MonoBehaviour
    {
        [SerializeField]
        private GameObject officialMissionCanvas, addonMissionCanvas;
        [SerializeField]
        private Transform officialSwitchTransform, addonSwitchTransform;

        private const string k_toAddonText = "ADD-ON MISSIONS >>", k_toOfficialText = "<< STANDARD MISSIONS";

        [SerializeField]
        private Color32 normalColor, hoverColor, pressedColor, shadowColor;
        [SerializeField]
        private Vector2 fontSize;

        private void Start()
        {
            var toAddonButton = CreateSwitchButton(officialSwitchTransform, k_toAddonText, "officialShadowText", "officialButtonText");
            var toOfficialButton = CreateSwitchButton(addonSwitchTransform, k_toOfficialText, "addonShadowText", "addonButtonText");

            toAddonButton.OnClick += ToAddonSwitchClicked;
            toOfficialButton.OnClick += ToOfficialSwitchClicked;

            addonSwitchTransform.gameObject.SetActive(false);

            if (MainmenuScene.IsAddonTab)
                ToAddonSwitchClicked();
        }

        /// <summary>
        /// 지정 루트(parent)에 그림자+버튼 2겹 탭 전환 텍스트를 생성하고 색/오프셋을 설정해 버튼을 반환한다.
        /// text=표시 문자열, shadowName/buttonName=각 오브젝트 이름.
        /// </summary>
        private XOPSSpriteTextButton CreateSwitchButton(Transform parent, string text, string shadowName, string buttonName)
        {
            var shadowText = FontManager.CreateSpriteText<XOPSSpriteText>(
                parent, text, new Vector2(0, 1), new Vector2(0, 1), new Vector2(1, -1),
                Vector2.zero, fontSize, shadowColor, TextAnchor.UpperLeft, 0);
            var buttonText = FontManager.CreateSpriteText<XOPSSpriteTextButton>(
                parent, text, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero,
                new Vector2(340, 25), fontSize, normalColor, TextAnchor.UpperLeft, 0);
            shadowText.name = shadowName;
            buttonText.name = buttonName;
            buttonText.color = buttonText.NormalColor = normalColor;
            buttonText.HoverColor = hoverColor;
            buttonText.PressedColor = pressedColor;
            buttonText.MovePixelX = 1f;
            buttonText.MovePixelY = -1f;
            return buttonText;
        }

        /// <summary>
        /// 애드온 탭으로 전환하며 관련 UI를 활성화하고 공식 탭 UI를 비활성화한다.
        /// </summary>
        private void ToAddonSwitchClicked()
        {
            MainmenuScene.IsAddonTab = true;

            officialMissionCanvas.SetActive(false);
            addonMissionCanvas.SetActive(true);

            officialSwitchTransform.gameObject.SetActive(false);
            addonSwitchTransform.gameObject.SetActive(true);
        }

        /// <summary>
        /// 공식 탭으로 전환하며 관련 UI를 활성화하고 애드온 탭 UI를 비활성화한다.
        /// </summary>
        private void ToOfficialSwitchClicked()
        {
            MainmenuScene.IsAddonTab = false;

            officialMissionCanvas.SetActive(true);
            addonMissionCanvas.SetActive(false);

            officialSwitchTransform.gameObject.SetActive(true);
            addonSwitchTransform.gameObject.SetActive(false);
        }
    }
}
