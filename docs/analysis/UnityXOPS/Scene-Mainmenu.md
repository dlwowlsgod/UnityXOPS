# Mainmenu 씬

## 개요

씬 인덱스 2. 데모 맵을 배경으로 렌더링하면서 공식/애드온 미션 목록을 표시하고 게임 시작을 대기한다. 미션 선택 시 맵 데이터를 교체하고 씬 인덱스 3(Briefing)으로 전환한다. ESC 입력 시 종료 확인 팝업을 표시한다.

**씬 전환 흐름**: Opening(1) → Mainmenu(2) → Briefing(3), 또는 Briefing(3) → Mainmenu(2) (ESC 복귀)

---

## 클래스 목록

### MainmenuScene

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Mainmenu/MainmenuScene.cs`
- **역할**: 데모 맵 로드, 미션 선택 처리, ESC 팝업 표시, 브리핑 씬 전환
- **주요 필드/프로퍼티**:
  - `IsAddonTab`(static bool) — 현재 선택된 탭이 애드온 탭인지 여부 (씬 재진입 시 상태 유지)
  - `OfficialScrollIndex`(static int) — 공식 미션 스크롤 위치 (씬 재진입 시 상태 유지)
  - `AddonScrollIndex`(static int) — 애드온 미션 스크롤 위치 (씬 재진입 시 상태 유지)
  - `switchCanvas`(GameObject) — 탭 전환 UI (애드온 미션이 없으면 비활성화)
  - `exitCanvas`(GameObject) — 종료 확인 팝업 캔버스
- **주요 메서드**:
  - `Start()` — DataManager에서 demoData를 읽어 랜덤 맵 로드. 애드온 미션 없으면 switchCanvas 비활성화
  - `Update()` — ESC 입력 시 exitCanvas 활성화
  - `Load(int index, bool mif)` — 데모 맵 언로드, 미션 데이터 로드(mif=true이면 애드온), 맵/스카이 로드 후 SceneManager.LoadScene(3)
- **특이사항**:
  - `Load()`에서 `UnloadBlockData()`가 두 번 연속 호출된다 (`UnloadPointData()` 가 한 번 빠져있는 형태). 코드상 중복 호출.
  - static 필드 3개(`IsAddonTab`, `OfficialScrollIndex`, `AddonScrollIndex`)로 씬 재로드 시에도 탭/스크롤 상태를 유지한다.

---

### MainmenuMousePointer

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Mainmenu/MainmenuMousePointer.cs`
- **역할**: 마우스 위치에 맞춰 수평·수직 십자선 RawImage 위치를 매 프레임 갱신
- **주요 필드/프로퍼티**:
  - `horizontal`(RectTransform) — 수평선 UI
  - `vertical`(RectTransform) — 수직선 UI
  - `canvasRect`(RectTransform) — 스크린→로컬 좌표 변환 기준 캔버스
  - `color`(Color32) — 십자선 색상
- **주요 메서드**:
  - `Start()` — horizontal·vertical RawImage에 color 적용
  - `Update()` — `RectTransformUtility.ScreenPointToLocalPointInRectangle`으로 마우스 좌표를 변환하여 anchoredPosition 갱신

---

### GameVersionTex

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Mainmenu/GameVersionText.cs`
- **역할**: `Application.version` 문자열을 그림자(+1, -1 오프셋) + 본문 2겹으로 스프라이트 텍스트 렌더링
- **주요 필드/프로퍼티**:
  - `root`(RectTransform) — 텍스트 생성 부모
  - `position`(Vector2) — 본문 텍스트 anchored 위치
  - `textColor`(Color32) — 본문 색상
  - `shadowColor`(Color32) — 그림자 색상
- **주요 메서드**:
  - `Start()` — FontManager.CreateSpriteText\<XOPSSpriteText\> 2회 호출 (그림자, 본문). 폰트 크기 18×22, TextAnchor.UpperRight

---

### GameTitleImage

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Mainmenu/GameTitleImage.cs`
- **역할**: `data/title.dds`를 로드하여 RawImage에 적용하고 RectTransform 크기·위치 설정
- **주요 필드/프로퍼티**:
  - `titleImage`(RawImage) — 타이틀 이미지 대상 RawImage
  - `position`(Vector2) — anchoredPosition
  - `size`(Vector2) — sizeDelta
  - `k_titlePath`(const string) — `"data/title.dds"`
