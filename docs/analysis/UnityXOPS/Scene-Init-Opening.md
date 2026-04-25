# Init + Opening 씬

## 개요

### Init 씬 (씬 인덱스 0)
모든 매니저(ImageLoader, ModelLoader, MapLoader, DataManager, FontManager, InputManager)의 로딩 완료를 폴링하고, 완료되면 씬 인덱스 1(Opening)으로 자동 전환한다.

### Opening 씬 (씬 인덱스 1)
StreamingAssets의 JSON 설정(`unitydata/opening_data.json`)을 읽어 맵 데이터와 스카이를 로드한 뒤, 카메라 이동/회전, 페이드 인·아웃, 텍스트 페이드, 레터박스 UI를 시퀀스 재생한다. 종료 조건(시간 초과 또는 ESC/좌클릭)이 되면 맵 데이터를 해제하고 씬 인덱스 2(Mainmenu)로 전환한다.

**씬 전환 흐름**: Init(0) → Opening(1) → Mainmenu(2)

---

## 클래스 목록

### InitScene

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Init/InitScene.cs`
- **역할**: 모든 매니저 로드 완료를 매 프레임 확인하고, 완료 시 씬 인덱스 1로 전환
- **주요 메서드**:
  - `Update()` — ImageLoader.Loaded, ModelLoader.Loaded, MapLoader.Loaded, DataManager.Loaded, FontManager.Loaded, InputManager.Loaded 6개 조건이 모두 true일 때 `SceneManager.LoadScene(1)` 호출
- **특이사항**: 폴링 방식으로 로드 완료를 감지. 이벤트 콜백 없이 Update에서만 처리.

---

### OpeningScene

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Opening/OpeningScene.cs`
- **역할**: 오프닝 데이터 로드 및 맵 초기화, 종료 조건 처리, 씬 전환 관리
- **주요 필드/프로퍼티**:
  - `openingData`(OpeningData) — 인스펙터 직렬화 + Start에서 JSON으로 덮어씌워짐
  - `OpeningData`(OpeningData, get) — 동일 게임오브젝트의 다른 컴포넌트들이 데이터 참조에 사용
  - `m_time`(float) — Start 시각 기록용 Time.time 스냅샷
  - `m_endTime`(float) — `openingFadeData.fadeOutEnd + 1.1f` 로 계산된 씬 종료 기준 시각
  - `k_openingDataPath`(const string) — `"unitydata/opening_data.json"`
- **주요 메서드**:
  - `Start()` — JSON 로드, 맵/스카이 데이터 로드, 마우스 커서 숨김·잠금
  - `Update()` — 경과 시간 초과 또는 ESC/좌클릭 입력 시 맵 언로드 후 SceneManager.LoadScene(2) 호출
- **특이사항**:
  - `openingData`의 bd1/pd1 경로는 JSON 로드 후 `SafePath.Combine(StreamingAssets, ...)` 으로 전체 경로로 변환된다.
  - 동일 게임오브젝트에 부착된 OpeningCameraSequence, OpeningFadeSequence, OpeningTextSequence, OpeningLetterBox는 `GetComponent<OpeningScene>().OpeningData`로 데이터를 가져간다.

---

### OpeningData

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Opening/OpeningData.cs`
- **역할**: JSON으로 역직렬화되는 오프닝 씬 전체 설정 데이터 컨테이너
- **주요 필드/프로퍼티**:
  - `openingBD1Path`(string) — BD1 맵 파일 상대 경로 (Start에서 전체 경로로 변환됨)
  - `openingPD1Path`(string) — PD1 포인트 파일 상대 경로 (Start에서 전체 경로로 변환됨)
  - `openingSkyIndex`(int) — 스카이 데이터 인덱스
  - `letterBoxHeight`(float) — 상하 레터박스 높이
  - `openingFadeData`(OpeningFadeData) — 화면 페이드 타이밍
  - `openingTextData`(List\<OpeningTextData\>) — 텍스트 항목 목록
  - `openingCameraData`(OpeningCameraData) — 카메라 초기 위치/회전 및 애니메이션 파라미터

---

### OpeningFadeData

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Opening/OpeningData.cs`
- **역할**: 화면 페이드 인/아웃의 시작·종료 시각 정의
- **주요 필드/프로퍼티**:
  - `fadeInStart`(float), `fadeInEnd`(float) — 페이드 인 구간 (초)
  - `fadeOutStart`(float), `fadeOutEnd`(float) — 페이드 아웃 구간 (초)
- **특이사항**: `fadeOutEnd`는 OpeningScene의 `m_endTime` 계산(`+ 1.1f`)에 사용된다.

---

