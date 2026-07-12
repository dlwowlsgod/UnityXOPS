using XLua;

namespace UnityXOPS.Modding
{
    public partial class UnityXOPSAPI
    {
        private UIAPI m_ui;
        public UIAPI UI => m_ui ??= new UIAPI();
    }

    /// <summary>
    /// 모드에 레터박스/페이드 등 화면 오버레이 제어를 제공하는 API 그룹.
    /// Lua에서는 XOPS.UI 로 접근한다.
    /// </summary>
    [LuaCallCSharp]
    public class UIAPI
    {
        /// <summary>
        /// 레터박스를 지정 레이어에 표시/숨기고 비율을 설정한다. show=false면 비율과 무관하게 끈다.
        /// </summary>
        /// <param name="layer">레터박스 레이어 우선순위(높을수록 위)</param>
        /// <param name="show">true면 레터박스를 표시한다.</param>
        /// <param name="ratio">상/하단 각각이 차지할 화면 비율(0~0.5). 0.1이면 위아래 10%씩, 총 20%.</param>
        public void SetLetterbox(int layer, bool show, float ratio)
        {
            UIOverlayManager.Instance.SetLetterbox(layer, show, ratio);
        }

        /// <summary>
        /// 지정 레이어의 화면 페이드를 지정 시간 동안 페이드 인(검은 화면 → 투명)한다. 씬 등장 연출용.
        /// </summary>
        /// <param name="layer">페이드 레이어 우선순위(높을수록 위)</param>
        /// <param name="duration">페이드 시간(초). 0이면 즉시</param>
        public void FadeIn(int layer, float duration)
        {
            UIOverlayManager.Instance.FadeIn(layer, duration);
        }

        /// <summary>
        /// 지정 레이어의 화면 페이드를 지정 시간 동안 페이드 아웃(투명 → 검은 화면)한다. 씬 퇴장 연출용.
        /// </summary>
        /// <param name="layer">페이드 레이어 우선순위(높을수록 위)</param>
        /// <param name="duration">페이드 시간(초). 0이면 즉시</param>
        public void FadeOut(int layer, float duration)
        {
            UIOverlayManager.Instance.FadeOut(layer, duration);
        }

        /// <summary>
        /// 지정 레이어의 페이드 알파를 즉시 지정한다. 씬 시작 시 초기 상태를 명시(0=투명, 1=암전)하거나 페이드를 스킵할 때 쓴다.
        /// 예: Briefing/Result처럼 페이드 없이 시작하려면 SetFade(layer, 0).
        /// </summary>
        /// <param name="layer">페이드 레이어 우선순위(높을수록 위)</param>
        /// <param name="alpha">페이드 알파(0 투명 ~ 1 암전)</param>
        public void SetFade(int layer, float alpha)
        {
            UIOverlayManager.Instance.SetFade(layer, alpha);
        }

        /// <summary>
        /// 레터박스를 끄고 페이드를 즉시 투명으로 되돌린다. 오버레이를 한 번에 초기화하는 편의 메서드.
        /// </summary>
        public void ClearOverlay()
        {
            UIOverlayManager.Instance.ClearOverlay();
        }

        /// <summary>
        /// 지정 sortOrder의 캔버스(레이어)를 미리 생성한다. 이미 있으면 아무 일도 하지 않는다.
        /// scaling이 true면 그 캔버스는 항상 640x480 기준(높이 매치)으로 동작해 4:3→16:9에서도 깨지지 않는다.
        /// 한 sortOrder의 scaling 여부는 최초 생성 시 결정된다.
        /// </summary>
        /// <param name="sortOrder">캔버스 우선순위(높을수록 위)</param>
        /// <param name="scaling">true면 640x480 기준 CanvasScaler(Height 매치)를 적용한다.</param>
        public void CreateCanvas(int sortOrder, bool scaling)
        {
            UIOverlayManager.Instance.GetOrCreateLayer(sortOrder, scaling);
        }