- **주요 메서드**:
  - `Start()` — `ImageLoader.LoadTexture(path)`로 텍스처 로드 후 RectTransform에 size·position 적용

---

### MissionItemScroll

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Mainmenu/MissionItemScroll.cs`
- **역할**: 드래그 가능한 커스텀 스크롤바 UI. 드래그/클릭으로 정수 인덱스 이벤트 발행
- **주요 필드/프로퍼티**:
  - `outlineNormalColor`, `outlineHoverColor`, `outlinePressedColor`(Color32) — 스크롤바 외곽선 색상 3상태
  - `normalColor`, `hoverColor`, `pressedColor`(Color32) — 스크롤바 내부 색상 3상태
  - `outlineRawImage`(RawImage) — 스크롤바 외곽선 이미지
  - `rawImage`(RawImage) — 스크롤바 내부 이미지
  - `OnScrollIndexChanged`(event Action\<int\>) — 인덱스 변경 시 발행
  - `ScrollAreaRawImage`(RawImage, get) — 스크롤 트랙 영역 RawImage
  - `ScrollbarRawImage`(RawImage, get) — 스크롤바 핸들 RawImage (= outlineRawImage)
  - `m_maxIndex`(int) — 스크롤 최대 인덱스
  - `m_floatPosition`(float) — 연속 float 스크롤 위치
  - `m_grabOffset`(float) — 드래그 시 핸들 내 클릭 오프셋
- **주요 메서드**:
  - `Initialize(int totalItems, int visibleItems, int maxIndex)` — 스크롤바 높이(`trackHeight * visibleItems / totalItems`) 및 최대 인덱스 설정
  - `SetScrollPosition(int index)` — 외부(Up/Down 버튼)에서 정수 인덱스로 위치 직접 지정
  - `OnPointerDown` — 그랩 오프셋 계산 후 위치 갱신
  - `OnDrag` — 드래그 중 위치 갱신
  - `UpdateFromLocal(Vector2)` — float 위치 계산 → ApplyFloatPosition → 인덱스 변경 시 이벤트 발행
- **특이사항**:
  - `IPointerEnterHandler`, `IPointerExitHandler`, `IPointerDownHandler`, `IPointerUpHandler`, `IDragHandler` 5개 인터페이스 구현
  - float 위치를 내부에서 유지하고 `FloorToInt`로 이벤트 발행용 정수 인덱스로 변환

---

### MissionItemController (abstract)

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Mainmenu/MissionItemController.cs`
- **역할**: Up/Down 버튼과 8개 미션 아이템 버튼(그림자+텍스트 구조)을 동적 생성하는 추상 베이스 클래스
- **주요 필드/프로퍼티**:
  - `mainmenuScene`(MainmenuScene) — 미션 로드 위임 대상
  - `upButton`, `downButton`(XOPSSpriteTextButton) — 스크롤 버튼
  - `missionItems`(List\<XOPSSpriteTextButton\>) — 미션 항목 버튼 목록
  - `spriteButtonRoot`(List\<Transform\>) — 각 버튼의 루트 Transform 목록 (Up/Down 버튼 루트는 제외됨)
  - `m_itemCount`(int) — 표시 항목 수, 기본값 8
