using UnityEngine;
using UnityEngine.UI;

namespace UnityXOPS
{
    /// <summary>
    /// 씬의 페이드-인을 구현하는 클래스입니다.
    /// </summary>
    public class FadeIn : MonoBehaviour
    {
        [SerializeField] 
        private float fadeInStart;
        [SerializeField]
        private float fadeInEnd;

        private RawImage _image;
        private Color _transparentColor;
        private Color _visibleColor;

        private void Start()
        {
            _image = GetComponent<RawImage>();
            _transparentColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            _visibleColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            _image.color = _visibleColor;
        }

        private void Update()
        {
            var clock = Clock.Instance.Process;
            if (clock < fadeInStart)
            {
                _image.color = _visibleColor;
            }

            if (clock >= fadeInStart && clock < fadeInEnd)
            {
                var lerp = Mathf.InverseLerp(fadeInStart, fadeInEnd, clock);
                _image.color = Color.Lerp(_visibleColor, _transparentColor, lerp);
            }

            if (clock >= fadeInEnd)
            {
                _image.color = _transparentColor;
            }
        }
    }
}