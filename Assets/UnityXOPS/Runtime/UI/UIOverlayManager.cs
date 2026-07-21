using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using JJLUtility;
using JJLUtility.IO;
using UnityXOPS.Modding;
using System.Collections.Generic;
using System.Linq;

namespace UnityXOPS
{
    /// <summary>
    /// 레터박스와 화면 페이드, 그리고 모드(Lua)가 만든 UI 레이어를 관리하는 전역 오버레이 싱글톤.
    /// 모든 레이어 order는 Lua가 지정한다(하드코딩 없음). 씬 전환에도 매니저는 유지되며,
    /// 페이드/레터박스는 영구 전용 캔버스, 모드가 만든 동적 레이어는 씬 언로드마다 자동 정리된다.
    /// </summary>
    public class UIOverlayManager : SingletonBehavior<UIOverlayManager>
    {
        private readonly Dictionary<int, Transform> m_layers = new Dictionary<int, Transform>();
        // 레이어별 서브 루트(뷰포트 영역에 앵커됨). CreateImage/Text는 이 루트 아래에 붙는다.
        private readonly Dictionary<int, RectTransform> m_layerRoots = new Dictionary<int, RectTransform>();
        private Rect m_appliedViewport = new Rect(0f, 0f, 1f, 1f);

        private Canvas m_letterboxCanvas;
        private RectTransform m_topBar;
        private RectTransform m_bottomBar;

        private const float k_referenceWidth = 640f;
        private const float k_referenceHeight = 480f;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }

            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDestroy() => SceneManager.sceneUnloaded -= OnSceneUnloaded;

        /// <summary>
        /// 씬이 언로드될 때 호출된다. 모드가 만든 동적 레이어(고아 GameObject)만 정리한다.
        /// 레터박스 상태는 의도적으로 건드리지 않는다 — 씬을 넘어 유지돼야 하는 화면비 설정이기 때문이다.
        /// 각 씬은 start()에서 자신의 오버레이 상태를 명시적으로 선언한다.
        /// 신 씬의 LuaSceneController.Start() 보다 먼저 실행되므로 새 UI를 지우지 않는다.
        /// </summary>
        /// <param name="scene">언로드된 씬</param>
        private void OnSceneUnloaded(Scene scene)
        {
            ClearDynamicLayers();
        }

        /// <summary>
        /// 모드/씬이 만든 동적 레이어(CreateImage/CreateText)를 모두 파괴한다.
        /// 레터박스는 별도 전용 캔버스라 영향받지 않는다.
        /// </summary>
        public void ClearDynamicLayers()
        {
            foreach (Transform layer in m_layers.Values)
            {
                if (layer != null)
                {
                    Destroy(layer.gameObject);   // 캔버스 파괴 시 하위 서브 루트도 함께 제거됨
                }
            }
            m_layers.Clear();
            m_layerRoots.Clear();
        }

        /// <summary>
        /// 레터박스/페이드가 쓰는 전용 ScreenSpaceOverlay 캔버스를 매니저 하위에 생성한다.
        /// </summary>
        /// <param name="name">캔버스 GameObject 이름</param>
        /// <returns>생성된 Canvas</returns>
        private Canvas CreateOverlayCanvas(string name)
        {
            GameObject go = new GameObject(name, typeof(Canvas));
            go.transform.SetParent(transform, false);
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            return canvas;
        }

        /// <summary>
        /// 레터박스 막대 2개와 전용 캔버스를 아직 없으면 생성한다(lazy).
        /// </summary>
        private void EnsureLetterbox()
        {
            if (m_topBar != null)
            {
                return;
            }

            m_letterboxCanvas = CreateOverlayCanvas("LetterboxOverlay");
            m_topBar = CreateBar(m_letterboxCanvas.transform, "LetterboxTop");
            m_bottomBar = CreateBar(m_letterboxCanvas.transform, "LetterboxBottom");
            ApplyLetterbox(false, 0f);
        }

