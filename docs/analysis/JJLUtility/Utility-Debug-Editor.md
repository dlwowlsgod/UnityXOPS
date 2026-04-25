# Utility / Debug / Editor

## 개요
JJLUtility의 공통 유틸리티, 디버그, 에디터 확장 모음이다. `SingletonBehavior`는 모든 IO 로더 싱글톤의 베이스 클래스이며, `BitReader`·`EncodingHelper`·`SafePath`는 IO 파서 내부에서 사용되는 저수준 헬퍼다. `Debugger`는 에디터 전용 로그 출력 유틸리티이며, Editor 디렉터리 하위 클래스들은 각 IO 싱글톤의 인스펙터 UI를 확장한다.

---

## 클래스 목록

### SingletonBehavior\<T\>
- **파일**: `Assets/JJLUtility/Runtime/Utility/SingletonBehavior.cs`
- **역할**: `MonoBehaviour` 기반 제네릭 싱글톤 베이스 클래스. `ImageLoader`, `ModelLoader`, `SoundLoader`가 상속한다.
- **주요 필드/프로퍼티**:
  - `m_instance`(static T) — 싱글톤 인스턴스 캐시
  - `m_isApplicationQuitting`(static bool) — 종료 중 플래그
  - `Instance`(static T, 프로퍼티) — 인스턴스 접근자. 없으면 씬에서 검색 후 없으면 새 GameObject 생성
  - `Loaded`(static bool, 프로퍼티) — `m_instance != null` 여부
- **주요 메서드**:
  - `Awake()` (virtual) — 중복 인스턴스 자신 파괴, `DontDestroyOnLoad` 등록
  - `OnApplicationQuit()` (virtual) — `m_isApplicationQuitting = true` 설정
- **특이사항**:
  - `Instance` 접근 시 `m_isApplicationQuitting`이면 `null` 반환 (종료 시 새 인스턴스 생성 방지)
  - 씬에 중복 인스턴스가 2개 이상이면 index 1 이후를 모두 `Destroy`

---

### BitReader
- **파일**: `Assets/JJLUtility/Runtime/Utility/BitReader.cs`
- **역할**: `BinaryReader`를 상속하여 비트 단위 읽기 기능을 추가한다. BMP 팔레트 인덱스(1/4bpp) 파싱에서 사용된다.
- **주요 필드/프로퍼티**:
  - `m_currentData`(byte) — 현재 버퍼링된 바이트
  - `m_currentBit`(int) — 남은 비트 수
- **주요 메서드**:
  - `ReadBit()` → `byte` — 비트 1개 읽기 (MSB 먼저). 버퍼 소진 시 다음 바이트 읽음
  - `ReadBits(int count)` → `ulong` — 최대 32비트 읽기. MSB 먼저 조합하여 반환
  - `ResetBitBuffer()` — 비트 버퍼 초기화 (BMP 행 패딩 처리에서 호출)
- **특이사항**:
  - 네임스페이스는 `System.IO` (JJLUtility가 아님)
  - `ReadBits` count 범위는 내부적으로 1~32로 클램핑됨
  - `ResetBitBuffer` 호출 후 다음 `ReadBit`은 새 바이트부터 읽음 → BMP 행 경계 정렬에 사용

---

### EncodingHelper
- **파일**: `Assets/JJLUtility/Runtime/Utility/EncodingHelper.cs`
- **역할**: 시스템 언어에 맞는 멀티바이트 인코딩을 반환하는 정적 유틸리티
- **주요 메서드**:
  - `GetEncoding()` → `Encoding` — 한국어: CP949(949), 일본어: Shift-JIS(932), 기타: Windows-1252(1252)

---

### SafePath
- **파일**: `Assets/JJLUtility/Runtime/Utility/SafePath.cs`
- **역할**: 경로 탈출 공격(Path Traversal) 방지 경로 결합 유틸리티
- **주요 메서드**:
  - `Combine(string root, params string[] paths)` → `string` — 결합된 전체 경로가 `root` 하위에 있는지 검증. 탈출 감지 시 `null` 반환
