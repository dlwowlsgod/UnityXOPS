# Map — Data (MapLoader, Block, Point, Sky, Mission)

## 개요

맵 데이터 시스템은 `MapLoader` 싱글톤이 BD1(블록) / PD1(포인트) 이진 파일을 파싱해 씬 오브젝트를 생성·제거하는 구조다. 스카이박스는 `SkyData`에서 메시·텍스처 경로를 읽어 메인 카메라 하위에 생성된다. 미션 메타데이터는 공식(JSON) 또는 어드온(.mif 텍스트) 두 경로로 로드된다. 각 기능은 `MapLoader`의 `partial class` 파일에 분리 구현된다.

좌표 변환: BD1·PD1 원시값 × 0.1 = Unity 단위. X·Z 축은 부호 반전(`-x * 0.1`, `-z * 0.1`)해 Y축 기준 180° 회전을 적용한다.

---

## 클래스 목록

### MapLoader
- **파일**: `Assets/UnityXOPS/Runtime/Map/MapLoader.cs` (partial, BlockData.cs / PointData.cs에 추가 partial 존재)
- **역할**: BD1·PD1·Sky·Mission 로드/언로드를 총괄하는 씬 싱글톤 매니저
- **주요 필드/프로퍼티**:
  - `blockRoot` (Transform) — 블록 오브젝트들의 부모 Transform
  - `humanRoot` (Transform) — Human 오브젝트들의 부모 Transform
  - `humanPrefab` (GameObject) — Human 스폰에 사용할 프리팹
  - `blockCount` (int) — 로드된 블록 수 (직렬화, 에디터 확인용)
  - `blockMaterials` (List\<Material\>) — BD1 텍스처 인덱스별 머티리얼 목록
  - `blockColliders` (List\<Block\>) — 충돌 판정용 Block 목록
  - `pointCount` / `humanCount` / `weaponCount` / `objectCount` (int) — PD1 통계
  - `messages` (List\<string\>) — .msg 파일에서 읽은 메시지 목록
  - `m_humanMaterialCache` / `m_weaponMaterialCache` / `m_objectMaterialCache` (Dictionary\<string, Material\>) — 경로 기반 머티리얼 캐시
  - `player` (Human) — 현재 플레이어 Human 참조
  - `missionName`, `missionFullname`, `missionBD1Path`, `missionPD1Path`, `missionAddonObjectPath`, `missionImage0`, `missionImage1`, `missionBriefing` (string) — 로드된 미션 메타 정보
  - `skyIndex` (int) — 사용 중인 스카이 텍스처 인덱스
  - `adjustCollision` (bool) — 충돌 보정 플래그
  - `darkScreen` (bool) — 화면 어둡게 플래그
  - `m_sortedRawPointData` (List\<Dictionary\<int, List\<RawPointData\>\>\>) — param0 타입별로 정렬된 포인트 데이터 (로드 중 임시 사용 후 null 처리)
  - `BlockMaterials` (IReadOnlyList 프로퍼티) — 블록 머티리얼 외부 읽기용
  - `BlockColliders` (IReadOnlyList 프로퍼티) — 블록 충돌체 외부 읽기용
  - `Player` (static Human) — 플레이어 Human 외부 읽기용
- **주요 메서드**:
  - `LoadBlockData(string filepath)` — BD1 파싱 → 메시·머티리얼 생성 → 씬 배치
  - `UnloadBlockData()` — blockRoot 하위 오브젝트 전부 제거 및 리스트 초기화
  - `LoadPointData(string filepath)` — PD1 파싱 → param0별 정렬 → Human 스폰
  - `UnloadPointData()` — humanRoot 하위 오브젝트 전부 제거 및 캐시 초기화
  - `LoadSkyData(int textureIndex)` — sky 메시 로드 → 머티리얼 생성 → 카메라 하위 배치
  - `UnloadSkyData()` — 카메라 하위 "Skybox" 오브젝트 제거
  - `LoadMissionData(int index, bool mif)` — 공식 JSON(false) 또는 .mif 텍스트(true) 파싱
  - `UnloadMissionData()` — 미션 관련 필드 전부 초기값으로 초기화
  - `RaycastBlock(Vector3 origin, Vector3 direction, float maxDist, out float dist)` — Block 레이어 Physics.Raycast 래퍼 (OpenXOPS `CheckALLBlockIntersectRay` 대응)
  - `IsInsideBlock(Vector3 point)` — blockColliders 순회하여 내부 판정 (OpenXOPS `CheckALLBlockInside` 대응)
  - `SpawnHumans(int pointType)` — 지정 pointType(1 또는 6)의 포인트를 순회해 Human 인스턴스화
