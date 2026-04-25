# Parameter — Object

## 개요

Object 파라미터 시스템은 `DataManager`가 보유하는 `ObjectParameterData` 단일 객체를 루트로 하며, OpenXOPS의 `SmallObject`(파괴 가능 오브젝트: 캔, PC, 의자, 콘 등)에 대응한다. Human/Weapon과 동일한 계층 패턴을 따르되, Unity에서 여러 콜라이더 형상을 지원하기 위해 `ObjectColliderData`를 별도 리스트로 분리한 것이 특징이다.

---

## 1. 추가된 파일

### 클래스
- `Assets/UnityXOPS/Runtime/Parameter/ObjectParameterData.cs` — 최상위 컨테이너
- `Assets/UnityXOPS/Runtime/Parameter/Object/ObjectGeneralData.cs` — 전역 공통값
- `Assets/UnityXOPS/Runtime/Parameter/Object/ObjectData.cs` — 개별 오브젝트 정의
- `Assets/UnityXOPS/Runtime/Parameter/Object/ObjectModelData.cs` — 모델/텍스처 정의
- `Assets/UnityXOPS/Runtime/Parameter/Object/ObjectColliderData.cs` — 콜라이더 형상 정의 (+ `ColliderShape`, `ColliderShapeType` enum)

### 데이터
- `Assets/StreamingAssets/unitydata/object_parameter_data.json` — 12개 오브젝트, 12개 모델, 5개 콜라이더 엔트리

---

## 2. 수정된 파일

### DataManager.cs
- `objectParameterData` 필드 및 프로퍼티 추가
- `k_objectParameterDataPath` 경로 상수 추가
- `LoadObjectParameterData()` 메서드 추가 및 `Start()`에서 호출

### HumanGeneralData.cs
- `grenadeDamageDistance` 필드 제거 (BulletData.explosionRadius와 중복)

### human_parameter_data.json
- `"grenadeDamageDistance": 8.0` 항목 제거

### weapon_parameter_data.json
- PSG1 스코프 라인 수정: 4개 분리 세그먼트(가장자리에만 그리던 버그) → 원본 OpenXOPS와 동일한 2개 연속 라인(중앙 관통)

---

## 클래스 목록

### ObjectParameterData
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/ObjectParameterData.cs`
- **역할**: Object 파라미터 전체를 하나로 묶는 최상위 컨테이너
- **주요 필드**:
  - `objectGeneralData` (ObjectGeneralData) — 전 오브젝트 공통 설정
  - `objectData` (List\<ObjectData\>) — 개별 오브젝트 정의 목록
  - `objectModelData` (List\<ObjectModelData\>) — 모델 정의 목록
  - `objectColliderData` (List\<ObjectColliderData\>) — 콜라이더 정의 목록
- **특이사항**: `[Serializable]`

---

### ObjectGeneralData
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/Object/ObjectGeneralData.cs`
- **역할**: 모든 오브젝트에 적용되는 전역 모델 스케일
- **주요 필드**:
  - `modelScale` (float) — 모델 렌더링 스케일 (OpenXOPS `SMALLOBJECT_SCALE=5.0` × 0.1 = **0.5**, Unity 값 직접)
- **특이사항**: `[Serializable]`. 콜라이더에는 적용하지 않음 (Human 히트박스와 동일한 정책)

---

### ObjectData
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/Object/ObjectData.cs`
- **역할**: 개별 오브젝트의 모델·콜라이더·내구력·사운드·파괴 물리 파라미터
- **주요 필드**:
  - `name` (string) — 오브젝트 식별 이름
  - `modelIndex` (int) — `objectModelData` 리스트 인덱스
  - `colliderIndex` (int) — `objectColliderData` 리스트 인덱스
  - `hp` (int) — 내구력
  - `soundPath` (string) — 피격 효과음 파일 경로
  - `soundVolume` (float) — 피격 효과음 볼륨
  - `jump` (int) — 파괴 시 튀는 강도
- **특이사항**: `[Serializable]`. modelIndex/colliderIndex 분리로 같은 콜라이더를 여러 모델이 공유 가능 (예: PC 변형 8종이 모두 colliderIndex=1 공유)

---

### ObjectModelData
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/Object/ObjectModelData.cs`
- **역할**: 오브젝트 모델의 메시/텍스처 구성
- **주요 필드**:
  - `textures` (List\<string\>) — 텍스처 파일 경로 목록
  - `modelData` (List\<ModelData\>) — 메시 정의 목록 (각각 modelPath, position, rotation, scale, textureIndex)
- **특이사항**: `[Serializable]`. Human/Weapon의 `ModelData` 구조를 공유

---

### ObjectColliderData
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/Object/ObjectColliderData.cs`
- **역할**: 오브젝트의 충돌 판정 형상 목록
- **주요 필드**:
  - `shapes` (List\<ColliderShape\>) — 충돌 형상 목록
- **특이사항**: `[Serializable]`. 원본 OpenXOPS는 오브젝트당 구 1개이나, 리스트 구조로 향후 다중 콜라이더 확장 대비

---

### ColliderShape
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/Object/ObjectColliderData.cs`
- **역할**: 개별 충돌 형상 (Sphere / Box / Capsule)
- **주요 필드**:
  - `type` (ColliderShapeType) — 형상 타입
  - `center` (Vector3) — 중심 오프셋
  - `size` (Vector3) — 타입별 해석: Box=(x,y,z), Sphere=x(반지름), Capsule=x(반지름) y(높이) z(방향 0=X 1=Y 2=Z)
