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
        private Texture2D charTexture;
        [SerializeField]
        private string text = "";
        [SerializeField]
        private float charWidth = 32f;
        [SerializeField]
        private float charHeight = 32f;
        [SerializeField]
        private float spacing = 0f;
        [SerializeField]
        private TextAnchor alignment = TextAnchor.LowerLeft;

        public override Texture mainTexture => charTexture != null ? charTexture : base.mainTexture;

        public Texture2D CharTexture
        {
            get => (Texture2D)mainTexture;
            set { charTexture = value; }
        }

        public string Text
        {
            get => text;
            set { text = value; SetVerticesDirty(); }
        }
        public float CharWidth
        {
            get => charWidth;
            set { charWidth = value; SetVerticesDirty(); }
        }
        public float CharHeight
        {
            get => charHeight;
            set { charHeight = value; SetVerticesDirty(); }
        }
        public float Spacing
        {
            get => spacing;
            set { spacing = value; SetVerticesDirty(); }
        }
        public TextAnchor Alignment
        {
            get => alignment;
            set { alignment = value; SetVerticesDirty(); }
        }
        public Color32 FontColor
        {
            get => color;
            set { color = value; SetVerticesDirty(); }
        }

#if UNITY_EDITOR
        // Graphic.OnValidate가 에디터 전용이라 #if로 격리한다(빌드에선 override 대상이 없음).
        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty();
        }
#endif //UNITY_EDITOR

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (charTexture == null || string.IsNullOrEmpty(text))
                return;

            // 전체 텍스트 폭: 마지막 글자 뒤 간격 제외
            float totalWidth = text.Length * charWidth + (text.Length - 1) * spacing;
            float totalHeight = charHeight;

            // 정렬은 pivot 원점이 아니라 rect 경계 기준으로 계산한다. 그래야 alignment가 "rect 안에서의 글자 정렬"이 되어
            // pivot(요소 기준점)과 독립적으로 동작한다(pivot≠alignment여도 위치가 맞음).
            Rect r = rectTransform.rect;
            float ox = alignment switch
            {
                TextAnchor.LowerCenter or TextAnchor.MiddleCenter or TextAnchor.UpperCenter => (r.xMin + r.xMax) * 0.5f - totalWidth * 0.5f,
                TextAnchor.LowerRight or TextAnchor.MiddleRight or TextAnchor.UpperRight => r.xMax - totalWidth,
                _ => r.xMin,
            };
            float oy = alignment switch
            {
                TextAnchor.MiddleLeft or TextAnchor.MiddleCenter or TextAnchor.MiddleRight => (r.yMin + r.yMax) * 0.5f - totalHeight * 0.5f,
                TextAnchor.UpperLeft or TextAnchor.UpperCenter or TextAnchor.UpperRight => r.yMax - totalHeight,
                _ => r.yMin,
            };

            for (int i = 0; i < text.Length; i++)
            {
                int charCode = text[i];
                int col = charCode % 16;
                int row = charCode / 16;

                // char.dds: 16열×16행, 셀 1개 = 1/16 UV
                float u0 = col / 16f;
                float v0 = 1f - (row + 1) / 16f;
                float u1 = u0 + 1f / 16f;
                float v1 = v0 + 1f / 16f;

                float x0 = ox + i * (charWidth + spacing);
                float x1 = x0 + charWidth;
                float y0 = oy;
                float y1 = oy + charHeight;

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