### OpeningTextData

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Opening/OpeningData.cs`
- **역할**: 개별 텍스트 항목의 레이아웃 및 페이드 타이밍 정의
- **주요 필드/프로퍼티**:
  - `text`(string) — 표시 문자열
  - `position`(Vector2), `size`(Vector2) — UI 배치
  - `color`(Color) — 텍스트 색상
  - `alignment`(TextAnchor) — 정렬
  - `spacing`(float) — 자간
  - `fadeInStart`(float), `fadeInEnd`(float), `fadeOutStart`(float), `fadeOutEnd`(float) — 페이드 타이밍

---

### OpeningCameraData

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Opening/OpeningData.cs`
- **역할**: 카메라 초기 위치·회전 및 이동·회전 애니메이션 파라미터 정의
- **주요 필드/프로퍼티**:
  - `initialPosition`(Vector3) — 카메라 초기 월드 위치
  - `initialEuler`(Vector3) — 카메라 초기 오일러 회전
  - `posAnim`(OpeningCameraAnimation) — 위치 애니메이션 파라미터
  - `rotAnim`(OpeningCameraAnimation) — 회전 애니메이션 파라미터

---

### OpeningCameraAnimation

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Opening/OpeningData.cs`
- **역할**: 가속·등속·감쇠 3구간으로 구성된 카메라 단일 축 애니메이션 파라미터
- **주요 필드/프로퍼티**:
  - `accelStart`(float) — 가속 구간 시작 시각
  - `accelEnd`(float) — 가속 구간 종료 시각
  - `constantEnd`(float) — 등속 구간 종료 시각 (`< 0`이면 무한 등속)
  - `targetAdd`(Vector3) — 목표 속도 벡터
  - `smoothFactor`(float) — 가속/감쇠 지수 평활 계수 (per 33.333fps 기준)

---

### OpeningCameraSequence

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Opening/OpeningCameraSequence.cs`
- **역할**: 매 프레임 카메라의 위치·회전 오프셋을 가속/등속/감쇠 구간에 따라 갱신
- **주요 필드/프로퍼티**:
  - `mainCamera`(Camera) — 인스펙터 직렬화, 조작 대상 카메라
  - `m_addPos`(Vector3) — 현재 프레임의 위치 속도 벡터
  - `m_addEuler`(Vector3) — 현재 프레임의 회전 속도 벡터
  - `m_time`(float) — Start 시각 스냅샷
  - `m_data`(OpeningCameraData) — GetComponent로 가져온 카메라 데이터
- **주요 메서드**:
  - `Start()` — 카메라 초기 위치·회전 적용, 속도 벡터 초기화
  - `Update()` — `UpdateAxis` 호출 후 `transform.position += m_addPos * dt`, `transform.eulerAngles += m_addEuler * dt`
  - `UpdateAxis(ref Vector3, OpeningCameraAnimation, float, float)` — 경과 시간에 따라 3구간(가속·등속·감쇠) 중 하나를 적용
- **특이사항**:
  - 감쇠 계수: `Mathf.Pow(smoothFactor, dt * 33.333f)` — 30fps 기준 프레임 독립적 지수 감쇠
  - 가속 구간: `Vector3.Lerp(targetAdd, add, decay)` (목표값으로 수렴)
  - `constantEnd < 0`이면 등속 구간이 무한 지속

---

### OpeningFadeSequence

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Opening/OpeningFadeSequence.cs`
- **역할**: OpeningFadeData 타이밍에 따라 FadeRawImage로 페이드 인·아웃 코루틴 실행
- **주요 필드/프로퍼티**:
  - `fadeRawImage`(FadeRawImage) — 인스펙터 직렬화, 페이드 대상 UI
  - `m_time`(float) — Start 시각 스냅샷
- **주요 메서드**:
  - `Start()` — OpeningScene에서 데이터 가져와 FadeRoutine 코루틴 시작
  - `FadeRoutine(OpeningFadeData)` — 알파1 설정 → fadeInStart 대기 → FadeIn → fadeOutStart 대기 → FadeOut

---

### OpeningTextSequence

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Opening/OpeningTextSequence.cs`
- **역할**: OpeningTextData 목록을 기반으로 XOPSSpriteTextFade를 생성하고 각각의 페이드 코루틴 실행
- **주요 필드/프로퍼티**:
  - `root`(Transform) — 인스펙터 직렬화, 텍스트 생성 부모 Transform
  - `fadeTexts`(List\<XOPSSpriteTextFade\>) — 생성된 텍스트 컴포넌트 목록
  - `m_openingTextData`(List\<OpeningTextData\>) — OpeningScene에서 가져온 텍스트 데이터
- **주요 메서드**:
  - `Start()` — 데이터 수만큼 FontManager.CreateSpriteText\<XOPSSpriteTextFade\> 생성, 각각 TextRoutine 코루틴 시작
  - `TextRoutine(XOPSSpriteTextFade, OpeningTextData)` — 알파0 설정 → fadeInStart 대기 → FadeIn → fadeOutStart 대기 → FadeOut

---

### OpeningLetterBox

- **파일**: `Assets/UnityXOPS/Runtime/Scene/Opening/OpeningLetterBox.cs`
- **역할**: OpeningData의 `letterBoxHeight` 값을 상단·하단 레터박스 RectTransform의 sizeDelta.y에 적용
- **주요 필드/프로퍼티**:
  - `top`(RectTransform) — 상단 레터박스
  - `bottom`(RectTransform) — 하단 레터박스
- **주요 메서드**:
  - `Start()` — OpeningScene에서 height 읽어 `top.sizeDelta = new Vector2(0, height)`, `bottom.sizeDelta = new Vector2(0, height)` 적용