- **특이사항**:
  - `SingletonBehavior<MapLoader>` 상속 (씬 싱글톤)
  - `partial class` — BD1 파싱 로직은 `BlockData.cs`, PD1 파싱 로직은 `PointData.cs`에 분리
  - `k_maxParameterCount = 20` 상수로 param0 타입 상한 고정
  - Human 스폰 시 param0==1(HUMAN)과 param0==6(HUMAN2) 모두 처리; param3==0이 플레이어
  - param0==1/6 포인트가 param1로 param0==4(정보 포인트)를 참조해 `HumanData` 결정

---

### BlockData
- **파일**: `Assets/UnityXOPS/Runtime/Map/Block/BlockData.cs`
- **역할**: BD1 파일 전체 파싱 결과 컨테이너 (+ `MapLoader` partial 구현 포함)
- **주요 필드/프로퍼티**:
  - `texturePaths` (string[]) — BD1 헤더의 텍스처 경로 10개
  - `rawBlockData` (RawBlockData[]) — 파싱된 원시 블록 배열
  - `textures` (Material[]) — (미사용, 예비 필드)
  - `blocks` (Block[]) — 빌드된 Block 배열
- **특이사항**:
  - `[Serializable]`
  - BD1 포맷: 텍스처 경로 10×31바이트 → uint16 블록 수 → 블록별 (X[8]·Y[8]·Z[8] float → U[24]·V[24] float → textureIndex[6] int32 → flag int32)
  - 좌표 변환: `(-x*0.1, y*0.1, -z*0.1)` (Y축 기준 180° 회전)
  - UV 변환: `V → 1f - V` (DirectX V=0 상단 → Unity V=0 하단)
  - 블록 1개가 서브메시 여러 개를 가질 수 있음 (텍스처 인덱스가 다른 면 수만큼)
  - `isBoardBlock` 판정: 확장 정점 중복 또는 법선-중심 내적 기준으로 얇은 판(투명벽) 여부 결정, 판이면 `collider = false`

---

### RawBlockData
- **파일**: `Assets/UnityXOPS/Runtime/Map/Block/RawBlockData.cs`
- **역할**: BD1에서 읽은 블록 하나의 원시 데이터 구조체
- **주요 필드/프로퍼티**:
  - `vertices` (Vector3[8]) — 이미 Unity 좌표계로 변환된 8개 정점
  - `uvs` (Vector2[24]) — 6면 × 4개 UV, V축 반전 적용 완료
  - `textureIndices` (int[6]) — 면별 텍스처 인덱스
  - `flag` (int) — BD1 원시 활성화 플래그
- **특이사항**: `struct`, `[Serializable]`

---

### Block
- **파일**: `Assets/UnityXOPS/Runtime/Map/Block/Block.cs`
- **역할**: 빌드 완료된 블록의 Mesh·충돌 데이터 + 점 내부 판정 메서드
- **주요 필드/프로퍼티**:
  - `mesh` (Mesh) — 서브메시 포함 Unity Mesh
  - `subMeshTextureIndices` (int[]) — 서브메시 순서별 텍스처 인덱스
  - `position` (Vector3) — 8개 정점의 중심 (씬 오브젝트 localPosition으로 사용)
  - `collider` (bool) — 충돌 판정 여부 (false면 투명벽)
  - `faceNormals` (Vector3[6]) — 외향 법선 (Contains 계산용)
  - `faceCenters` (Vector3[6]) — 면 중심 (Contains 계산용)
- **주요 메서드**:
  - `Contains(Vector3 worldPoint) → bool` — 6개 면 법선·중심 내적으로 볼록 도형 내부 판정
- **특이사항**: `collider == false`면 `Contains`는 항상 false 반환

---

### PointData
- **파일**: `Assets/UnityXOPS/Runtime/Map/Point/PointData.cs`
- **역할**: PD1 파일 전체 파싱 결과 컨테이너 (+ `MapLoader` partial 구현 포함)
- **주요 필드/프로퍼티**:
  - `rawPointData` (RawPointData[]) — 파싱된 포인트 배열
  - `msg` (string[]) — 같은 경로의 .msg 파일 라인 배열 (없으면 null)
- **특이사항**:
  - PD1 포맷: int16 포인트 수 → 포인트별 (X·Y·Z float → look float(라디안) → param0~3 byte)
  - look 변환: `radian * Rad2Deg + 180f`
  - .msg 파일은 PD1와 동일 경로·이름, 확장자만 `.msg`로 읽음