        /// <summary>
        /// 지정 레이어의 스케일 팩터(UI 전체 확대/축소)를 설정한다. 레이어가 없으면 생성한다.
        /// Constant Pixel Size로 만든 레이어(CreateCanvas의 scaling=false)에서 의미가 있으며, scaling=true 레이어에서는 무시된다.
        /// </summary>
        /// <param name="layer">대상 레이어 우선순위</param>
        /// <param name="factor">스케일 팩터(1=기본, 2=2배). 0 이하는 0.01로 보정</param>
        public void SetScaleFactor(int layer, float factor)
        {
            UIOverlayManager.Instance.SetLayerScaleFactor(layer, factor);
        }

        /// <summary>
        /// 지정 레이어의 현재 스케일 팩터를 반환한다. 레이어가 없으면 생성한다.
        /// </summary>
        /// <param name="layer">대상 레이어 우선순위</param>
        /// <returns>현재 스케일 팩터(기본 1)</returns>
        public float GetScaleFactor(int layer)
        {
            return UIOverlayManager.Instance.GetLayerScaleFactor(layer);
        }

        /// <summary>
        /// 현재 마우스 포인터의 지정 레이어 로컬 X 좌표(중심 기준, 스케일 반영)를 반환한다.
        /// 반환값을 그대로 핸들의 SetPosition X에 넘기면 포인터를 따라가는 UI를 만들 수 있다(십자 커서 등).
        /// scaling은 그 레이어를 만들 때 쓴 값과 동일하게 준다.
        /// </summary>
        /// <param name="layer">기준 레이어 우선순위</param>
        /// <param name="scaling">레이어 생성 시 scaling 값</param>
        /// <returns>레이어 로컬 X 좌표</returns>
        public float GetPointerX(int layer, bool scaling)
        {
            return UIOverlayManager.PointerInLayer(layer, scaling).x;
        }

        /// <summary>
        /// 현재 마우스 포인터의 지정 레이어 로컬 Y 좌표(중심 기준, 스케일 반영)를 반환한다.
        /// 반환값을 그대로 핸들의 SetPosition Y에 넘기면 포인터를 따라가는 UI를 만들 수 있다.
        /// scaling은 그 레이어를 만들 때 쓴 값과 동일하게 준다.
        /// </summary>
        /// <param name="layer">기준 레이어 우선순위</param>
        /// <param name="scaling">레이어 생성 시 scaling 값</param>
        /// <returns>레이어 로컬 Y 좌표</returns>
        public float GetPointerY(int layer, bool scaling)
        {
            return UIOverlayManager.PointerInLayer(layer, scaling).y;
        }

        /// <summary>
        /// 지정 레이어에 이미지/패널 UI를 생성하고 핸들을 반환한다.
        /// texturePath가 비면 색만 적용되는 패널이 된다. 반환 핸들로 이동/색/삭제를 제어한다.
        /// pivot 9지점 중 하나를 기준으로 배치하며, x/y는 그 지점으로부터의 오프셋(+x 오른쪽, +y 위쪽)이다.
        /// </summary>
        /// <param name="layer">레이어 우선순위(높을수록 위)</param>
        /// <param name="scaling">true면 640x480 기준 스케일 레이어에 배치(최초 생성 시 결정)</param>
        /// <param name="pivot">앵커/피벗 기준 지점 이름("TopRight", "center", "bottom_left" 등). 대소문자/구분자 무시</param>
        /// <param name="texturePath">streamingAssets 기준 이미지 경로(빈 문자열=패널)</param>
        /// <param name="x">기준 지점 기준 X 오프셋(오른쪽 +)</param>
        /// <param name="y">기준 지점 기준 Y 오프셋(위쪽 +)</param>
        /// <param name="width">너비</param>
        /// <param name="height">높이</param>
        /// <param name="r">빨강(0~1)</param>
        /// <param name="g">초록(0~1)</param>
        /// <param name="b">파랑(0~1)</param>
        /// <param name="a">알파(0~1)</param>
        /// <returns>요소 제어 핸들(UIElementHandle)</returns>
        public UIElementHandle CreateImage(int layer, bool scaling, string pivot, string texturePath, float x, float y, float width, float height, float r, float g, float b, float a)
        {
            return UIOverlayManager.Instance.CreateImage(layer, scaling, UIOverlayManager.ParsePivot(pivot), texturePath, x, y, width, height, r, g, b, a);
        }

