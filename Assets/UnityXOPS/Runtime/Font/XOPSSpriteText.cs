using UnityEngine;
using UnityEngine.UI;

namespace UnityXOPS
{
    /// <summary>
    /// 스프라이트 시트 폰트를 사용해 UI 텍스트를 렌더링하는 커스텀 그래픽 컴포넌트.
    /// 시트 한 장(페이지)만 쓰며, 글자 코드가 곧 그 시트 안의 칸 번호(0~255)가 된다.
    /// 지정한 페이지가 등록돼 있지 않으면 0번 페이지로 떨어진다.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class XOPSSpriteText : MaskableGraphic
    {
        // 시트 한 장 = 16열x16행 = 칸 256개
        private const int k_cellsPerRow = 16;
        private const int k_cellsPerPage = k_cellsPerRow * k_cellsPerRow;

        [SerializeField]
        private int page = 0;
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

        public override Texture mainTexture => PageTexture() ?? base.mainTexture;

        public int Page
        {
            get => page;
            set { page = value; SetMaterialDirty(); SetVerticesDirty(); }
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
            SetMaterialDirty();
            SetVerticesDirty();
        }
#endif //UNITY_EDITOR

        /// <summary>
        /// 이 컴포넌트가 그릴 시트 텍스처를 가져온다. 지정 페이지가 없으면 0번 페이지로 대체한다.
        /// </summary>
        /// <returns>시트 텍스처. 0번 페이지마저 등록돼 있지 않으면 null.</returns>
        private Texture2D PageTexture()
        {
            return FontManager.GetSpritePage(page) ?? FontManager.GetSpritePage(0);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Texture2D pageTexture = PageTexture();
            if (pageTexture == null || string.IsNullOrEmpty(text))
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
                // 시트 한 장이 칸 256개라 그 밖의 글자는 표현할 수 없다. 빈 칸으로 약속된 0번 칸으로 그린다.
                int charCode = text[i];
                int cell = charCode < k_cellsPerPage ? charCode : 0;
                int col = cell % k_cellsPerRow;
                int row = cell / k_cellsPerRow;

                // 셀 1개 = 1/16 UV. DirectX와 달리 Unity는 V=0이 아래라 행을 뒤집는다.
                float u0 = col / (float)k_cellsPerRow;
                float v0 = 1f - (row + 1) / (float)k_cellsPerRow;
                float u1 = u0 + 1f / k_cellsPerRow;
                float v1 = v0 + 1f / k_cellsPerRow;

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
