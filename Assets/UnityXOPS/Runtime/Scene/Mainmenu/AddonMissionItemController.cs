using UnityEngine;
using System.Collections.Generic;

namespace UnityXOPS
{
    /// <summary>
    /// 애드온 미션 목록을 스크롤하며 표시하고 선택된 미션을 로드하는 컨트롤러.
    /// </summary>
    public class AddonMissionItemController : MissionItemController
    {
        [SerializeField]
        private Color32 disabledColor;
        [SerializeField]
        private MissionItemScroll itemScroll;

        private int m_maxItemIndex;
        private int m_topIndex;

        private List<AddonMissionData> m_data;

        private Color32 m_shadowColor;

        /// <summary>
        /// 애드온 미션 데이터를 로드하고 버튼 텍스트·이벤트·스크롤 상태를 초기화한다.
        /// </summary>
        protected override void Start()
        {
            m_data = DataManager.Instance.MissionData.addonMissions;
            m_itemCount = Mathf.Min(m_data.Count, 8); // base.Start() 전에 세팅
            base.Start();

            m_maxItemIndex = m_data.Count - m_itemCount;

            // 텍스트 초기화
            for (int i = 0; i < m_itemCount; i++)
            {
                spriteButtonRoot[i].GetChild(0).GetComponent<XOPSSpriteText>().Text = m_data[i].name;
                spriteButtonRoot[i].GetChild(1).GetComponent<XOPSSpriteTextButton>().Text = m_data[i].name;
            }

            // 이벤트 구독
            upButton.OnClick += () => UpButtonClicked();
            downButton.OnClick += () => DownButtonClicked();
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
                ScrollToIndex(MainmenuScene.AddonScrollIndex);
            }
        }

        /// <summary>
        /// 지정한 인덱스로 스크롤하여 애드온 미션 버튼 텍스트와 상태를 갱신한다.
        /// </summary>
        private void ScrollToIndex(int topIndex)
        {
            topIndex = Mathf.Clamp(topIndex, 0, m_maxItemIndex);
            m_topIndex = topIndex;
            MainmenuScene.AddonScrollIndex = topIndex;

            for (int i = 0; i < m_itemCount; i++)
            {
                spriteButtonRoot[i].GetChild(0).GetComponent<XOPSSpriteText>().Text = m_data[topIndex + i].name;
                spriteButtonRoot[i].GetChild(1).GetComponent<XOPSSpriteTextButton>().Text = m_data[topIndex + i].name;
            }

            SetButtonState(upButton, topIndex > 0);
            SetButtonState(downButton, topIndex < m_maxItemIndex);
            itemScroll.SetScrollPosition(topIndex);
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

        /// <summary>
        /// Up 버튼 클릭 시 스크롤 인덱스를 1 감소한다.
        /// </summary>
        public override void UpButtonClicked()              => ScrollToIndex(m_topIndex - 1);
        /// <summary>
        /// Down 버튼 클릭 시 스크롤 인덱스를 1 증가한다.
        /// </summary>
        public override void DownButtonClicked()            => ScrollToIndex(m_topIndex + 1);
        /// <summary>
        /// 미션 항목 클릭 시 해당 애드온 미션을 로드한다.
        /// </summary>
        public override void MissionItemClicked(int index)  => mainmenuScene.Load(m_topIndex + index, true);
    }
}
