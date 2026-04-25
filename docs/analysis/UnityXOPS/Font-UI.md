# Font / UI

## 개요
XOPS 원본의 `char.dds` 스프라이트 폰트 텍스처를 Unity UI(uGUI)로 렌더링하는 시스템. `FontManager`가 텍스처 로드와 OS TMP 폰트 초기화를 담당하고, `XOPSSpriteText`(베이스)와 그 파생 클래스들이 실제 메시 생성 및 애니메이션을 수행한다. `FadeRawImage`는 화면 전환용 독립 UI 컴포넌트다.

## 클래스 목록

### FontManager
- **파일**: `Assets/UnityXOPS/Runtime/Font/FontManager.cs`
- **역할**: 스프라이트 폰트 텍스처 로드, 시스템 언어별 OS TMP 폰트 생성, `XOPSSpriteText` 파생 인스턴스 팩토리를 제공하는 싱글톤 매니저
- **주요 필드/프로퍼티**:
  - `osFont`(TMP_FontAsset) — `[SerializeField]`, 런타임에 동적 생성된 OS 폰트 에셋
  - `SpriteFontTexture`(Texture2D) — `[SerializeField]`, StreamingAssets에서 로드된 `char.dds` 텍스처
  - `m_koreanOSFontPath`, `m_japaneseOSFontPath`, `m_englishOSFontPath`(string) — OS 폰트 파일 절대 경로 (런타임 탐색)
  - `OSFont`(TMP_FontAsset, static) — `Instance.osFont` 래핑 정적 프로퍼티
- **주요 메서드**:
  - `Start()` — `StreamingAssets/data/char.dds`를 `ImageLoader.LoadTexture`로 로드하고, `Font.GetPathsToOSFonts()`로 시스템 언어에 맞는 폰트 경로를 찾아 `TMP_FontAsset.CreateFontAsset`으로 동적 생성
  - `CreateSpriteText<T>(root, text, anchorMin, anchorMax, position, size, fontSize, color, alignment, spacing)` — 제네릭 팩토리 메서드; `XOPSSpriteText` 파생 타입 T를 가진 `GameObject`를 생성하고 `RectTransform` 및 스프라이트 텍스트 속성을 일괄 설정해 반환
- **특이사항**:
  - OS 폰트 우선순위: 한국어 → `malgun.ttf`, 일본어 → `YuGothR.ttc`, 그 외 → `segoeui.ttf`
  - 폰트 경로를 찾지 못하면 `Debugger.LogWarning` 출력 후 조기 반환 (osFont = null 상태 유지)
  - `CreateSpriteText<T>`에서 피벗은 `TextAnchor` 값을 직접 `Vector2`로 변환 (switch expression)

---

### XOPSSpriteText
- **파일**: `Assets/UnityXOPS/Runtime/Font/XOPSSpriteText.cs`
- **역할**: `char.dds`(16×16 그리드) 텍스처를 UV 계산으로 문자별 쿼드를 생성해 렌더링하는 커스텀 uGUI 그래픽 컴포넌트. 파생 클래스들의 베이스
- **주요 필드/프로퍼티**:
  - `_charTexture`(Texture2D) — 스프라이트 폰트 텍스처 (`[SerializeField]`)
  - `_text`(string) — 렌더링할 문자열 (`[SerializeField]`)
  - `_charWidth`, `_charHeight`(float) — 문자 1개의 픽셀 크기, 기본값 32 (`[SerializeField]`)
  - `_spacing`(float) — 문자 간 추가 간격, 기본값 0 (`[SerializeField]`)
  - `_alignment`(TextAnchor) — 정렬 기준, 기본값 LowerLeft (`[SerializeField]`)
  - `Text`, `CharWidth`, `CharHeight`, `Spacing`, `Alignment` — 세터에서 `SetVerticesDirty()` 호출
  - `FontColor`(Color32) — `MaskableGraphic.color` 래핑; 세터에서 `SetVerticesDirty()` 호출
- **주요 메서드**:
  - `OnPopulateMesh(VertexHelper vh)` — 문자별 UV 계산 후 쿼드(버텍스 4개 + 삼각형 2개) 생성; 정렬에 따른 원점 오프셋(ox, oy) 적용
  - `OnValidate()` — 에디터 프로퍼티 변경 시 `SetVerticesDirty()` 호출
- **특이사항**:
  - UV 계산: `char.dds`는 16열×16행 그리드; 문자 코드에서 `col = charCode % 16`, `row = charCode / 16`으로 셀 위치 결정
  - V축 원점: `v0 = 1f - (row + 1) / 16f` (Unity UV V=0 하단 기준)
  - `[RequireComponent(typeof(CanvasRenderer))]` 지정
  - 총 텍스트 폭: `length * charWidth + (length - 1) * spacing` (마지막 글자 뒤 간격 미포함)

---

### XOPSSpriteTextButton
- **파일**: `Assets/UnityXOPS/Runtime/Font/XOPSSpriteTextButton.cs`
- **역할**: 마우스 호버/클릭 상태에 따라 색상과 앵커 위치가 바뀌는 버튼 기능이 추가된 스프라이트 텍스트
- **주요 필드/프로퍼티**:
  - `normalColor`, `hoverColor`, `pressedColor`(Color32) — 상태별 색상 (`[SerializeField]`)
  - `movePixelX`, `movePixelY`(float) — 눌렸을 때 위치 오프셋 (`[SerializeField]`)
  - `s_pressedItem`(static XOPSSpriteTextButton) — 현재 눌린 버튼 추적용 정적 필드
  - `m_originalAnchoredPos`(Vector2) — `Start()`에서 초기 앵커 위치 저장
  - `OnClick`(event Action) — 클릭 시 발생하는 이벤트