        /// <summary>
        /// 지정 sortingOrder의 오버레이 레이어(독립 ScreenSpaceOverlay Canvas)를 가져오거나 생성한다.
        /// sortingOrder가 높을수록 위에 그려진다. UI 요소를 반환된 Transform 아래에 붙인다.
        /// scaling이 true면 640x480 기준 CanvasScaler(높이 매치)로 비율이 바뀌어도 깨지지 않게 하고,
        /// false면 Constant Pixel Size(1:1) CanvasScaler를 붙인다(어느 쪽이든 scaleFactor 조절 가능).
        /// 한 sortingOrder의 scaling 여부는 최초 생성 시 결정되며 이후 호출에서는 무시된다.
        /// </summary>
        /// <param name="sortingOrder">레이어 우선순위(정수)</param>
        /// <param name="scaling">true면 640x480 기준 CanvasScaler(Height 매치)를 적용한다.</param>
        /// <returns>해당 레이어 Canvas의 Transform</returns>
        public Transform GetOrCreateLayer(int sortingOrder, bool scaling = false)
        {
            if (m_layerRoots.TryGetValue(sortingOrder, out RectTransform existingRoot))
            {
                return existingRoot;
            }

            GameObject go = new GameObject($"OverlayLayer_{sortingOrder}", typeof(Canvas));
            go.transform.SetParent(transform, false);
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            // scaling 여부와 무관하게 CanvasScaler를 항상 붙인다. scaling=false면 Constant Pixel Size(1:1)로 둬
            // scaleFactor 조절 지점(SetLayerScaleFactor)을 항상 확보한다.
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            if (scaling)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(k_referenceWidth, k_referenceHeight);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f;
            }
            else
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1f;
            }

            // 게임 뷰포트(레터박스 후 영역) 안에만 UI가 들어가도록 앵커된 서브 루트. 콘텐츠는 이 루트 아래에 붙는다.
            // 앵커가 뷰포트 rect(정규화)를 따르므로, 4:3 뷰포트에선 640x480 코너 레이아웃이 그 영역에 정확히 들어가고
            // 16:9 뷰포트(꽉 채움)에선 화면 전체로 퍼진다. 뷰포트가 바뀌면 LateUpdate가 앵커를 갱신한다.
            RectTransform root = new GameObject("ViewportRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(go.transform, false);
            ApplyViewportAnchors(root, CurrentViewport());

            m_layers[sortingOrder] = go.transform;
            m_layerRoots[sortingOrder] = root;
            return root;
        }

        private void LateUpdate()
        {
            Rect vp = CurrentViewport();
            if (vp == m_appliedViewport)
            {
                return;
            }
            m_appliedViewport = vp;
            foreach (RectTransform root in m_layerRoots.Values)
            {
                if (root != null)
                {
                    ApplyViewportAnchors(root, vp);
                }
            }
        }

        /// <summary>
        /// 지정 레이어가 콘텐츠를 배치하는 영역의 크기를 그 레이어의 좌표 단위로 반환한다.
        /// scaling=true 레이어는 높이가 항상 480이고 폭이 화면비를 따라 늘어나므로, 화면비에 맞춰
        /// 콘텐츠를 맞추려면(예: 스코프를 4:3으로 내접) 이 크기를 읽어 계산한다. 레이어가 없으면 생성한다.
        /// </summary>
        /// <param name="sortingOrder">대상 레이어 우선순위</param>
        /// <param name="scaling">레이어 생성 시 scaling 값(이미 있으면 무시)</param>
        /// <returns>레이어 영역 크기(레이어 좌표 단위). 레이아웃 전이면 0이 될 수 있다.</returns>
        public Vector2 GetLayerSize(int sortingOrder, bool scaling)
        {
            GetOrCreateLayer(sortingOrder, scaling);
            RectTransform root = m_layerRoots[sortingOrder];
            return root != null ? root.rect.size : Vector2.zero;
        }

        /// <summary>
        /// 현재 게임 뷰포트(레터박스/필러박스 후 정규화 rect)를 반환한다. 레터박스 컨트롤러가 없으면 전체 화면.
        /// </summary>
        /// <returns>정규화 뷰포트 rect(0~1).</returns>
        private static Rect CurrentViewport()
        {
            return LetterboxController.Loaded ? LetterboxController.Instance.Viewport : new Rect(0f, 0f, 1f, 1f);
        }

        /// <summary>
        /// 레이어 서브 루트의 앵커를 뷰포트 rect에 맞춰, 콘텐츠가 그 영역에만 놓이게 한다.
        /// </summary>
        /// <param name="root">레이어 서브 루트 RectTransform.</param>
        /// <param name="vp">정규화 뷰포트 rect(0~1).</param>
        private static void ApplyViewportAnchors(RectTransform root, Rect vp)
        {
            root.anchorMin = new Vector2(vp.xMin, vp.yMin);
            root.anchorMax = new Vector2(vp.xMax, vp.yMax);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 지정 레이어의 CanvasScaler scaleFactor를 설정한다(UI 전체 확대/축소). 레이어가 없으면 생성한다.
        /// Constant Pixel Size(scaling=false) 레이어에서 의미가 있으며, ScaleWithScreenSize(scaling=true) 레이어에서는 무시된다.
        /// </summary>
        /// <param name="sortingOrder">대상 레이어 우선순위</param>
        /// <param name="factor">scaleFactor(1=기본, 2=2배 확대). 0 이하는 0.01로 보정</param>
        public void SetLayerScaleFactor(int sortingOrder, float factor)
        {
            GetOrCreateLayer(sortingOrder);
            CanvasScaler scaler = m_layers[sortingOrder].GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.scaleFactor = Mathf.Max(0.01f, factor);
            }
        }

        /// <summary>
        /// 지정 레이어의 현재 CanvasScaler scaleFactor를 반환한다. 레이어가 없으면 생성한다.
        /// </summary>
        /// <param name="sortingOrder">대상 레이어 우선순위</param>
        /// <returns>현재 scaleFactor. CanvasScaler가 없으면 1</returns>
        public float GetLayerScaleFactor(int sortingOrder)
        {
            GetOrCreateLayer(sortingOrder);
            CanvasScaler scaler = m_layers[sortingOrder].GetComponent<CanvasScaler>();
            return scaler != null ? scaler.scaleFactor : 1f;
        }

        /// <summary>
        /// 피벗 이름 문자열을 UIPivot으로 변환한다. 대소문자와 구분자(_, -, 공백)를 무시한다.
        /// 알 수 없는 값이면 경고 후 Center를 반환한다.
        /// </summary>
        /// <param name="pivot">"TopRight", "top_right", "bottom" 등의 피벗 이름</param>
        /// <returns>대응하는 UIPivot(미일치 시 Center)</returns>
        public static UIPivot ParsePivot(string pivot)
        {
            if (string.IsNullOrEmpty(pivot))
            {
                return UIPivot.Center;
            }

            string key = new string(pivot.ToLowerInvariant().Where(char.IsLetter).ToArray());
            switch (key)
            {
                case "topleft": return UIPivot.TopLeft;
                case "topcenter":
                case "topmiddle":
                case "top": return UIPivot.TopCenter;
                case "topright": return UIPivot.TopRight;
                case "middleleft":
                case "centerleft":
                case "left": return UIPivot.MiddleLeft;
                case "center":
                case "middle":
                case "middlecenter":
                case "centercenter": return UIPivot.Center;
                case "middleright":
                case "centerright":
                case "right": return UIPivot.MiddleRight;
                case "bottomleft": return UIPivot.BottomLeft;
                case "bottomcenter":
                case "bottommiddle":
                case "bottom": return UIPivot.BottomCenter;
                case "bottomright": return UIPivot.BottomRight;
                case "stretchtop": return UIPivot.StretchTop;
                case "stretchmiddle": return UIPivot.StretchMiddle;
                case "stretchbottom": return UIPivot.StretchBottom;
                case "stretchleft": return UIPivot.StretchLeft;
                case "stretchcenter": return UIPivot.StretchCenter;
                case "stretchright": return UIPivot.StretchRight;
                case "stretchfull":
                case "stretch":
                case "full": return UIPivot.StretchFull;
                default:
                    Debugger.LogWarning($"알 수 없는 피벗 '{pivot}', Center로 대체합니다.", Instance, "UIOverlayManager");
                    return UIPivot.Center;
            }
        }

        /// <summary>
        /// UIPivot 9지점을 0~1 정규화 앵커/피벗 좌표로 변환한다.
        /// </summary>
        /// <param name="pivot">기준 지점</param>
        /// <returns>anchorMin/anchorMax/pivot에 함께 사용할 정규화 좌표</returns>
        public static Vector2 PivotToAnchor(UIPivot pivot)
        {
            float px = pivot switch
            {
                UIPivot.TopLeft or UIPivot.MiddleLeft or UIPivot.BottomLeft => 0f,
                UIPivot.TopRight or UIPivot.MiddleRight or UIPivot.BottomRight => 1f,
                _ => 0.5f,
            };
            float py = pivot switch
            {
                UIPivot.TopLeft or UIPivot.TopCenter or UIPivot.TopRight => 1f,
                UIPivot.BottomLeft or UIPivot.BottomCenter or UIPivot.BottomRight => 0f,
                _ => 0.5f,
            };
            return new Vector2(px, py);
        }

        /// <summary>
        /// UIPivot을 RectTransform 앵커 구성(anchorMin/anchorMax/pivot)으로 변환한다.
        /// 9지점은 한 점(anchorMin=anchorMax=pivot), stretch는 펼쳐지는 축에서 anchorMin≠anchorMax가 된다.
        /// 펼쳐지는 축의 크기는 sizeDelta로 정해진다(0이면 부모를 꽉 채움).
        /// </summary>
        /// <param name="pivot">UIPivot 값</param>
        /// <param name="anchorMin">출력: anchorMin</param>
        /// <param name="anchorMax">출력: anchorMax</param>
        /// <param name="pivotPoint">출력: pivot</param>
        public static void GetAnchorConfig(UIPivot pivot, out Vector2 anchorMin, out Vector2 anchorMax, out Vector2 pivotPoint)
        {
            switch (pivot)
            {
                case UIPivot.StretchTop:    anchorMin = new Vector2(0f, 1f); anchorMax = new Vector2(1f, 1f); pivotPoint = new Vector2(0.5f, 1f); return;
                case UIPivot.StretchMiddle: anchorMin = new Vector2(0f, 0.5f); anchorMax = new Vector2(1f, 0.5f); pivotPoint = new Vector2(0.5f, 0.5f); return;
                case UIPivot.StretchBottom: anchorMin = new Vector2(0f, 0f); anchorMax = new Vector2(1f, 0f); pivotPoint = new Vector2(0.5f, 0f); return;
                case UIPivot.StretchLeft:   anchorMin = new Vector2(0f, 0f); anchorMax = new Vector2(0f, 1f); pivotPoint = new Vector2(0f, 0.5f); return;
                case UIPivot.StretchCenter: anchorMin = new Vector2(0.5f, 0f); anchorMax = new Vector2(0.5f, 1f); pivotPoint = new Vector2(0.5f, 0.5f); return;
                case UIPivot.StretchRight:  anchorMin = new Vector2(1f, 0f); anchorMax = new Vector2(1f, 1f); pivotPoint = new Vector2(1f, 0.5f); return;
                case UIPivot.StretchFull:   anchorMin = new Vector2(0f, 0f); anchorMax = new Vector2(1f, 1f); pivotPoint = new Vector2(0.5f, 0.5f); return;
                default:
                    Vector2 p = PivotToAnchor(pivot);
                    anchorMin = p;
                    anchorMax = p;
                    pivotPoint = p;
                    return;
            }
        }

        /// <summary>
        /// UIPivot을 텍스트 정렬용 TextAnchor로 변환한다. stretch 값은 정렬 의미가 없어 가까운 9지점으로 폴백한다.
        /// </summary>
        /// <param name="pivot">기준 지점</param>
        /// <returns>대응하는 TextAnchor</returns>
        public static TextAnchor PivotToTextAnchor(UIPivot pivot)
        {
            return pivot switch
            {
                UIPivot.TopLeft => TextAnchor.UpperLeft,
                UIPivot.TopCenter => TextAnchor.UpperCenter,
                UIPivot.TopRight => TextAnchor.UpperRight,
                UIPivot.MiddleLeft => TextAnchor.MiddleLeft,
                UIPivot.MiddleRight => TextAnchor.MiddleRight,
                UIPivot.BottomLeft => TextAnchor.LowerLeft,
                UIPivot.BottomCenter => TextAnchor.LowerCenter,
                UIPivot.BottomRight => TextAnchor.LowerRight,
                UIPivot.StretchTop => TextAnchor.UpperCenter,
                UIPivot.StretchBottom => TextAnchor.LowerCenter,
                UIPivot.StretchLeft => TextAnchor.MiddleLeft,
                UIPivot.StretchRight => TextAnchor.MiddleRight,
                _ => TextAnchor.MiddleCenter,
            };
        }

        /// <summary>
        /// 검정 레터박스 막대 하나를 생성해 RectTransform을 반환한다.
        /// </summary>
        /// <param name="parent">막대를 붙일 레이어 Transform</param>
        /// <param name="name">생성할 GameObject 이름</param>
        /// <returns>생성된 막대의 RectTransform</returns>
        private RectTransform CreateBar(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RawImage));
            go.transform.SetParent(parent, false);
            RawImage image = go.GetComponent<RawImage>();
            image.texture = Texture2D.whiteTexture;
            image.color = Color.black;
            image.raycastTarget = false;
            return image.rectTransform;
        }

        /// <summary>
        /// 레터박스를 지정 레이어(order)에 표시/숨기고 비율을 설정한다. 앵커 기반이라 해상도에 독립적이다.
        /// </summary>
        /// <param name="order">레터박스 캔버스 sortingOrder(높을수록 위)</param>
        /// <param name="active">true면 막대를 표시한다.</param>
        /// <param name="ratio">상/하단 각각이 차지할 화면 비율(0~0.5). 0.1이면 위아래 10%씩.</param>
        public void SetLetterbox(int order, bool active, float ratio)
        {
            EnsureLetterbox();
            m_letterboxCanvas.sortingOrder = order;
            ApplyLetterbox(active, ratio);
        }

        /// <summary>
        /// 레터박스 막대의 앵커/표시 상태를 설정한다(레이어는 건드리지 않음).
        /// </summary>
        /// <param name="active">true면 막대를 표시한다.</param>
        /// <param name="ratio">상/하단 각각이 차지할 화면 비율(0~0.5).</param>
        private void ApplyLetterbox(bool active, float ratio)
        {
            float r = active ? Mathf.Clamp(ratio, 0f, 0.5f) : 0f;

            m_topBar.anchorMin = new Vector2(0f, 1f - r);
            m_topBar.anchorMax = new Vector2(1f, 1f);
            m_topBar.offsetMin = Vector2.zero;
            m_topBar.offsetMax = Vector2.zero;

            m_bottomBar.anchorMin = new Vector2(0f, 0f);
            m_bottomBar.anchorMax = new Vector2(1f, r);
            m_bottomBar.offsetMin = Vector2.zero;
            m_bottomBar.offsetMax = Vector2.zero;

            bool show = r > 0f;
            m_topBar.gameObject.SetActive(show);
            m_bottomBar.gameObject.SetActive(show);
        }

        /// <summary>
        /// 레터박스를 끈다. 오버레이를 한 번에 초기화하는 편의 메서드.
        /// 아직 생성되지 않은 오버레이는 건드리지 않는다(레이어는 유지).
        /// </summary>
        public void ClearOverlay()
        {
            if (m_topBar != null)
            {
                ApplyLetterbox(false, 0f);
            }
        }

        /// <summary>
        /// 지정 레이어에 RawImage 기반 UI 요소를 생성하고 핸들을 반환한다.
        /// texturePath가 비면 색만 적용되는 패널, 있으면 해당 이미지를 표시한다.
        /// pivot 지점을 기준으로 앵커/피벗이 정해지며, x/y는 그 지점으로부터의 오프셋(+x 오른쪽, +y 위쪽)이다.
        /// </summary>
        /// <param name="layer">레이어 우선순위(sortingOrder). 없으면 lazy 생성</param>
        /// <param name="scaling">true면 640x480 기준 스케일 레이어에 배치한다(최초 생성 시 결정).</param>
        /// <param name="pivot">앵커/피벗 기준 지점(9지점)</param>
        /// <param name="texturePath">streamingAssets 기준 이미지 경로(빈 문자열이면 패널)</param>
        /// <param name="x">기준 지점으로부터의 X 오프셋(오른쪽 +)</param>
        /// <param name="y">기준 지점으로부터의 Y 오프셋(위쪽 +)</param>
        /// <param name="width">너비</param>
        /// <param name="height">높이</param>
        /// <param name="r">빨강(0~1)</param>
        /// <param name="g">초록(0~1)</param>
        /// <param name="b">파랑(0~1)</param>
        /// <param name="a">알파(0~1)</param>
        /// <returns>생성된 요소를 제어하는 핸들</returns>
        public UIElementHandle CreateImage(int layer, bool scaling, UIPivot pivot, string texturePath, float x, float y, float width, float height, float r, float g, float b, float a)
        {
            return CreateImageUnder(GetOrCreateLayer(layer, scaling), pivot, texturePath, x, y, width, height, r, g, b, a);
        }

        /// <summary>
        /// 지정한 부모 Transform 아래에 RawImage 요소를 생성한다. 레이어 직속(CreateImage) 또는
        /// 다른 요소의 자식(UIElementHandle.CreateChildImage)으로 붙일 때 공용으로 쓴다.
        /// 앵커/피벗/스트레치는 부모 rect 기준으로 계산되므로, 스트레치 자식은 부모 크기를 따라간다.
        /// </summary>
        /// <param name="parent">부모 Transform(레이어 캔버스 또는 다른 요소).</param>
        /// <param name="pivot">앵커/피벗 기준 지점(9지점/스트레치).</param>
        /// <param name="texturePath">streamingAssets 기준 이미지 경로(빈 문자열이면 패널).</param>
        /// <param name="x">기준 지점으로부터의 X 오프셋(스트레치 축이면 sizeDelta 보정).</param>
        /// <param name="y">기준 지점으로부터의 Y 오프셋(스트레치 축이면 sizeDelta 보정).</param>
        /// <param name="width">너비(스트레치 축이면 부모 대비 증감량).</param>
        /// <param name="height">높이(스트레치 축이면 부모 대비 증감량).</param>
        /// <param name="r">빨강(0~1)</param>
        /// <param name="g">초록(0~1)</param>
        /// <param name="b">파랑(0~1)</param>
        /// <param name="a">알파(0~1)</param>
        /// <returns>생성된 요소를 제어하는 핸들</returns>
        public UIElementHandle CreateImageUnder(Transform parent, UIPivot pivot, string texturePath, float x, float y, float width, float height, float r, float g, float b, float a)
        {
            return CreateImageFromTexture(parent, pivot, LoadTexture(texturePath), x, y, width, height, r, g, b, a);
        }

        /// <summary>
        /// 지정 레이어에 이미 만들어진 텍스처를 그대로 붙인 이미지 요소를 생성한다.
        /// 파일에서 읽는 대신 런타임에 그려지는 텍스처(무기 뷰포트 등)를 화면에 띄울 때 쓴다.
        /// </summary>
        /// <param name="layer">레이어 우선순위(sortingOrder). 없으면 lazy 생성</param>
        /// <param name="scaling">true면 640x480 기준 스케일 레이어에 배치한다(최초 생성 시 결정).</param>
        /// <param name="pivot">앵커/피벗 기준 지점(9지점/스트레치)</param>
        /// <param name="texture">표시할 텍스처. null이면 색만 있는 패널이 된다.</param>
        /// <param name="x">기준 지점으로부터의 X 오프셋(오른쪽 +)</param>
        /// <param name="y">기준 지점으로부터의 Y 오프셋(위쪽 +)</param>
        /// <param name="width">너비</param>
        /// <param name="height">높이</param>
        /// <param name="r">빨강(0~1)</param>
        /// <param name="g">초록(0~1)</param>
        /// <param name="b">파랑(0~1)</param>
        /// <param name="a">알파(0~1)</param>
        /// <returns>생성된 요소를 제어하는 핸들</returns>
        public UIElementHandle CreateImageWithTexture(int layer, bool scaling, UIPivot pivot, Texture texture, float x, float y, float width, float height, float r, float g, float b, float a)
        {
            return CreateImageFromTexture(GetOrCreateLayer(layer, scaling), pivot, texture, x, y, width, height, r, g, b, a);
        }

        /// <summary>
        /// 지정한 부모 Transform 아래에 텍스처를 붙인 RawImage 요소를 생성한다. 경로 기반/텍스처 기반 생성의 공용 본체다.
        /// </summary>
        /// <param name="parent">부모 Transform(레이어 캔버스 또는 다른 요소).</param>
        /// <param name="pivot">앵커/피벗 기준 지점(9지점/스트레치).</param>
        /// <param name="texture">표시할 텍스처(null이면 패널).</param>
        /// <param name="x">기준 지점으로부터의 X 오프셋(스트레치 축이면 sizeDelta 보정).</param>
        /// <param name="y">기준 지점으로부터의 Y 오프셋(스트레치 축이면 sizeDelta 보정).</param>
        /// <param name="width">너비(스트레치 축이면 부모 대비 증감량).</param>
        /// <param name="height">높이(스트레치 축이면 부모 대비 증감량).</param>
        /// <param name="r">빨강(0~1)</param>
        /// <param name="g">초록(0~1)</param>
        /// <param name="b">파랑(0~1)</param>
        /// <param name="a">알파(0~1)</param>
        /// <returns>생성된 요소를 제어하는 핸들</returns>
        private UIElementHandle CreateImageFromTexture(Transform parent, UIPivot pivot, Texture texture, float x, float y, float width, float height, float r, float g, float b, float a)
        {
            GameObject go = new GameObject("UIImage", typeof(RawImage));
            go.transform.SetParent(parent, false);

            RawImage image = go.GetComponent<RawImage>();
            image.texture = texture;
            image.color = new Color(r, g, b, a);
            image.raycastTarget = false;

            GetAnchorConfig(pivot, out Vector2 anchorMin, out Vector2 anchorMax, out Vector2 pivotPoint);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivotPoint;
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);

            return new UIElementHandle(go, image);
        }

        /// <summary>
        /// 지정 레이어에 스프라이트 폰트 텍스트(XOPSSpriteText)를 생성하고 핸들을 반환한다.
        /// pivot은 UI 요소의 기준점(앵커/피벗), alignment는 글자 정렬을 따로 정한다.
        /// 예: pivot=TopRight(우상단에 배치) + alignment=Center(글자는 가운데 정렬).
        /// x/y는 pivot 지점으로부터의 오프셋(+x 오른쪽, +y 위쪽)이다.
        /// </summary>
        /// <param name="layer">레이어 우선순위(sortingOrder). 없으면 lazy 생성</param>
        /// <param name="scaling">true면 640x480 기준 스케일 레이어에 배치한다(최초 생성 시 결정).</param>
        /// <param name="pivot">UI 요소 기준점(앵커/피벗, 9지점)</param>
        /// <param name="alignment">글자 정렬 기준점(9지점)</param>
        /// <param name="text">표시할 문자열</param>
        /// <param name="x">pivot 지점으로부터의 X 오프셋(오른쪽 +)</param>
        /// <param name="y">pivot 지점으로부터의 Y 오프셋(위쪽 +)</param>
        /// <param name="fontWidth">글자 너비</param>
        /// <param name="fontHeight">글자 높이</param>
        /// <param name="spacing">글자 간격</param>
        /// <param name="r">빨강(0~1)</param>
        /// <param name="g">초록(0~1)</param>
        /// <param name="b">파랑(0~1)</param>
        /// <param name="a">알파(0~1)</param>
        /// <returns>생성된 텍스트를 제어하는 핸들</returns>
        public UITextHandle CreateText(int layer, bool scaling, UIPivot pivot, UIPivot alignment, string text, float x, float y, float fontWidth, float fontHeight, float spacing, float r, float g, float b, float a)
        {
            return CreateTextUnder(GetOrCreateLayer(layer, scaling), pivot, alignment, text, x, y, fontWidth, fontHeight, spacing, r, g, b, a);
        }

        /// <summary>
        /// 지정한 부모 Transform 아래에 스프라이트 텍스트(XOPSSpriteText)를 생성한다. 레이어 직속(CreateText) 또는
        /// 다른 요소의 자식(UIElementHandle.CreateChildText)으로 붙일 때 공용으로 쓴다.
        /// pivot은 UI 요소 기준점(앵커/피벗), alignment는 글자 정렬을 따로 정한다.
        /// </summary>
        /// <param name="parent">부모 Transform(레이어 캔버스 또는 다른 요소).</param>
        /// <param name="pivot">UI 요소 기준점(앵커/피벗, 9지점).</param>
        /// <param name="alignment">글자 정렬 기준점(9지점).</param>
        /// <param name="text">표시할 문자열.</param>
        /// <param name="x">pivot 지점으로부터의 X 오프셋(오른쪽 +).</param>
        /// <param name="y">pivot 지점으로부터의 Y 오프셋(위쪽 +).</param>
        /// <param name="fontWidth">글자 너비.</param>
        /// <param name="fontHeight">글자 높이.</param>
        /// <param name="spacing">글자 간격.</param>
        /// <param name="r">빨강(0~1)</param>
        /// <param name="g">초록(0~1)</param>
        /// <param name="b">파랑(0~1)</param>
        /// <param name="a">알파(0~1)</param>
        /// <returns>생성된 텍스트를 제어하는 핸들.</returns>
        public UITextHandle CreateTextUnder(Transform parent, UIPivot pivot, UIPivot alignment, string text, float x, float y, float fontWidth, float fontHeight, float spacing, float r, float g, float b, float a)
        {
            GetAnchorConfig(pivot, out Vector2 anchorMin, out Vector2 anchorMax, out Vector2 pivotPoint);
            TextAnchor textAlignment = PivotToTextAnchor(alignment);
            Vector2 fontSize = new Vector2(fontWidth, fontHeight);

            XOPSSpriteText spriteText = FontManager.CreateSpriteText<XOPSSpriteText>(
                parent, text, anchorMin, anchorMax, new Vector2(x, y), fontSize, fontSize, new Color(r, g, b, a), textAlignment, spacing);

            // CreateSpriteText는 정렬에서 rect.pivot을 유도하므로, UI 기준점(pivot)으로 덮어써 정렬과 분리한다.
            spriteText.rectTransform.pivot = pivotPoint;
            spriteText.raycastTarget = false;

            return new UITextHandle(spriteText.gameObject, spriteText);
        }

        /// <summary>
        /// 지정 레이어에 OS 폰트(TMP) 텍스트를 생성하고 핸들을 반환한다.
        /// 스프라이트 폰트(CreateText) 대신 가독성 좋은 OS 폰트를 쓸 때 사용하며, 글자 크기는 스칼라(pt)다.
        /// </summary>
        /// <param name="layer">레이어 우선순위(sortingOrder).</param>
        /// <param name="scaling">true면 640x480 기준 스케일 레이어에 배치.</param>
        /// <param name="pivot">UI 요소 기준점(앵커/피벗).</param>
        /// <param name="alignment">글자 정렬 기준점.</param>
        /// <param name="text">표시할 문자열.</param>
        /// <param name="x">pivot 지점 기준 X 오프셋.</param>
        /// <param name="y">pivot 지점 기준 Y 오프셋.</param>
        /// <param name="fontSize">글자 크기(pt).</param>
        /// <param name="r">빨강(0~1)</param>
        /// <param name="g">초록(0~1)</param>
        /// <param name="b">파랑(0~1)</param>
        /// <param name="a">알파(0~1)</param>
        /// <returns>OS 폰트 텍스트 제어 핸들.</returns>
        public UIOSTextHandle CreateOSText(int layer, bool scaling, UIPivot pivot, UIPivot alignment, string text, float x, float y, float fontSize, float r, float g, float b, float a)
        {
            return CreateOSTextUnder(GetOrCreateLayer(layer, scaling), pivot, alignment, text, x, y, fontSize, r, g, b, a);
        }

        /// <summary>
        /// 지정한 부모 Transform 아래에 OS 폰트(TMP) 텍스트를 생성한다. 레이어 직속(CreateOSText) 또는 다른 요소의 자식으로 붙일 때 공용.
        /// </summary>
        /// <param name="parent">부모 Transform.</param>
        /// <param name="pivot">UI 요소 기준점(앵커/피벗).</param>
        /// <param name="alignment">글자 정렬 기준점.</param>
        /// <param name="text">표시할 문자열.</param>
        /// <param name="x">pivot 지점 기준 X 오프셋.</param>
        /// <param name="y">pivot 지점 기준 Y 오프셋.</param>
        /// <param name="fontSize">글자 크기(pt).</param>
        /// <param name="r">빨강(0~1)</param>
        /// <param name="g">초록(0~1)</param>
        /// <param name="b">파랑(0~1)</param>
        /// <param name="a">알파(0~1)</param>
        /// <returns>OS 폰트 텍스트 제어 핸들.</returns>
        public UIOSTextHandle CreateOSTextUnder(Transform parent, UIPivot pivot, UIPivot alignment, string text, float x, float y, float fontSize, float r, float g, float b, float a)
        {
            GetAnchorConfig(pivot, out Vector2 anchorMin, out Vector2 anchorMax, out Vector2 pivotPoint);
            TextAnchor textAlignment = PivotToTextAnchor(alignment);

            TextMeshProUGUI tmp = FontManager.CreateOSFont(
                parent, text, anchorMin, anchorMax, new Vector2(x, y), Vector2.zero, fontSize, new Color(r, g, b, a), textAlignment);

            // CreateOSFont는 정렬에서 rect.pivot을 유도하므로, UI 기준점(pivot)으로 덮어써 정렬과 분리한다.
            tmp.rectTransform.pivot = pivotPoint;
            tmp.raycastTarget = false;

            return new UIOSTextHandle(tmp.gameObject, tmp);
        }

        /// <summary>
        /// streamingAssets 하위의 이미지 파일을 Texture2D로 로드한다.
        /// ImageLoader를 통해 DDS/BMP/TGA/PNG/JPG를 지원하며, 경로별로 텍스처가 캐시된다.
        /// 캐시가 텍스처를 소유하므로 호출 측(핸들)은 텍스처를 Destroy하지 않는다.
        /// </summary>
        /// <param name="relativePath">streamingAssets 기준 경로. 비면 null 반환(패널용).</param>
        /// <returns>로드한 Texture2D, 경로가 비거나 로드 실패면 null</returns>
        public static Texture2D LoadTexture(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return null;
            }

            string fullPath = SafePath.Combine(Application.streamingAssetsPath, relativePath);
            return ImageLoader.LoadTexture(fullPath);
        }

        /// <summary>
        /// 현재 마우스 포인터가 지정 RectTransform 위에 있는지 검사한다.
        /// ScreenSpaceOverlay라 카메라 없이 기하학적으로 판정하며, CanvasScaler 스케일은 자동 반영된다.
        /// </summary>
        /// <param name="rect">검사할 RectTransform</param>
        /// <returns>포인터가 rect 영역 안이면 true</returns>
        public static bool IsPointerOver(RectTransform rect)
        {
            if (rect == null || Mouse.current == null)
            {
                return false;
            }
            return RectTransformUtility.RectangleContainsScreenPoint(rect, Mouse.current.position.ReadValue(), null);
        }

        /// <summary>
        /// 현재 마우스 포인터를 지정 RectTransform의 로컬 좌표로 변환한다(드래그/슬라이더 계산용).
        /// </summary>
        /// <param name="rect">기준 RectTransform</param>
        /// <returns>rect 로컬 좌표. rect/마우스가 없으면 (0,0)</returns>
        public static Vector2 PointerLocal(RectTransform rect)
        {
            if (rect == null || Mouse.current == null)
            {
                return Vector2.zero;
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, Mouse.current.position.ReadValue(), null, out Vector2 local);
            return local;
        }

        /// <summary>
        /// 현재 마우스 포인터를 지정 레이어 캔버스의 로컬 좌표(중심 기준, CanvasScaler 스케일 반영)로 변환한다.
        /// 십자 커서 등 포인터를 따라가는 UI가 그대로 anchoredPosition(SetPosition)에 넘길 수 있는 좌표다.
        /// scaling은 레이어 최초 생성 시 값과 동일하게 줘야 같은 좌표계를 얻는다.
        /// </summary>
        /// <param name="sortingOrder">기준 레이어 우선순위</param>
        /// <param name="scaling">레이어 생성 시 사용한 scaling 값</param>
        /// <returns>레이어 로컬 좌표. 포인터가 없으면 (0,0)</returns>
        public static Vector2 PointerInLayer(int sortingOrder, bool scaling)
        {
            return PointerLocal(Instance.GetOrCreateLayer(sortingOrder, scaling) as RectTransform);
        }
    }
}
