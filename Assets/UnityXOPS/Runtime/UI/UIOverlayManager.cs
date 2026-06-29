using UnityEngine;
using UnityEngine.UI;
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

        private Canvas m_letterboxCanvas;
        private RectTransform m_topBar;
        private RectTransform m_bottomBar;

        private Canvas m_fadeCanvas;
        private FadeRawImage m_fade;

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
        /// 페이드/레터박스 상태는 의도적으로 건드리지 않는다 — 이전 씬 FadeOut(검정) → 로드 → 새 씬 FadeIn
        /// 같은 전환 연속성을 위해서다. 각 씬은 start()에서 자신의 오버레이 상태를 명시적으로 선언한다.
        /// 신 씬의 LuaSceneController.Start() 보다 먼저 실행되므로 새 UI를 지우지 않는다.
        /// </summary>
        /// <param name="scene">언로드된 씬</param>
        private void OnSceneUnloaded(Scene scene)
        {
            ClearDynamicLayers();
        }

        /// <summary>
        /// 모드/씬이 만든 동적 레이어(CreateImage/CreateText)를 모두 파괴한다.
        /// 페이드/레터박스는 별도 전용 캔버스라 영향받지 않는다.
        /// </summary>
        public void ClearDynamicLayers()
        {
            foreach (Transform layer in m_layers.Values)
            {
                if (layer != null)
                {
                    Destroy(layer.gameObject);
                }
            }
            m_layers.Clear();
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
        /// 풀스크린 페이드 이미지와 전용 캔버스를 아직 없으면 생성한다(lazy).
        /// </summary>
        private void EnsureFade()
        {
            if (m_fade != null)
            {
                return;
            }

            m_fadeCanvas = CreateOverlayCanvas("FadeOverlay");
            GameObject fadeGO = new GameObject("Fade", typeof(RawImage), typeof(FadeRawImage));
            fadeGO.transform.SetParent(m_fadeCanvas.transform, false);
            RawImage fadeImage = fadeGO.GetComponent<RawImage>();
            fadeImage.texture = Texture2D.whiteTexture;
            fadeImage.raycastTarget = false;
            Stretch(fadeImage.rectTransform);
            m_fade = fadeGO.GetComponent<FadeRawImage>();
            m_fade.SetAlphaZero();
        }

        /// <summary>
        /// 지정 sortingOrder의 오버레이 레이어(독립 ScreenSpaceOverlay Canvas)를 가져오거나 생성한다.
        /// sortingOrder가 높을수록 위에 그려진다. UI 요소를 반환된 Transform 아래에 붙인다.
        /// scaling이 true면 640x480 기준의 CanvasScaler(높이 매치)를 붙여 비율이 바뀌어도 깨지지 않게 한다.
        /// 한 sortingOrder의 scaling 여부는 최초 생성 시 결정되며 이후 호출에서는 무시된다.
        /// </summary>
        /// <param name="sortingOrder">레이어 우선순위(정수)</param>
        /// <param name="scaling">true면 640x480 기준 CanvasScaler(Height 매치)를 적용한다.</param>
        /// <returns>해당 레이어 Canvas의 Transform</returns>
        public Transform GetOrCreateLayer(int sortingOrder, bool scaling = false)
        {
            if (m_layers.TryGetValue(sortingOrder, out Transform existing))
            {
                return existing;
            }

            GameObject go = new GameObject($"OverlayLayer_{sortingOrder}", typeof(Canvas));
            go.transform.SetParent(transform, false);
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            if (scaling)
            {
                CanvasScaler scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(k_referenceWidth, k_referenceHeight);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f;
            }

            m_layers[sortingOrder] = go.transform;
            return go.transform;
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
        /// 지정 레이어(order)의 화면 페이드를 지정 시간 동안 페이드 인(어둠 → 화면)한다.
        /// </summary>
        /// <param name="order">페이드 캔버스 sortingOrder(높을수록 위)</param>
        /// <param name="duration">페이드 시간(초)</param>
        public void FadeIn(int order, float duration)
        {
            EnsureFade();
            m_fadeCanvas.sortingOrder = order;
            m_fade.FadeIn(duration);
        }

        /// <summary>
        /// 지정 레이어(order)의 화면 페이드를 지정 시간 동안 페이드 아웃(화면 → 어둠)한다.
        /// </summary>
        /// <param name="order">페이드 캔버스 sortingOrder(높을수록 위)</param>
        /// <param name="duration">페이드 시간(초)</param>
        public void FadeOut(int order, float duration)
        {
            EnsureFade();
            m_fadeCanvas.sortingOrder = order;
            m_fade.FadeOut(duration);
        }

        /// <summary>
        /// 지정 레이어(order)의 페이드 알파를 즉시 설정한다(진행 중 페이드 중단).
        /// 씬 시작 시 초기 상태를 명시(0=투명, 1=완전 암전)하거나 페이드를 스킵할 때 쓴다.
        /// </summary>
        /// <param name="order">페이드 캔버스 sortingOrder(높을수록 위)</param>
        /// <param name="alpha">페이드 알파(0 투명 ~ 1 암전)</param>
        public void SetFade(int order, float alpha)
        {
            EnsureFade();
            m_fadeCanvas.sortingOrder = order;
            m_fade.SetAlpha(alpha);
        }

        /// <summary>
        /// 레터박스를 끄고 페이드를 즉시 투명으로 되돌린다. 오버레이를 한 번에 초기화하는 편의 메서드.
        /// 아직 생성되지 않은 오버레이는 건드리지 않는다(레이어는 유지).
        /// </summary>
        public void ClearOverlay()
        {
            if (m_topBar != null)
            {
                ApplyLetterbox(false, 0f);
            }
            if (m_fade != null)
            {
                m_fade.SetAlpha(0f);
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
            Transform parent = GetOrCreateLayer(layer, scaling);

            GameObject go = new GameObject("UIImage", typeof(RawImage));
            go.transform.SetParent(parent, false);

            RawImage image = go.GetComponent<RawImage>();
            image.texture = LoadTexture(texturePath);
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
            Transform parent = GetOrCreateLayer(layer, scaling);

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
        /// RectTransform을 부모 영역 전체로 늘린다.
        /// </summary>
        /// <param name="rect">대상 RectTransform</param>
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
    }
}