        /// <summary>
        /// 지정 레이어에 스프라이트 폰트 텍스트를 생성하고 핸들을 반환한다.
        /// 반환 핸들로 텍스트/색/알파/위치/삭제를 제어한다(펄스·페이드는 update에서 SetAlpha로).
        /// pivot은 UI 요소 기준점, alignment는 글자 정렬을 따로 정한다(예: pivot 우상단 배치 + alignment 가운데 정렬).
        /// x/y는 pivot 지점으로부터의 오프셋(+x 오른쪽, +y 위쪽)이다.
        /// </summary>
        /// <param name="layer">레이어 우선순위(높을수록 위)</param>
        /// <param name="scaling">true면 640x480 기준 스케일 레이어에 배치(최초 생성 시 결정)</param>
        /// <param name="pivot">UI 요소 기준점 이름("TopRight", "center" 등). 대소문자/구분자 무시</param>
        /// <param name="alignment">글자 정렬 기준점 이름("center", "left" 등). 대소문자/구분자 무시</param>
        /// <param name="text">표시할 문자열</param>
        /// <param name="x">pivot 지점 기준 X 오프셋(오른쪽 +)</param>
        /// <param name="y">pivot 지점 기준 Y 오프셋(위쪽 +)</param>
        /// <param name="fontWidth">글자 너비</param>
        /// <param name="fontHeight">글자 높이</param>
        /// <param name="spacing">글자 간격</param>
        /// <param name="r">빨강(0~1)</param>
        /// <param name="g">초록(0~1)</param>
        /// <param name="b">파랑(0~1)</param>
        /// <param name="a">알파(0~1)</param>
        /// <returns>텍스트 제어 핸들(UITextHandle)</returns>
        public UITextHandle CreateText(int layer, bool scaling, string pivot, string alignment, string text, float x, float y, float fontWidth, float fontHeight, float spacing, float r, float g, float b, float a)
        {
            return UIOverlayManager.Instance.CreateText(layer, scaling, UIOverlayManager.ParsePivot(pivot), UIOverlayManager.ParsePivot(alignment), text, x, y, fontWidth, fontHeight, spacing, r, g, b, a);
        }

        /// <summary>
        /// 지정 레이어에 OS 폰트(TMP) 텍스트를 생성하고 핸들을 반환한다. 스프라이트 폰트(CreateText) 대신 가독성 텍스트용.
        /// CreateText와 달리 글자 크기가 스칼라(pt) 하나다(w/h가 아님). pivot=UI 기준점, alignment=글자 정렬.
        /// </summary>
        /// <param name="layer">레이어 우선순위(높을수록 위)</param>
        /// <param name="scaling">true면 640x480 기준 스케일 레이어에 배치</param>
        /// <param name="pivot">UI 요소 기준점 이름("TopRight", "center" 등). 대소문자/구분자 무시</param>
        /// <param name="alignment">글자 정렬 기준점 이름("center", "left" 등). 대소문자/구분자 무시</param>
        /// <param name="text">표시할 문자열</param>
        /// <param name="x">pivot 지점 기준 X 오프셋(오른쪽 +)</param>
        /// <param name="y">pivot 지점 기준 Y 오프셋(위쪽 +)</param>
        /// <param name="fontSize">글자 크기(pt)</param>
        /// <param name="r">빨강(0~1)</param>
        /// <param name="g">초록(0~1)</param>
        /// <param name="b">파랑(0~1)</param>
        /// <param name="a">알파(0~1)</param>
        /// <returns>OS 폰트 텍스트 제어 핸들(UIOSTextHandle)</returns>
        public UIOSTextHandle CreateOSText(int layer, bool scaling, string pivot, string alignment, string text, float x, float y, float fontSize, float r, float g, float b, float a)
        {
            return UIOverlayManager.Instance.CreateOSText(layer, scaling, UIOverlayManager.ParsePivot(pivot), UIOverlayManager.ParsePivot(alignment), text, x, y, fontSize, r, g, b, a);
        }
    }
}