- **특이사항**: `[Serializable]`. `size`는 **Unity 값 직접**(런타임 변환 없음)

---

### ColliderShapeType (enum)
- `Sphere` (0) / `Box` (1) / `Capsule` (2)

---

## 3. 관련 기능 구현 시 주의점

### (1) JSON 값 컨벤션 — Pre-converted Unity 값
모든 스케일/크기/위치/속도 값은 **OpenXOPS 원본 × 0.1이 이미 적용된 Unity 값**으로 저장된다. 런타임에서 추가로 × 0.1을 곱하면 안 된다.

- 예: `modelScale = 0.5` (= OpenXOPS `SMALLOBJECT_SCALE 5.0` × 0.1)
- 예: `humanBodyScale = 0.94` (= `upmodel_size 9.4` × 0.1)
- 예: CAN 콜라이더 `size.x = 0.13` (= `decide 10 × SMALLOBJECT_COLLISIONSCALE 0.13` × 0.1)

### (2) modelScale은 모델 렌더링 전용
`ObjectGeneralData.modelScale`은 **모델 Transform에만 적용**하고 콜라이더에는 곱하지 않는다. 히트박스(`headHitboxRadius` 등)가 `humanBodyScale`에 곱해지지 않는 것과 동일한 정책. 콜라이더 크기는 이미 `ColliderShape.size`에 Unity 값으로 저장되어 있다.

### (3) 인덱스 교차 참조
`ObjectData`의 `modelIndex`와 `colliderIndex`는 서로 다른 리스트를 가리킨다. 이 둘을 1:1 매핑으로 강제하면 안 되고(콜라이더는 의도적으로 공유 가능), 런타임에서 각 인덱스로 독립 조회해야 한다.

### (4) ColliderShape.size 해석
`type` 별로 다르게 해석한다. 런타임에서 Sphere의 y/z 값을 실수로 사용하면 안 된다:
- **Sphere**: `size.x` = 반지름 (y, z 무시)
- **Box**: `(size.x, size.y, size.z)` = 전체 크기
- **Capsule**: `size.x` = 반지름, `size.y` = 높이, `size.z` = 방향 축 (0=X, 1=Y, 2=Z)

### (5) 수류탄 폭발 데미지 거리
`HumanGeneralData.grenadeDamageDistance`는 제거됨. 수류탄 폭발 반경은 `BulletData.explosionRadius` (grenade bulletIndex=1) 하나가 단일 진실 소스. 오브젝트 측 최대 데미지(`SMALLOBJECT_DAMAGE_GRENADE=80`)는 현재 미구현이며, 필요 시 별도 위치에서 관리.

### (6) 사운드 볼륨 > 1.0 주의
`SmallObject`의 피격 볼륨은 `MAX_SOUNDHITSMALLOBJ=110` → JSON `1.1`로 저장되어 있다. Unity AudioSource.volume은 0.0–1.0 범위라 재생 시 1.0으로 클램프된다. 원본 의도를 따르되, 실제 재생 시엔 `Mathf.Clamp01()` 적용 권장.

### (7) 파일 누락 처리 미구현
`LoadObjectParameterData()`는 JSON 파일 존재 여부를 확인하지 않는다. 파일이 없으면 `FileNotFoundException`이 발생한다. JSON이 안정화되기 전까지 이 상태를 유지하며, 향후 Human/Weapon과 일괄 대응 예정.

---

## 4. 의의

### 설계 일관성
Human/Weapon 파라미터의 `GeneralData / List<X>` 패턴을 그대로 계승하여 세 도메인이 동일한 구조를 가지며, `DataManager`의 로딩 파이프라인도 완전히 대칭을 이룬다. 새 파라미터 그룹 추가 시 이 템플릿을 따르면 된다.

### 데이터 주도 전환
OpenXOPS는 `parameter.cpp`에 12개 SmallObject를 하드코딩했다. UnityXOPS는 이를 JSON으로 외부화하여 에디터/모딩 워크플로를 지원할 기반을 마련했다.

### 콜라이더 아키텍처 확장성
OpenXOPS 원본의 "오브젝트당 단일 구 콜라이더" 한계를 벗어나, `List<ColliderShape>` 구조로 **Sphere/Box/Capsule 다중 콜라이더**를 데이터로 정의할 수 있게 되었다. 향후 세분화된 히트 리전(PC의 모니터/본체 분리 등), AI pathfinding용 볼륨 등으로 확장 가능.

### 데이터 중복 제거
- `colliderIndex`/`modelIndex` 분리로 동일 콜라이더를 여러 오브젝트가 공유 (PC 계열 8종이 하나의 콜라이더 공유)
- `grenadeDamageDistance` 중복 제거 → `BulletData.explosionRadius` 단일 소스

### 컨벤션 명문화
JSON 값이 "OpenXOPS × 0.1의 Unity 값"임을 문서화하여, 향후 `colliderScale`/`modelScale` 같은 런타임 multiplier를 추가하려는 실수를 방지한다. 히트박스와 콜라이더 크기 모두 동일한 저장 정책을 따른다.

### 부수 정정
PSG1 스코프 라인이 원본(중앙 관통 십자선)과 반대 영역(가장자리)만 그리던 버그 수정. OpenXOPS `gamemain.cpp:2966-2967`의 실제 동작과 일치하도록 정렬.
