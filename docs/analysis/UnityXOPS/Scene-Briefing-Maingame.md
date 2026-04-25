# Briefing + Maingame 씬

## 개요

### Briefing 씬 (씬 인덱스 3)
Mainmenu에서 선택한 미션의 이미지·전체 이름·브리핑 텍스트를 표시한다. ESC 입력 시 미션 데이터를 해제하고 Mainmenu(2)로 복귀하고, 좌클릭 시 Maingame(4)으로 진행한다.

### Maingame 씬 (씬 인덱스 4)
실제 게임 씬. 현재는 ESC 입력 시 맵·미션 데이터를 해제하고 Mainmenu(2)로 복귀하는 기본 흐름만 구현되어 있다.

**씬 전환 흐름**: Mainmenu(2) → Briefing(3) → Maingame(4), ESC 시 Briefing(3) → Mainmenu(2), ESC 시 Maingame(4) → Mainmenu(2)

---

## 클래스 목록

### BriefingScene

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Briefing/BriefingScene.cs`
- **역할**: 브리핑 씬 진입/종료 처리 및 씬 전환 관리
- **주요 메서드**:
  - `Start()` — `InputManager.MouseCursorMode(true, false, false)` (커서 표시, 비잠금, 비숨김)
  - `Update()` — ESC: 맵/미션 데이터 전체 언로드 후 SceneManager.LoadScene(2). 좌클릭: SceneManager.LoadScene(4)
- **특이사항**:
  - ESC 처리 시 `MapLoader.UnloadBlockData()`, `UnloadPointData()`, `UnloadMissionData()`, `UnloadSkyData()` 4개를 모두 호출한다.
  - 좌클릭 전환 시에는 맵 데이터를 언로드하지 않는다 (Maingame 씬에서 그대로 사용).

---

### BriefingBackground

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Briefing/BriefingBackground.cs`
- **역할**: `data/title.dds`를 로드하여 브리핑 배경 타이틀 이미지로 표시
- **주요 필드/프로퍼티**:
  - `titleImage`(RawImage) — 타이틀 이미지 대상 RawImage
- **주요 메서드**:
  - `Start()` — `ImageLoader.LoadTexture(StreamingAssets/data/title.dds)`로 텍스처 로드 후 titleImage.texture에 적용

---

### BriefingContent

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Briefing/BriefingContent.cs`
- **역할**: 미션 이미지 수에 따라 단일/이중 레이아웃을 선택하고, 미션 전체 이름과 브리핑 텍스트를 표시
- **주요 필드/프로퍼티**:
  - `singleImage`(RawImage) — 단일 이미지 레이아웃의 이미지
  - `doubleFirstImage`, `doubleSecondImage`(RawImage) — 이중 이미지 레이아웃의 이미지
  - `singleObject`, `doubleObject`(GameObject) — 레이아웃 전환 대상 루트 오브젝트
  - `fullnameRoot`(Transform) — 미션 전체 이름 텍스트 생성 위치
  - `fullnameFontSize`(Vector2) — 전체 이름 텍스트 폰트 크기
  - `fullnameColor`(Color32) — 전체 이름 텍스트 색상
  - `textArea`(TMP_Text) — 브리핑 텍스트 표시용 TextMeshPro 컴포넌트
- **주요 메서드**:
  - `Start()` — `MapLoader.Instance.MissionImage1`이 비어있으면 단일 레이아웃, 아니면 이중 레이아웃 활성화. 이미지 텍스처 로드 적용. `FontManager.CreateSpriteText<XOPSSpriteText>`로 전체 이름 생성. `textArea.font = FontManager.OSFont`, `textArea.text = MapLoader.Instance.MissionBriefing`
- **특이사항**:
  - 미션 이미지 경로는 `MapLoader.Instance.MissionImage0`, `MissionImage1`에서 읽는다.
  - 브리핑 텍스트는 XOPSSpriteText가 아닌 TMP_Text를 사용하며, OSFont를 직접 할당한다.

---

### BriefingPulseTextSequence

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Briefing/BriefingPulseTextSequence.cs`
- **역할**: "BRIEFING" 텍스트 펄스와 "LEFT CLICK TO BEGIN" 스프레드 펄스 애니메이션 실행
- **주요 필드/프로퍼티**:
  - `briefingRoot`(RectTransform) — "BRIEFING" 텍스트 생성 위치
  - `clickToNextRoot`(RectTransform) — "LEFT CLICK TO BEGIN" 텍스트 생성 위치
  - `briefingFontSize`(Vector2) — BRIEFING 텍스트 폰트 크기
  - `clickToNextStartFontSize`, `clickToNextEndFontSize`(Vector2) — 클릭 유도 텍스트 시작/종료 폰트 크기
  - `briefingColor`, `clickToNextColor`(Color32) — 각 텍스트 색상
  - `briefingDuration`(float), `briefingStartAlpha`(float), `briefingEndAlpha`(float) — BRIEFING 펄스 파라미터
  - `clickToNextDuration`(float), `clickToNextStartAlpha`(float), `clickToNextEndAlpha`(float) — 클릭 유도 텍스트 펄스 파라미터
