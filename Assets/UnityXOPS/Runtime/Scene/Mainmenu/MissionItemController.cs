using UnityEngine;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 미션 목록 항목 버튼과 Up/Down 버튼을 생성·관리하는 추상 베이스 컨트롤러.
    /// </summary>
    public abstract class MissionItemController : MonoBehaviour
    {
        [SerializeField]
        protected MainmenuScene mainmenuScene;
        [SerializeField]
        protected XOPSSpriteTextButton upButton, downButton;
        [SerializeField]
        protected List<XOPSSpriteTextButton> missionItems;
        [SerializeField]
        protected List<Transform> spriteButtonRoot;

        [SerializeField]
        protected Color32 missionItemNormalColor, missionItemHoverColor, missionItemPressedColor, missionItemShadowColor,
            buttonNormalColor, buttonHoverColor, buttonPressedColor, buttonShadowColor;
        [SerializeField]
        protected Vector2 missionItemSize, buttonSize;

        protected int m_itemCount = 8;

        /// <summary>
        /// Up/Down 버튼과 미션 아이템 버튼 텍스트를 생성하고 레이아웃을 초기화한다.
        /// </summary>
        protected virtual void Start()
        {
            missionItems = new List<XOPSSpriteTextButton>();
            spriteButtonRoot = new List<Transform>();
            Vector2 sizeDelta = GetComponent<RectTransform>().sizeDelta;

            int offset = (int)GetComponent<RectTransform>().sizeDelta.y / 10;
            Vector2 itemSize = new Vector2(sizeDelta.x, offset);

            //up
            upButton = CreateButtonText(transform, "<  UP  >", new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, itemSize, buttonSize
                ,buttonNormalColor, buttonHoverColor, buttonPressedColor, buttonShadowColor, TextAnchor.UpperLeft, 0, 1);
            //items
            for (int i = 0; i < m_itemCount; i++)
            {
                missionItems.Add(CreateButtonText(transform, $"item_{i}", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -(offset * i + offset)), itemSize, missionItemSize
                , missionItemNormalColor, missionItemHoverColor, missionItemPressedColor, missionItemShadowColor, TextAnchor.UpperLeft, 0, 1));
            }
            //down
            downButton = CreateButtonText(transform, "< DOWN >", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -(sizeDelta.y - offset)), itemSize, buttonSize
                , buttonNormalColor, buttonHoverColor, buttonPressedColor, buttonShadowColor, TextAnchor.UpperLeft, 0, 1);

            spriteButtonRoot.RemoveAt(m_itemCount + 1);
            spriteButtonRoot.RemoveAt(0);
        }

        /// <summary>
        /// 그림자와 버튼 텍스트로 구성된 미션 버튼 항목을 생성하고 반환한다.
        /// </summary>
        private XOPSSpriteTextButton CreateButtonText(
            Transform root, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 fontSize,
            Color32 normalColor, Color32 hoverColor, Color32 pressedColor, Color32 shadowColor, TextAnchor alignment, float spacing, float moveOffset)
        {
            var parentObj = new GameObject($"{text}_Root", typeof(RectTransform));
            parentObj.transform.SetParent(root, false);
            spriteButtonRoot.Add(parentObj.transform);
            RectTransform rect = parentObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = Vector2.zero;
            rect.pivot = Vector2.zero;

            //shadow
            var shadowText = FontManager.CreateSpriteText<XOPSSpriteText>(
                parentObj.transform, text, anchorMin, anchorMax, new Vector2(position.x + 1, position.y - 1), size, fontSize,
                shadowColor, alignment, spacing);
            shadowText.name = $"{text}_Shadow";

            //text
            var buttonText = FontManager.CreateSpriteText<XOPSSpriteTextButton>(
                parentObj.transform, text, anchorMin, anchorMax, position, size, fontSize, normalColor, alignment, spacing);
            buttonText.name = $"{text}_Button";
            buttonText.FontColor = buttonText.NormalColor = normalColor;
            buttonText.HoverColor = hoverColor;
            buttonText.PressedColor = pressedColor;
            buttonText.MovePixelX = moveOffset;
            buttonText.MovePixelY = -moveOffset;

            return buttonText;
        }

        /// <summary>
        /// Up 버튼 클릭 시 호출된다.
        /// </summary>
        public abstract void UpButtonClicked();
        /// <summary>
        /// Down 버튼 클릭 시 호출된다.
        /// </summary>
        public abstract void DownButtonClicked();
        /// <summary>
        /// 미션 항목 클릭 시 호출된다.
        /// </summary>
        /// <param name="index">클릭된 항목의 표시 인덱스.</param>
        public abstract void MissionItemClicked(int index);
    }
}
