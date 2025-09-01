using UnityEngine;
using TMPro;

namespace UnityXOPS
{
    public class BriefingShadeFont : BriefingTitleFont
    {
        [SerializeField]
        private float spreadTime;
        [SerializeField]
        private Vector2 widthMinMax;
        [SerializeField]
        private Vector2 heightMinMax;
        
        private RectTransform _rect;
        private Vector2 _fixedSizeDelta;

        protected override void Start()
        {
            base.Start();
            _rect = GetComponent<RectTransform>();
            _fixedSizeDelta = _rect.sizeDelta;
        }
        
        protected override void Update()
        {
            base.Update();
            
            var clock = Clock.Instance.Process % spreadTime;
            var ratio = clock / spreadTime;
            var timeLerp = Mathf.InverseLerp(0f, spreadTime, ratio);
            var widthLerp = Mathf.Lerp(widthMinMax.x, widthMinMax.y, ratio);
            var heightLerp = Mathf.Lerp(heightMinMax.x, heightMinMax.y, ratio);

            if (timeLerp == 0f)
            {
                _rect.sizeDelta = _fixedSizeDelta;
                return;
            }
            
            _rect.sizeDelta = new Vector2(_fixedSizeDelta.x * widthLerp, _fixedSizeDelta.y * heightLerp);
        }
    }
}