- **주요 메서드**:
  - `Start()` — XOPSSpriteTextPulse 1개, XOPSSpriteText 1개(고정 표시), XOPSSpriteTextPulseSpread 1개 생성 후 PulseRoutine, PulseSpreadRoutine 코루틴 시작
  - `PulseRoutine(XOPSSpriteTextPulse)` — 1프레임 대기 후 `text.StartPulse(duration, startAlpha, endAlpha)`
  - `PulseSpreadRoutine(XOPSSpriteTextPulseSpread)` — 1프레임 대기 후 `text.StartPulseSpread(duration, startAlpha, endAlpha, startW, startH, endW, endH)`
- **특이사항**:
  - "LEFT CLICK TO BEGIN"은 XOPSSpriteText(고정)와 XOPSSpriteTextPulseSpread(애니메이션) 두 레이어를 겹쳐서 같은 위치에 생성한다.
  - 코루틴에서 `yield return null` 1프레임 대기 후 애니메이션을 시작하는 이유는 FontManager.CreateSpriteText 내부의 초기화 완료를 보장하기 위함으로 보인다.

---

### MaingameScene

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Maingame/MaingameScene.cs`
- **역할**: 메인게임 씬 진입/종료 처리 및 ESC 입력 시 Mainmenu 복귀
- **주요 메서드**:
  - `Start()` — `InputManager.MouseCursorMode(true, true, true)` (커서 표시, 잠금, 숨김)
  - `Update()` — ESC: 맵/스카이/미션 데이터 언로드 후 SceneManager.LoadScene(2). F12: 현재 빈 블록 (미구현)
- **특이사항**:
  - F12 키 처리 블록이 존재하나 내부 로직이 없다 (미구현 상태).
  - ESC 처리 시 `UnloadBlockData`, `UnloadPointData`, `UnloadSkyData`, `UnloadMissionData` 4개를 모두 호출한다.

---

### MaingameFadeSequence

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Maingame/MaingameFadeSequence.cs`
- **역할**: 씬 진입 시 페이드 인, 외부 호출로 페이드 아웃 실행
- **주요 필드/프로퍼티**:
  - `fadeRawImage`(FadeRawImage) — 페이드 대상 UI
  - `fadeInTime`(float) — 페이드 인 지속 시간, 기본값 1f
  - `fadeOutTime`(float) — 페이드 아웃 지속 시간, 기본값 1f
- **주요 메서드**:
  - `Start()` — FadeInRoutine 코루틴 시작
  - `FadeInRoutine()` — 알파1 설정 → 1프레임 대기 → `FadeIn(fadeInTime)`
  - `FadeOut()` — 외부에서 호출하는 public 메서드, `FadeOut(fadeOutTime)`
- **특이사항**: `FadeOut()`은 public으로 노출되어 있으나 현재 MaingameScene에서 호출하는 코드가 없다 (추후 게임 종료/씬 전환 시 활용 예정으로 보임).