---

### RawPointData
- **파일**: `Assets/UnityXOPS/Runtime/Map/Point/RawPointData.cs`
- **역할**: PD1에서 파싱된 포인트 하나의 원시 데이터 클래스
- **주요 필드/프로퍼티**:
  - `position` (Vector3) — Unity 좌표계 위치 (`(-x*0.1, y*0.1, -z*0.1)`)
  - `look` (float) — Y축 회전각 (도, 변환 완료)
  - `param0` (int) — 포인트 타입 (1=HUMAN, 4=정보, 6=HUMAN2 등)
  - `param1` (int) — 타입별 보조 파라미터 (Human: 정보 포인트 인덱스)
  - `param2` (int) — 타입별 보조 파라미터 (Human: 팀 번호)
  - `param3` (int) — 타입별 보조 파라미터 (Human: 0이면 플레이어)

---

### SkyData
- **파일**: `Assets/UnityXOPS/Runtime/Map/Sky/SkyData.cs`
- **역할**: 스카이박스 메시 경로와 텍스처 경로 목록
- **주요 필드/프로퍼티**:
  - `skyMeshPath` (string) — 스카이 메시 파일 경로 (StreamingAssets 상대)
  - `skyTexturePath` (List\<string\>) — 스카이 텍스처 경로 목록 (인덱스 0은 '텍스처 없음' 처리)
- **특이사항**: `[Serializable]`. `DataManager`가 보유. `textureIndex == 0`이면 검은색 머티리얼 적용

---

### MissionData
- **파일**: `Assets/UnityXOPS/Runtime/Map/Mission/MissionData.cs`
- **역할**: 데모·공식·어드온 미션 목록을 담는 최상위 미션 데이터 클래스
- **주요 필드/프로퍼티**:
  - `demoData` (List\<DemoData\>) — 메인메뉴 배경용 데모 목록
  - `officialMissions` (List\<OfficialMissionData\>) — 공식 미션 목록
  - `addonMissions` (List\<AddonMissionData\>) — 어드온 미션 목록
- **특이사항**: `[Serializable]`. `addonMissions`는 `#if !UNITY_EDITOR` 블록 안에서 `[NonSerialized]` 적용되어 빌드에서는 직렬화 제외

---

### OfficialMissionData
- **파일**: `Assets/UnityXOPS/Runtime/Map/Mission/OfficialMissionData.cs`
- **역할**: JSON에 저장된 공식 미션의 경로 및 플래그
- **주요 필드/프로퍼티**:
  - `name` (string) — 미션 단축 이름
  - `fullname` (string) — 미션 전체 이름
  - `bd1Path` (string) — BD1 파일 경로 (StreamingAssets 상대)
  - `pd1Path` (string) — PD1 파일 경로 (StreamingAssets 상대)
  - `txtPath` (string) — 브리핑 텍스트 파일 경로 (StreamingAssets 상대)
  - `adjustCollision` (bool) — 충돌 보정 활성화 여부
  - `darkScreen` (bool) — 화면 어둡게 활성화 여부
- **특이사항**: `[Serializable]`. txt 파일 라인 구조: [0]=이미지0, [1]=이미지1, [2]=skyIndex, [3..]= 브리핑 텍스트

---

### AddonMissionData
- **파일**: `Assets/UnityXOPS/Runtime/Map/Mission/AddonMissionData.cs`
- **역할**: 어드온 미션의 이름과 .mif 파일 경로
- **주요 필드/프로퍼티**:
  - `name` (string) — 미션 이름 (표시용)
  - `mifPath` (string) — .mif 파일 전체 경로
- **특이사항**: `[Serializable]`. .mif 파일 라인 구조: [0]=name, [1]=fullname, [2]=BD1경로, [3]=PD1경로, [4]=skyIndex, [5]=비트플래그(bit0=adjustCollision, bit1=darkScreen), [6]=오브젝트경로('!'=없음), [7]=이미지0, [8]=이미지1('!'=없음), [9..]=브리핑

---

### DemoData
- **파일**: `Assets/UnityXOPS/Runtime/Map/Mission/DemoData.cs`
- **역할**: 메인메뉴 배경 데모 맵의 최소 경로 정보
- **주요 필드/프로퍼티**:
  - `bd1Path` (string) — BD1 파일 경로
  - `pd1Path` (string) — PD1 파일 경로
  - `skyIndex` (int) — 스카이 텍스처 인덱스
- **특이사항**: `[Serializable]`
