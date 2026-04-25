# IO/Model

## 개요
DirectX `.x` 텍스트 포맷 3D 모델 파일을 런타임에서 파싱하여 Unity `Mesh`로 변환하는 모듈이다. `ModelLoader` 싱글톤이 진입점이며 `.x` 파서 구현은 `XFile.cs` partial로 분리된다. 여러 Mesh 블록을 하나의 Unity Mesh로 병합하고 노말/탄젠트/바운드를 자동 재계산한다.

---

## 클래스 목록

### ModelLoader
- **파일**: `Assets/JJLUtility/Runtime/IO/Model/ModelLoader.cs`
- **역할**: 3D 메시 파일 로드 진입점, 메시 캐시 관리 싱글톤
- **주요 필드/프로퍼티**:
  - `m_meshCache`(Dictionary\<string, int\> / Dictionary\<string, Mesh\>) — 경로 → 캐시 인덱스(에디터) / 경로 → Mesh(빌드)
  - `meshCacheList`(List\<Mesh\>, `[SerializeField]`, 에디터 전용) — 인스펙터 직렬화용 메시 목록
- **주요 메서드**:
  - `LoadMesh(string filepath)` → `Mesh` — 확장자 검사 후 `.x` 파서 호출, 캐시 히트 시 즉시 반환
  - `BuildMeshFromXFile(XFile xFile, string meshName)` → `Mesh` — XFile의 모든 XMeshData를 하나의 Mesh로 병합
- **특이사항**:
  - 현재 지원 확장자는 `.x` 단독
  - 정점 수 65535 초과 시 `IndexFormat.UInt32` 자동 적용
  - UV가 정점 수와 일치할 때만 `mesh.SetUVs(0, ...)` 호출
  - 에디터/빌드 캐시 이중 구조는 `ImageLoader`, `SoundLoader`와 동일 패턴

---

### XFile
- **파일**: `Assets/JJLUtility/Runtime/IO/Model/X/XFile.cs`
- **역할**: 파싱된 `.x` 파일 전체 컨테이너 + `ModelLoader` partial 구현 (파서 전체 포함)
- **주요 필드/프로퍼티**:
  - `Meshes`(List\<XMeshData\>) — 파일에서 파싱된 모든 메시 목록
- **내부 클래스**:
  - `XTokenizer` — `.x` 텍스트 토크나이저
    - `Read()` → `string` — 다음 토큰 반환 (위치 전진)
    - `Peek()` → `string` — 다음 토큰 미리 보기 (위치 유지)
    - `ReadFloat()` → `float`, `ReadInt()` → `int` — 토큰 파싱 단축 메서드
    - `SkipBlock()` — 현재 `{` 블록의 `}` 까지 건너뜀 (깊이 추적)
    - `SkipSeparators()` — 공백·구분자(`;` `,`)·`//` 주석·`#` 주석 건너뜀
- **주요 메서드** (partial ModelLoader 내):
  - `LoadXFile(string filepath)` → `XFile` — 파일 읽기, 헤더 16바이트 슬라이스 후 `ParseTopLevel` 호출
  - `ParseTopLevel(XTokenizer, XFile)` — `template`, `Header`, `Frame`, `Mesh` 블록 분기
  - `ParseFrame(XTokenizer, XFile)` — `Frame` 블록 재귀 파싱 (중첩 Frame 지원)
  - `ParseMesh(XTokenizer)` → `XMeshData` — 정점·면·자식 블록 파싱
  - `ParseMeshTextureCoords(XTokenizer, XMeshData)` — UV 파싱, V 축 변환 적용
  - `SkipOptionalName(XTokenizer)` — 블록 앞의 선택적 이름 토큰 건너뜀
- **특이사항**:
  - 헤더는 정확히 16바이트 슬라이스: `"xof " + version(4) + format(3) + " " + floatsize(4)`
  - `;` `,` 모두 구분자 취급(공백과 동일)
  - GUID `<xxxxxxxx-...>` 패턴 자동 건너뜀
  - 면 데이터는 쿼드/삼각형 혼합 가능 — fan 삼각화로 인덱스 생성: `(idx[0], idx[j], idx[j+1])`
  - DirectX와 Unity 모두 왼손 좌표계 + CW = 앞면이므로 **와인딩 반전 불필요**
  - UV V 축 변환: `v = 1f - v` (DirectX V=0 상단 → Unity V=0 하단)
  - `MeshNormals`, `MeshMaterialList` 등 나머지 자식 블록은 모두 건너뜀

---

### XMeshData
- **파일**: `Assets/JJLUtility/Runtime/IO/Model/X/XMeshData.cs`
- **역할**: `.x` 파일 단일 Mesh 블록의 정점·인덱스·UV 데이터 컨테이너
- **주요 필드/프로퍼티**:
  - `Vertices`(List\<Vector3\>) — 정점 좌표 목록
  - `Indices`(List\<int\>) — 삼각형 인덱스 목록 (fan 삼각화 후)
  - `UVs`(List\<Vector2\>) — UV 좌표 목록 (파싱 전 `Vector2.zero`로 초기화, `MeshTextureCoords` 블록 파싱 후 갱신)