- **주요 메서드**:
  - `Start()` — Up 버튼, m_itemCount개 아이템 버튼, Down 버튼을 세로로 배치 생성. spriteButtonRoot에서 Up/Down 루트를 제거하여 아이템 루트만 남김
  - `CreateButtonText(...)` — 그림자(XOPSSpriteText) + 버튼(XOPSSpriteTextButton) 2개를 하나의 루트 오브젝트 아래 생성
  - `UpButtonClicked()`, `DownButtonClicked()`, `MissionItemClicked(int)` — abstract
- **특이사항**:
  - `spriteButtonRoot`는 Start 후 Up/Down 인덱스(0번, m_itemCount+1번)를 `RemoveAt`으로 제거하여 아이템 인덱스와 1:1 매핑된다.
  - 아이템 높이는 `RectTransform.sizeDelta.y / 10`으로 계산된다.

---

### OfficialMissionItemController

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Mainmenu/OfficialMissionItemController.cs`
- **역할**: 공식 미션 목록 스크롤 표시 및 미션 선택 처리
- **주요 필드/프로퍼티**:
  - `disabledColor`(Color32) — 비활성화된 버튼의 그림자 색상
  - `itemScroll`(MissionItemScroll) — 연결된 스크롤바 컴포넌트
  - `m_maxItemIndex`(int) — `m_data.Count - m_itemCount`
  - `m_topIndex`(int) — 현재 목록 상단 인덱스
  - `m_data`(List\<OfficialMissionData\>) — DataManager에서 가져온 공식 미션 데이터
- **주요 메서드**:
  - `Start()` — m_itemCount를 `Min(m_data.Count, 8)`로 설정 후 base.Start() 호출. 텍스트 초기화, 이벤트 구독, 스크롤 초기화. `MainmenuScene.OfficialScrollIndex`로 초기 위치 복원
  - `ScrollToIndex(int)` — 버튼 텍스트 갱신, Up/Down 버튼 활성화 상태 갱신, 스크롤바 위치 갱신
  - `SetButtonState(XOPSSpriteTextButton, bool)` — 버튼 활성화 + 그림자 색상 전환
  - `UpButtonClicked()` — `ScrollToIndex(m_topIndex - 1)`
  - `DownButtonClicked()` — `ScrollToIndex(m_topIndex + 1)`
  - `MissionItemClicked(int index)` — `mainmenuScene.Load(m_topIndex + index, false)`
- **특이사항**:
  - 미션 수가 m_itemCount 이하이면 Up/Down 버튼 비활성화, 스크롤바 숨김 및 레이캐스트 비활성화
  - 스크롤 인덱스를 static 필드 `MainmenuScene.OfficialScrollIndex`에 저장하여 씬 재진입 시 유지

---

### AddonMissionItemController

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Mainmenu/AddonMissionItemController.cs`
- **역할**: 애드온 미션 목록 스크롤 표시 및 미션 선택 처리
- **주요 필드/프로퍼티**:
  - `disabledColor`(Color32) — 비활성화된 버튼의 그림자 색상
  - `itemScroll`(MissionItemScroll) — 연결된 스크롤바 컴포넌트
  - `m_maxItemIndex`(int) — `m_data.Count - m_itemCount`
  - `m_topIndex`(int) — 현재 목록 상단 인덱스
  - `m_data`(List\<AddonMissionData\>) — DataManager에서 가져온 애드온 미션 데이터
- **주요 메서드**:
  - `Start()` — m_itemCount를 `Min(m_data.Count, 8)`로 설정 후 base.Start(). `MainmenuScene.AddonScrollIndex`로 초기 위치 복원
  - `ScrollToIndex(int)`, `SetButtonState(...)`, `UpButtonClicked()`, `DownButtonClicked()`, `MissionItemClicked(int)` — OfficialMissionItemController와 구조 동일
  - `MissionItemClicked(int index)` — `mainmenuScene.Load(m_topIndex + index, true)` (mif=true)
- **특이사항**: OfficialMissionItemController와 구현이 거의 동일하며 데이터 타입(`AddonMissionData`)과 `IsAddonTab` 저장(`MainmenuScene.AddonScrollIndex`), mif 인수만 다르다.

