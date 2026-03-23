using UnityEngine;
using UnityEngine.UI;

namespace UnityXOPS
{
    /// <summary>
    /// char.dds 스프라이트 폰트 텍스처를 사용해 UI 텍스트를 렌더링하는 커스텀 그래픽 컴포넌트.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class XOPSSpriteText : MaskableGraphic
    {
        [SerializeField] 
        private Texture2D _charTexture;
        [SerializeField] 
        private string _text = "";
        [SerializeField] 
        private float _charWidth = 32f;
        [SerializeField] 
        private float _charHeight = 32f;
        [SerializeField] 
        private float _spacing = 0f;
        [SerializeField] 
        private TextAnchor _alignment = TextAnchor.LowerLeft;

        public override Texture mainTexture => _charTexture != null ? _charTexture : base.mainTexture;

        public Texture2D CharTexture
        {
            get => (Texture2D)mainTexture;
            set { _charTexture = value; }
        }

        public string Text
        {
            get => _text;
            set { _text = value; SetVerticesDirty(); }
        }
        public float CharWidth
        {
            get => _charWidth;
            set { _charWidth = value; SetVerticesDirty(); }
        }
        public float CharHeight
        {
            get => _charHeight;
            set { _charHeight = value; SetVerticesDirty(); }
        }
        public float Spacing 
        {
            get => _spacing;
            set { _spacing = value; SetVerticesDirty(); }
        }
        public TextAnchor Alignment
        {
            get => _alignment;
            set { _alignment = value; SetVerticesDirty(); }
        }
        public Color32 FontColor
        {
            get => color;
            set { color = value; SetVerticesDirty(); }    
        }

        /// <summary>
        /// 에디터에서 프로퍼티 변경 시 메시를 재생성한다.
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty();
        }

        /// <summary>
        /// 문자별 UV를 계산해 쿼드 버텍스와 삼각형을 생성한다.
        /// </summary>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (_charTexture == null || string.IsNullOrEmpty(_text))
                return;

            // 전체 텍스트 폭: 마지막 글자 뒤 간격 제외
            float totalWidth  = _text.Length * _charWidth + (_text.Length - 1) * _spacing;
            float totalHeight = _charHeight;

            // 정렬 기준으로 원점 오프셋 계산
            float ox = _alignment switch
            {
                TextAnchor.LowerCenter or TextAnchor.MiddleCenter or TextAnchor.UpperCenter => -totalWidth  * 0.5f,
                TextAnchor.LowerRight  or TextAnchor.MiddleRight  or TextAnchor.UpperRight  => -totalWidth,
                _ => 0f,
            };
            float oy = _alignment switch
            {
                TextAnchor.MiddleLeft or TextAnchor.MiddleCenter or TextAnchor.MiddleRight => -totalHeight * 0.5f,
                TextAnchor.UpperLeft  or TextAnchor.UpperCenter  or TextAnchor.UpperRight  => -totalHeight,
                _ => 0f,
            };

            for (int i = 0; i < _text.Length; i++)
            {
                int charCode = _text[i];
                int col = charCode % 16;
                int row = charCode / 16;

                // char.dds: 16열×16행, 셀 1개 = 1/16 UV
                float u0 = col / 16f;
                float v0 = 1f - (row + 1) / 16f;
                float u1 = u0 + 1f / 16f;
                float v1 = v0 + 1f / 16f;

                float x0 = ox + i * (_charWidth + _spacing);
                float x1 = x0 + _charWidth;
                float y0 = oy;
                float y1 = oy + _charHeight;

                int vertIndex = i * 4;
                vh.AddVert(new Vector3(x0, y0), color, new Vector2(u0, v0));
                vh.AddVert(new Vector3(x0, y1), color, new Vector2(u0, v1));
                vh.AddVert(new Vector3(x1, y1), color, new Vector2(u1, v1));
                vh.AddVert(new Vector3(x1, y0), color, new Vector2(u1, v0));

                vh.AddTriangle(vertIndex, vertIndex + 1, vertIndex + 2);
                vh.AddTriangle(vertIndex, vertIndex + 2, vertIndex + 3);
            }
        }
    }
}