- **특이사항**:
  - Windows(`\`)는 `OrdinalIgnoreCase`, 유닉스(`/`)는 `Ordinal` 비교
  - `root` 끝에 디렉터리 구분자가 없으면 자동 추가 후 `StartsWith` 검사

---

### Debugger (Runtime)
- **파일**: `Assets/JJLUtility/Runtime/Debug/Debugger.cs`
- **역할**: `[Conditional("UNITY_EDITOR")]`로 빌드에서 완전히 제거되는 에디터 전용 로그 유틸리티
- **주요 메서드**:
  - `Log(object message, Object context, string label)` — `Debug.Log` 출력
  - `LogWarning(object message, Object context, string label)` — `Debug.LogWarning` 출력
  - `LogError(object message, Object context, string label)` — `Debug.LogError` 출력
- **특이사항**:
  - `partial class` 선언 — `DebuggerEditor.cs`의 에디터 파트와 분리
  - 기본 `label`은 `"UnityXOPS"`, 출력 형식: `[{label}] {message}`
  - `[Conditional("UNITY_EDITOR")]` 적용으로 빌드 바이너리에서 호출 코드 자체가 제거됨

---

### Debugger (Editor)
- **파일**: `Assets/JJLUtility/Editor/Debug/DebuggerEditor.cs`
- **역할**: 에디터 콘솔에서 로그를 클릭 시 실제 호출 소스 파일을 여는 에디터 확장
- **주요 메서드**:
  - `OnOpenLog(int instanceID)` → `bool` — `[OnOpenAsset]` 콜백. 클릭된 오브젝트 이름이 "Debug"이면 스택 트레이스를 파싱해 실제 호출 위치 파일을 열어줌
- **특이사항**:
  - Reflection으로 Unity 내부 `ConsoleWindow.ms_ConsoleWindow`, `ConsoleWindow.m_ActiveText` 필드 접근
  - 스택 트레이스에서 `(at 파일경로:줄번호)` 정규식 파싱. 첫 번째 매칭(Debugger.cs 자신)은 건너뛰고 두 번째 매칭 사용
  - `InternalEditorUtility.OpenFileAtLineExternal`로 파일 오픈

---

### ImageLoaderEditor
- **파일**: `Assets/JJLUtility/Editor/IO/ImageLoaderEditor.cs`
- **역할**: `ImageLoader` 컴포넌트의 텍스처 캐시 목록을 인스펙터에서 시각화하는 `CustomEditor`
- **주요 메서드**:
  - `OnEnable()` — `textureCacheList` 직렬화 프로퍼티 참조 캐싱
  - `OnInspectorGUI()` — 배열 원소를 `[0]`, `[1]`... 레이블로 열거, 빈 경우 "No cached textures." 표시

---

### ModelLoaderEditor
- **파일**: `Assets/JJLUtility/Editor/IO/ModelLoaderEditor.cs`
- **역할**: `ModelLoader` 컴포넌트의 메시 캐시 목록을 인스펙터에서 시각화하는 `CustomEditor`
- **주요 메서드**:
  - `OnEnable()` — `meshCacheList` 직렬화 프로퍼티 참조 캐싱
  - `OnInspectorGUI()` — 배열 원소를 `[0]`, `[1]`... 레이블로 열거, 빈 경우 "No cached meshes." 표시

---

### SoundLoaderEditor
- **파일**: `Assets/JJLUtility/Editor/IO/SoundLoaderEditor.cs`
- **역할**: `SoundLoader` 컴포넌트의 오디오 캐시 목록을 인스펙터에서 시각화하는 `CustomEditor`
- **주요 메서드**:
  - `OnEnable()` — `audioCacheList` 직렬화 프로퍼티 참조 캐싱
  - `OnInspectorGUI()` — 배열 원소를 `[0]`, `[1]`... 레이블로 열거, 빈 경우 "No cached audio clips." 표시