---

### MainmenuFadeSequence

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Mainmenu/MainmenuFadeSequence.cs`
- **역할**: 씬 진입 시 화면 페이드 인과 클릭 허용 타이밍 제어
- **주요 필드/프로퍼티**:
  - `fadeRawImage`(FadeRawImage) — 페이드 대상 UI
  - `fadeTime`(float) — 페이드 인 지속 시간, 기본값 2f
  - `clickAllowTime`(float) — 클릭 허용까지 대기 시간, 기본값 0.2f
- **주요 메서드**:
  - `Start()` — FadeRoutine, ClickAllowRoutine 두 코루틴 동시 시작
  - `FadeRoutine()` — 알파1 설정 → 즉시 FadeIn(fadeTime)
  - `ClickAllowRoutine()` — clickAllowTime 대기 후 fadeRawImage의 RawImage.raycastTarget = false
- **특이사항**: FadeRoutine의 `WaitUntil(() => Time.time - m_time >= 0)`은 항상 즉시 통과한다.

---

### MissionSwitch

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Mainmenu/MissionSwitch.cs`
- **역할**: 공식/애드온 미션 탭 전환 버튼 생성 및 탭 전환 처리
- **주요 필드/프로퍼티**:
  - `officialMissionCanvas`, `addonMissionCanvas`(GameObject) — 각 탭의 캔버스
  - `officialSwitchTransform`, `addonSwitchTransform`(Transform) — 탭 전환 버튼이 생성될 루트
  - `toAddonButton`, `toOfficialButton`(XOPSSpriteTextButton) — 동적 생성된 탭 전환 버튼
  - `k_toAddonText`(const string) — `"ADD-ON MISSIONS >>"`
  - `k_toOfficialText`(const string) — `"<< STANDARD MISSIONS"`
- **주요 메서드**:
  - `Start()` — 두 버튼 텍스트(그림자+버튼) 생성, 이벤트 등록. addonSwitchTransform 비활성화. `MainmenuScene.IsAddonTab`이 true이면 ToAddonSwitchClicked 즉시 호출
  - `ToAddonSwitchClicked()` — `IsAddonTab = true`, officialCanvas 비활성화, addonCanvas 활성화
  - `ToOfficialTextClicked()` — `IsAddonTab = false`, officialCanvas 활성화, addonCanvas 비활성화
- **특이사항**: `MainmenuScene.IsAddonTab` static 필드로 씬 재진입 시 마지막 탭 상태를 복원한다.

---

### ExitConfirmPopup

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Mainmenu/ExitConfirmPopup.cs`
- **역할**: 게임 종료 확인 팝업 UI 구성 및 QUIT/ABORT 버튼 처리
- **주요 필드/프로퍼티**:
  - `root`(Transform) — 팝업 전체 루트 (ABORT 시 비활성화 대상)
  - `labelRoot`, `quitRoot`, `abortRoot`(Transform) — 텍스트 생성 위치
  - `normalColor`, `hoverColor`, `pressedColor`, `shadowColor`(Color32) — 버튼 색상
  - `labelSize`, `buttonSize`(Vector2) — 레이블/버튼 폰트 크기
  - `k_label`(const string) — `"Do you want to quit the game?"`
  - `k_quitText`(const string) — `"QUIT"`
  - `k_abortText`(const string) — `"ABORT"`
- **주요 메서드**:
  - `Start()` — 레이블, QUIT, ABORT 버튼을 각각 그림자+버튼 구조로 생성
  - `OnQuitButtonPressed()` — `#if UNITY_EDITOR` 분기: 에디터는 `EditorApplication.isPlaying = false`, 빌드는 `Application.Quit()`
  - `OnAbortButtonPressed()` — `root.gameObject.SetActive(false)`
- **특이사항**: 버튼 가로 크기를 `labelSize.x * text.Length`로 계산한다.
