# Editor

## 개요
UnityXOPS 전용 에디터 확장 모음. 런타임 컴포넌트의 내부 상태를 인스펙터에서 시각화하거나, 에디터 워크플로우를 개선하는 도구를 제공한다. 네임스페이스는 `UnityXOPSEditor`이며 빌드에 포함되지 않는다.

## 클래스 목록

### FontManagerEditor
- **파일**: `Assets/UnityXOPS/Editor/Manager/FontManagerEditor.cs`
- **역할**: `FontManager` 컴포넌트의 인스펙터를 커스터마이징해 현재 시스템 언어에 맞는 OS 폰트 경로를 표시하고 해당 폴더를 탐색기에서 여는 버튼을 제공
- **주요 필드/프로퍼티**: 없음
- **주요 메서드**:
  - `OnInspectorGUI()` — 시스템 언어에 따라 `m_koreanOSFontPath` / `m_japaneseOSFontPath` / `m_englishOSFontPath` 중 해당 private 필드를 리플렉션(`BindingFlags.NonPublic | BindingFlags.Instance`)으로 읽어 경로 표시; 경로가 비어있으면 버튼 비활성화
- **특이사항**:
  - 에디터 플레이 모드 전에는 `FontManager.Start()`가 실행되지 않으므로 경로가 null → `"(The path is not displayed in editor mode.)"` 출력
  - `EditorUtility.RevealInFinder`로 폴더 열기

---

### InputManagerEditor
- **파일**: `Assets/UnityXOPS/Editor/Manager/InputManagerEditor.cs`
- **역할**: `InputManager` 컴포넌트의 인스펙터를 커스터마이징해 Look/Move 벡터를 2D 박스로 시각화하고 13개 버튼 입력 상태를 색상 라벨로 실시간 표시
- **주요 필드/프로퍼티**:
  - `m_boolStyle`(GUIStyle) — 버튼 상태 라벨용 스타일 (null 체크 후 지연 초기화)
- **주요 메서드**:
  - `RequiresConstantRepaint()` — 플레이 중(`Application.isPlaying`)이면 true 반환 → 매 프레임 인스펙터 갱신
  - `OnInspectorGUI()` — Look/Move 2D 시각화 → 버튼 상태 3행으로 배치 렌더링
  - `DrawVector2Visualizer(string label, Vector2 value, Color dotColor)` — 80×80 픽셀 박스에 1px 테두리 그리고, 입력 벡터를 정규화해 점으로 표시; Y축은 `dotY = centerY - normalized.y * ...`로 반전
  - `DrawBoolLabel(string label, bool value)` — 60×22 픽셀 박스; `value`가 true이면 초록, false이면 검정 아웃라인; 다크/라이트 모드 대응 박스·텍스트 색상 분기
- **특이사항**:
  - `InputManager`의 `#if UNITY_EDITOR` 직렬화 필드를 `serializedObject.FindProperty`로 읽어 표시 — 빌드에서는 해당 필드 자체가 없으므로 에디터에서만 동작
  - `Handles.DrawSolidDisc`로 점 렌더링 (`Handles.BeginGUI` / `EndGUI` 블록 내)
  - Look은 빨간색 점, Move는 검은색 점

---

### MapLoaderEditor
- **파일**: `Assets/UnityXOPS/Editor/Loader/MapLoaderEditor.cs`
- **역할**: `MapLoader` 컴포넌트의 인스펙터를 커스터마이징해 편집 모드에서는 레퍼런스 필드만, 플레이 모드에서는 블록·포인트·미션 데이터를 상세 표시
- **주요 필드/프로퍼티**:
  - `m_blockRoot`, `m_humanRoot`, `m_humanPrefab`(SerializedProperty) — 편집 모드 레퍼런스 필드
  - `m_blockCount`, `m_blockMaterials`, `m_blockColliders`(SerializedProperty) — 블록 데이터
  - `m_pointCount`, `m_humanCount`, `m_weaponCount`, `m_objectCount`, `m_messages`(SerializedProperty) — 포인트 데이터
  - `m_missionName`, `m_missionFullname`, `m_missionBD1Path`, `m_missionPD1Path`, `m_missionAddonObjectPath`, `m_missionImage0`, `m_missionImage1`, `m_skyIndex`, `m_missionBriefing`, `m_adjustCollision`, `m_darkScreen`(SerializedProperty) — 미션 데이터
  - `m_missionFoldout`(bool) — 미션 Details 섹션 접힘 상태, 기본값 true
  - `m_wrapStyle`(GUIStyle) — 줄바꿈 라벨용 스타일 (null 체크 후 지연 초기화)