- **주요 메서드**:
  - `OnPointerEnter` — `s_pressedItem == null`일 때만 호버 색상 적용
  - `OnPointerExit` — 기본 색상 복귀
  - `OnPointerDown` — `s_pressedItem = this` 설정, 눌린 색상 및 위치 오프셋 적용
  - `OnPointerUp` — `s_pressedItem = null`, 위치 복원; 포인터가 여전히 위에 있으면 호버 색상 유지
  - `OnPointerClick` — `OnClick?.Invoke()`
- **특이사항**:
  - `IPointerEnterHandler`, `IPointerExitHandler`, `IPointerDownHandler`, `IPointerUpHandler`, `IPointerClickHandler` 모두 구현
  - `s_pressedItem` 정적 필드로 다른 버튼이 눌려있을 때 호버 색상 변경을 방지
  - `Start()`에서 `FontColor = normalColor` 호출은 코드 주석에 "설계적 미스"로 명시되어 있음

---

### XOPSSpriteTextFade
- **파일**: `Assets/UnityXOPS/Runtime/Font/XOPSSpriteTextFade.cs`
- **역할**: 알파 페이드 인/아웃 코루틴 애니메이션 기능이 추가된 스프라이트 텍스트
- **주요 필드/프로퍼티**: 없음 (베이스 클래스 필드만 사용)
- **주요 메서드**:
  - `SetAlphaZero()` — 알파값 즉시 0
  - `SetAlphaOne()` — 알파값 즉시 1
  - `FadeIn(float duration)` — 알파 0→1 코루틴 실행 (기존 코루틴 중지 후 재시작)
  - `FadeOut(float duration)` — 알파 1→0 코루틴 실행
  - `FadeRoutine(float from, float to, float duration)` — `Mathf.Lerp` 선형 보간, 매 프레임 `Time.deltaTime` 누산
- **특이사항**: RGB 채널은 유지하고 알파만 변경

---

### XOPSSpriteTextPulse
- **파일**: `Assets/UnityXOPS/Runtime/Font/XOPSSpriteTextPulse.cs`
- **역할**: 알파값이 지정 범위에서 주기적으로 반복 변화하는 펄스 애니메이션 기능이 추가된 스프라이트 텍스트
- **주요 필드/프로퍼티**: 없음
- **주요 메서드**:
  - `StartPulse(float duration, float alphaFrom, float alphaTo)` — 기존 코루틴 중지 후 펄스 루프 시작
  - `StopPulse()` — `StopAllCoroutines()` 호출
  - `PulseRoutine(...)` — `while (true)` 루프; 매 사이클 alphaFrom에서 alphaTo로 보간 후 다음 프레임에 alphaFrom으로 즉시 리셋
- **특이사항**: 한 사이클 완료 후 `yield return null`로 1프레임 대기 후 alphaFrom에서 재시작 (불연속 리셋)

---

### XOPSSpriteTextPulseSpread
- **파일**: `Assets/UnityXOPS/Runtime/Font/XOPSSpriteTextPulseSpread.cs`
- **역할**: 알파값과 문자 크기(CharWidth, CharHeight)가 동시에 변화하는 스프레드 펄스 애니메이션 기능이 추가된 스프라이트 텍스트
- **주요 필드/프로퍼티**: 없음
- **주요 메서드**:
  - `StartPulseSpread(duration, alphaFrom, alphaTo, startWidth, endWidth, startHeight, endHeight)` — 기존 코루틴 중지 후 스프레드 펄스 루프 시작
  - `PulseSpreadRoutine(...)` — `while (true)` 루프; 알파와 문자 크기를 동시에 Lerp; 사이클 종료 후 1프레임 대기 후 시작값으로 리셋
- **특이사항**: `CharWidth`/`CharHeight` 세터가 `SetVerticesDirty()`를 호출하므로 매 프레임 메시 재생성 발생

---

### FadeRawImage
- **파일**: `Assets/UnityXOPS/Runtime/UI/FadeRawImage.cs`
- **역할**: `RawImage`의 알파값을 페이드 인/아웃으로 제어하는 UI 컴포넌트. 주로 화면 전환용 검은 오버레이에 사용
- **주요 필드/프로퍼티**:
  - `_rawImage`(RawImage) — `Awake()`에서 `GetComponent`로 캐싱
- **주요 메서드**:
  - `Awake()` — RawImage 캐싱 및 초기 색상을 `Color.black`으로 설정
  - `SetAlphaZero()` — 알파 즉시 0
  - `SetAlphaOne()` — 알파 즉시 1
  - `FadeOut(float duration)` — 알파 0→1 코루틴 (화면이 검게 변함)
  - `FadeIn(float duration)` — 알파 1→0 코루틴 (화면이 밝아짐)
  - `FadeRoutine(float from, float to, float duration)` — `Mathf.Lerp` 선형 보간
- **특이사항**:
  - `[RequireComponent(typeof(RawImage))]` 지정
  - 메서드 명칭 주의: `FadeOut`이 알파 0→1(화면 어두워짐), `FadeIn`이 알파 1→0(화면 밝아짐)으로, 일반적인 용어 관례와 반대
