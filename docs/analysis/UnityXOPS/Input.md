# Input

## 개요
Unity Input System 기반 입력 처리 레이어. `InputBindingData`가 JSON 직렬화 구조체로 바인딩 문자열을 보유하고, `InputManager`가 런타임에 이를 읽어 `InputActionMap`을 동적으로 구성한다. 모든 게임 액션은 `InputManager`의 정적 `Instance`를 통해 전역 접근된다.

## 클래스 목록

### InputBindingData
- **파일**: `Assets/UnityXOPS/Runtime/Input/InputBindingData.cs`
- **역할**: `StreamingAssets/unitydata/input_bindings.json`을 `JsonUtility.FromJson`으로 역직렬화하기 위한 데이터 클래스
- **주요 필드/프로퍼티**:
  - `look`(string) — Look 액션 마우스 델타 바인딩 경로
  - `lookUp`, `lookDown`, `lookLeft`, `lookRight`(string) — Look 2DVector 컴포짓 바인딩 경로
  - `moveForward`, `moveBackward`, `moveLeft`, `moveRight`(string) — Move 2DVector 컴포짓 바인딩 경로
  - `jump`, `walk`, `drop`, `fire`, `zoom`(string) — 버튼 액션 바인딩 경로
  - `previous`, `next`, `reload`, `first`, `second`, `interact`(string) — 버튼 액션 바인딩 경로
- **특이사항**: `[Serializable]` 어트리뷰트로 `JsonUtility` 직렬화 지원. 모든 필드는 Input System 경로 문자열 (예: `"<Keyboard>/w"`)

---

### InputManager
- **파일**: `Assets/UnityXOPS/Runtime/Input/InputManager.cs`
- **역할**: JSON 바인딩을 읽어 `InputActionMap`을 동적 생성하고 13개 게임 액션을 전역 노출하는 싱글톤 매니저. 마우스 커서 제어 기능도 포함
- **주요 필드/프로퍼티**:
  - `Look`(InputAction) — `PassThrough` 타입; 마우스 델타 + 방향키 2DVector 컴포짓 복합 바인딩
  - `Move`(InputAction) — `Value` 타입; WASD 2DVector 컴포짓 바인딩
  - `Jump`, `Walk`, `Drop`, `Fire`, `Zoom`(InputAction) — 단일 버튼 액션
  - `Previous`, `Next`, `Reload`, `First`, `Second`, `Interact`(InputAction) — 단일 버튼 액션
  - `Keyboard`(static Keyboard) — `Keyboard.current` 캐싱
  - `Mouse`(static Mouse) — `Mouse.current` 캐싱
  - `m_map`(InputActionMap) — 이름 `"XOPS"`의 액션 맵
  - `m_hideInWindow`(bool) — 창 내부에 마우스가 있을 때만 커서 숨기는 모드 플래그
  - `k_bindingsPath`(const string) — `"unitydata/input_bindings.json"`
- **주요 메서드**:
  - `Start()` — JSON 로드 → `InputBindingData` 역직렬화 → `m_map` 생성 → 13개 액션 등록 → `m_map.Enable()`
  - `Update()` — `m_hideInWindow`가 true일 때 마우스 위치로 창 내부 여부 판단해 커서 표시 여부 갱신; `#if UNITY_EDITOR`에서 `UpdateDebugValues()` 호출
  - `MouseCursorMode(bool hideInWindow, bool centered, bool moveToCenter)` — 커서 표시/잠금/위치 이동을 일괄 설정하는 정적 메서드
  - `OnDestroy()` — `m_map.Disable()` + `m_map.Dispose()` 정리
  - `UpdateDebugValues()` — `#if UNITY_EDITOR` 블록; 인스펙터 실시간 모니터링용 직렬화 필드 갱신
- **특이사항**:
  - `Look` 액션은 `AddBinding(binding.look)` (마우스 델타)와 `AddCompositeBinding("2DVector")` (방향키) 두 바인딩을 동시 등록
  - 에디터 전용 디버그 필드 13개(`lookValue`, `moveValue`, `jumpValue` 등)를 `[SerializeField]`로 직렬화해 인스펙터에서 실시간 확인 가능; 빌드에서는 `#if UNITY_EDITOR`로 완전 제거
  - `m_hideInWindow` 모드: `Update()`에서 매 프레임 마우스 위치로 `Cursor.visible` 직접 제어 (Screen 범위 체크)
  - `MouseCursorMode`에서 `moveToCenter`가 true이면 `Mouse.WarpCursorPosition`으로 화면 중앙 강제 이동