- **주요 메서드**:
  - `OnEnable()` — `serializedObject.FindProperty`로 모든 SerializedProperty 초기화
  - `OnInspectorGUI()` — `EditorApplication.isPlaying` 분기; 플레이 중이면 `DrawBlockData` → `DrawPointData` → `DrawMissionData` 순서로 렌더링, 편집 중이면 레퍼런스 3개 필드만 표시
  - `DrawBlockData()` — 블록 수, 머티리얼 배열, 콜라이더 배열 표시
  - `DrawPointData()` — 포인트/휴먼/무기/오브젝트 수 및 메시지 배열 표시
  - `DrawMissionData()` — 미션 이름, Details 폴더아웃(BD1/PD1/AddonObject 경로, 스카이 인덱스, 이미지, AdjustCollision/DarkScreen 토글), 브리핑 텍스트 표시
- **특이사항**:
  - 브리핑 텍스트는 `GUIStyle.CalcHeight`로 실제 높이를 계산한 뒤 `GUILayout.Height(briefingHeight)`를 지정해 다중 줄 표시
  - `AdjustCollision`, `DarkScreen` 토글은 `EditorGUI.BeginDisabledGroup(true)`로 읽기 전용 표시

---

### PlayFromFirstScene
- **파일**: `Assets/UnityXOPS/Editor/PlayFromFirstScene.cs`
- **역할**: `UnityXOPS/Play From First Scene` 메뉴로 토글 가능한 에디터 유틸리티. 활성화 시 플레이 버튼을 누르면 현재 씬이 아닌 Build Settings 0번 씬에서 항상 게임이 시작되고, 종료 후 원래 씬이 자동 복원됨
- **주요 필드/프로퍼티**:
  - `k_menuPath`(const string) — `"UnityXOPS/Play From First Scene"`
  - `k_prefKey`(const string) — `"UnityXOPS.PlayFromFirstScene"` (EditorPrefs 키)
  - `k_prevScenePrefKey`(const string) — `"UnityXOPS.PlayFromFirstScene.PrevScene"` (이전 씬 경로 저장용 EditorPrefs 키)
  - `IsEnabled`(static bool) — `EditorPrefs.GetBool(k_prefKey, false)` 래핑 프로퍼티
- **주요 메서드**:
  - 생성자 — `EditorApplication.playModeStateChanged += OnPlayModeStateChanged` 등록 (`[InitializeOnLoad]`)
  - `Toggle()` — `[MenuItem]`; IsEnabled 값 반전 후 체크 상태 갱신
  - `ToggleValidate()` — `[MenuItem]` validate; 메뉴 렌더링마다 체크 상태 동기화
  - `OnPlayModeStateChanged(PlayModeStateChange state)` — `ExitingEditMode`일 때 현재 씬 경로를 EditorPrefs에 저장하고 0번 씬으로 전환; `EnteredEditMode`일 때 저장된 경로로 씬 복원
- **특이사항**:
  - 이미 0번 씬에서 플레이하면 `k_prevScenePrefKey`를 삭제하고 씬 전환 없이 진행
  - 씬 저장 팝업(`SaveCurrentModifiedScenesIfUserWantsTo`)에서 사용자가 취소하면 `EditorApplication.isPlaying = false`로 플레이 시작 자체를 중단
  - EditorPrefs를 통해 도메인 리로드 후에도 상태 유지
