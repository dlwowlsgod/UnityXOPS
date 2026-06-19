using UnityEngine;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 미션 목록(공식/애드온 공통) 항목 버튼과 Up/Down 버튼을 생성·관리하고 스크롤·선택을 처리하는 제네릭 베이스 컨트롤러.
    /// 파생은 데이터 소스(LoadData)·스크롤 인덱스 상태(ScrollIndexState)·애드온 여부(IsAddon)만 지정한다.
    /// </summary>
    public abstract class MissionItemController<T> : MonoBehaviour where T : IMissionData
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

        [SerializeField]
        protected Color32 disabledColor;
        [SerializeField]
        protected MissionItemScroll itemScroll;

        protected int m_itemCount = 8;

        private int m_maxItemIndex;
        private int m_topIndex;
        private List<T> m_data;
        private Color32 m_shadowColor;

        /// <summary>
        /// 파생이 제공하는 미션 데이터 목록을 반환한다.
        /// </summary>
        protected abstract List<T> LoadData();

        /// <summary>
        /// 씬 재진입 시 유지되는 스크롤 상단 인덱스(공식/애드온별 static 필드에 위임).
        /// </summary>
        protected abstract int ScrollIndexState { get; set; }

        /// <summary>
        /// 애드온 탭이면 true. MainmenuScene.Load 의 mif 인자로 전달된다.
        /// </summary>
        protected abstract bool IsAddon { get; }

        protected virtual void Start()
        {
            m_data = LoadData();
            m_itemCount = Mathf.Min(m_data.Count, 8);
            m_maxItemIndex = m_data.Count - m_itemCount;

            BuildButtons();

            for (int i = 0; i < m_itemCount; i++)
                SetItemText(i, m_data[i].Name);

            upButton.OnClick += UpButtonClicked;
            downButton.OnClick += DownButtonClicked;
            for (int i = 0; i < missionItems.Count; i++)
            {
                int captured = i;
                missionItems[captured].OnClick += () => MissionItemClicked(captured);
            }

            m_shadowColor = upButton.transform.parent.GetChild(0).GetComponent<XOPSSpriteText>().FontColor;

            if (m_data.Count <= m_itemCount)
            {
                // 8개 이하: up/down/스크롤 모두 불필요
                SetButtonState(upButton, false);
                SetButtonState(downButton, false);
                itemScroll.ScrollAreaRawImage.color = new Color(0, 0, 0, 0.5f);
                itemScroll.ScrollbarRawImage.raycastTarget = false;
                itemScroll.ScrollbarRawImage.gameObject.SetActive(false);
            }
            else
            {
                // 처음엔 맨 위이므로 up 비활성화
                SetButtonState(upButton, false);
                itemScroll.Initialize(m_data.Count, m_itemCount, m_maxItemIndex);
                itemScroll.OnScrollIndexChanged += ScrollToIndex;
                ScrollToIndex(ScrollIndexState);
            }
        }

        /// <summary>
        /// Up/Down 버튼과 m_itemCount 개의 미션 아이템 버튼을 세로로 배치 생성한다.
        /// </summary>
        private void BuildButtons()
        {
            missionItems = new List<XOPSSpriteTextButton>();
            spriteButtonRoot = new List<Transform>();

            Vector2 sizeDelta = GetComponent<RectTransform>().sizeDelta;
            int offset = (int)sizeDelta.y / 10;
            Vector2 itemSize = new Vector2(sizeDelta.x, offset);

            upButton = CreateButtonText(transform, "<  UP  >", new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, itemSize, buttonSize
                , buttonNormalColor, buttonHoverColor, buttonPressedColor, buttonShadowColor, TextAnchor.UpperLeft, 0, 1);
            for (int i = 0; i < m_itemCount; i++)
            {
                missionItems.Add(CreateButtonText(transform, $"item_{i}", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -(offset * i + offset)), itemSize, missionItemSize
                , missionItemNormalColor, missionItemHoverColor, missionItemPressedColor, missionItemShadowColor, TextAnchor.UpperLeft, 0, 1));
            }
            downButton = CreateButtonText(transform, "< DOWN >", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -(sizeDelta.y - offset)), itemSize, buttonSize
                , buttonNormalColor, buttonHoverColor, buttonPressedColor, buttonShadowColor, TextAnchor.UpperLeft, 0, 1);

            // up/down 버튼 루트(0번, 마지막)는 아이템과 별도 관리하므로 spriteButtonRoot 에서 제외 → 이후 인덱스가 아이템과 1:1.
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
        /// 지정한 상단 인덱스로 스크롤해 미션 버튼 텍스트와 Up/Down·스크롤바 상태를 갱신한다.
        /// </summary>
        /// <param name="topIndex">목록 상단에 표시할 데이터 인덱스.</param>
        private void ScrollToIndex(int topIndex)
        {
            topIndex = Mathf.Clamp(topIndex, 0, m_maxItemIndex);
            m_topIndex = topIndex;
            ScrollIndexState = topIndex;

            for (int i = 0; i < m_itemCount; i++)
                SetItemText(i, m_data[topIndex + i].Name);

            SetButtonState(upButton, topIndex > 0);
            SetButtonState(downButton, topIndex < m_maxItemIndex);
            itemScroll.SetScrollPosition(topIndex);
        }

        /// <summary>
        /// spriteButtonRoot[slot] 의 그림자/버튼 텍스트를 동일 문자열로 설정한다.
        /// </summary>
        private void SetItemText(int slot, string text)
        {
            spriteButtonRoot[slot].GetChild(0).GetComponent<XOPSSpriteText>().Text = text;
            spriteButtonRoot[slot].GetChild(1).GetComponent<XOPSSpriteTextButton>().Text = text;
        }

        /// <summary>
        /// 버튼 활성화 여부에 따라 게임오브젝트와 그림자 색상을 설정한다.
        /// </summary>
        private void SetButtonState(XOPSSpriteTextButton button, bool active)
        {
            button.gameObject.SetActive(active);
            button.transform.parent.GetChild(0).GetComponent<XOPSSpriteText>().FontColor =
                active ? m_shadowColor : disabledColor;
        }

        /// <summary>Up 버튼 클릭 — 스크롤 인덱스를 1 감소.</summary>
        private void UpButtonClicked() => ScrollToIndex(m_topIndex - 1);
        /// <summary>Down 버튼 클릭 — 스크롤 인덱스를 1 증가.</summary>
        private void DownButtonClicked() => ScrollToIndex(m_topIndex + 1);
        /// <summary>미션 항목 클릭 — 해당 미션을 로드 (IsAddon 으로 공식/애드온 분기).</summary>
        private void MissionItemClicked(int index) => mainmenuScene.Load(m_topIndex + index, IsAddon);
    }
}
