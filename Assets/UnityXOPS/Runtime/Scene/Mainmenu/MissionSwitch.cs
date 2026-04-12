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
        private XOPSSpriteTextButton toAddonButton, toOfficialButton;
        [SerializeField]
        private Vector2 fontSize;

        /// <summary>
        /// 탭 전환 버튼 텍스트를 생성하고 클릭 이벤트를 등록한 후 초기 탭 상태를 적용한다.
        /// </summary>
        private void Start()
        {
            var officialShadowText = FontManager.CreateSpriteText<XOPSSpriteText>(
                officialSwitchTransform, k_toAddonText, new Vector2(0, 1), new Vector2(0, 1), new Vector2(1, -1)
                , Vector2.zero, fontSize, shadowColor, TextAnchor.UpperLeft, 0);
            var officialButtonText = FontManager.CreateSpriteText<XOPSSpriteTextButton>(
                officialSwitchTransform, k_toAddonText, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero
                , new Vector2(340, 25), fontSize, normalColor, TextAnchor.UpperLeft, 0);
            officialShadowText.name = "officialShadowText";
            officialButtonText.name = "officialButtonText";
            officialButtonText.color = officialButtonText.NormalColor = normalColor;
            officialButtonText.HoverColor = hoverColor;
            officialButtonText.PressedColor = pressedColor;
            officialButtonText.MovePixelX = 1f;
            officialButtonText.MovePixelY = -1f;
            toAddonButton = officialButtonText;

            var addonShadowText = FontManager.CreateSpriteText<XOPSSpriteText>(
                addonSwitchTransform, k_toOfficialText, new Vector2(0, 1), new Vector2(0, 1), new Vector2(1, -1)
                , Vector2.zero, fontSize, shadowColor, TextAnchor.UpperLeft, 0);
            var addonButtonText = FontManager.CreateSpriteText<XOPSSpriteTextButton>(
                addonSwitchTransform, k_toOfficialText, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero
                , new Vector2(340, 25), fontSize, normalColor, TextAnchor.UpperLeft, 0);
            addonShadowText.name = "addonShadowText";
            addonButtonText.name = "addonButtonText";
            addonButtonText.color = addonButtonText.NormalColor = normalColor;
            addonButtonText.HoverColor = hoverColor;
            addonButtonText.PressedColor = pressedColor;
            addonButtonText.MovePixelX = 1f;
            addonButtonText.MovePixelY = -1f;
            toOfficialButton = addonButtonText;

            toAddonButton.OnClick += () => ToAddonSwitchClicked();
            toOfficialButton.OnClick += () => ToOfficialTextClicked();

            addonSwitchTransform.gameObject.SetActive(false);

            if (MainmenuScene.IsAddonTab)
                ToAddonSwitchClicked();
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
        private void ToOfficialTextClicked()
        {
            MainmenuScene.IsAddonTab = false;

            officialMissionCanvas.SetActive(true);
            addonMissionCanvas.SetActive(false);

            officialSwitchTransform.gameObject.SetActive(true);
            addonSwitchTransform.gameObject.SetActive(false);
        }
    }
}
