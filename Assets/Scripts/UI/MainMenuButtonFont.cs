using UnityEngine;

namespace UnityXOPS
{
    public class MainMenuButtonFont : GameFont
    {
        [SerializeField] 
        private Color hoverColor;
        [SerializeField] 
        private Color disableColor;

        private RectTransform _rect;
        private Vector2 _fixedPosition;

        protected override void Start()
        {
            base.Start();
            _rect = Text.gameObject.GetComponent<RectTransform>();
            _fixedPosition = _rect.anchoredPosition;
        }
        private bool _isEnable = true;
        public bool IsEnable
        {
            get => _isEnable;
            set
            {
                _isEnable = value;
                if (!_isEnable)
                {
                    IsHover = false;
                    IsDown = false;
                }
            }
        }

        private bool IsHover { get; set; }
        private bool IsDown { get; set; }

        private void Update()
        {
            if (IsEnable)
            {
                if (IsHover)
                {
                    Text.color = hoverColor;
                    _rect.anchoredPosition = IsDown ? _fixedPosition + new Vector2(2, -2) : _fixedPosition;
                }
                else
                {
                    Text.color = fontColor;
                    _rect.anchoredPosition = _fixedPosition;
                }
            }
            else
            {
                Text.color = disableColor;
                _rect.anchoredPosition = _fixedPosition;
            }
            
            Text.text = fontText;
            Text.text = CharacterToRichText(Text);
            ShadowText.text = fontText;
            ShadowText.text = CharacterToRichText(ShadowText);

        }

        /// <summary>
        /// 텍스트를 업데이트합니다.
        /// </summary>
        /// <param name="text">변경할 텍스트</param>
        public void UpdateText(string text)
        {
            fontText = text;
        }

        public void MouseEnter()
        {
            if (IsEnable)
            {
                IsHover = true;
            }
        }

        public void MouseExit()
        {
            if (IsEnable)
            {
                IsHover = false;
            }
        }

        public void MouseDown()
        {
            if (IsEnable)
            {
                IsDown = true;
            }
        }

        public void MouseUp()
        {
            if (IsEnable)
            {
                IsDown = false;
            }
        }
    }
}