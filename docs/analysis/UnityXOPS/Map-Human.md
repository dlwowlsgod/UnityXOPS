# Map-Human

## 개요
맵 위에 배치되는 Human 오브젝트의 역할과 Parameter 데이터와의 관계

`Human` 컴포넌트는 맵에서 파싱된 `RawPointData`(배치 정보)와 `HumanParameterData`(공용 파라미터)를 연결하여, 해당 인간의 HP/팀/생존 상태 같은 런타임 데이터와 실제 시각 표현(`HumanVisual`)을 생성한다. `HumanVisual`은 `DataManager.Instance.HumanParameterData`에 정의된 Body/Arm/Leg 모델·텍스처·애니메이션 데이터를 참조해 본체, 팔, 다리 메시와 머티리얼을 로드하고 부착한다. 공유 머티리얼은 `MapLoader.Instance.HumanMaterialCache`를 통해 캐시된다.

## 클래스 목록

### Human
- **파일**: `Assets/UnityXOPS/Runtime/Map/Human/Human.cs`
- **역할**: 맵에 배치된 인간 캐릭터의 런타임 상태(HP, 팀, 생존 여부)와 파라미터 데이터 참조를 보관하고, 생성 시점에 `HumanVisual`을 초기화하는 `MonoBehaviour`.
- **주요 필드/프로퍼티**:
  - `hp`(float, `[SerializeField]`) — 현재 HP. `HP` 프로퍼티로 외부 노출.
  - `team`(int, `[SerializeField]`) — 팀 번호. `Team` 프로퍼티로 외부 노출.
  - `alive`(bool, `[SerializeField]`) — 생존 여부. `Alive` 프로퍼티로 외부 노출.
  - `humanVisual`(HumanVisual, `[SerializeField]`) — 시각 표현 컴포넌트 참조. `HumanVisual` 프로퍼티로 외부 노출.
  - `m_humanData`(HumanData) — `HumanParameterData.humanData`에서 가져온 이 인간의 파라미터 엔트리.
  - `m_humanTypeData`(HumanTypeData) — `HumanParameterData.humanTypeData`에서 가져온 타입 엔트리. `HumanTypeData` 프로퍼티로 외부 노출.
  - `m_humanParam`, `m_humanDataParam`(RawPointData) — 맵 포인트 데이터 원본 참조.
- **주요 메서드**:
  - `CreateHuman(RawPointData humanParam, RawPointData humanDataParam)` — 두 개의 `RawPointData`를 받아 `param1`을 `humanData` 인덱스로, 참조된 `HumanData.typeIndex`를 `humanTypeData` 인덱스로 사용해 파라미터를 해석. 이후 `humanVisual.CreateHumanVisual(m_humanData)` 호출. 마지막으로 `hp`는 `m_humanData.hp`, `team`은 `m_humanDataParam.param2`, `alive`는 `hp > 0`으로 초기화.
- **특이사항**:
  - `RawPointData.param1`은 HumanData 인덱스, `RawPointData.param2`는 팀 번호라는 관례를 따른다.
  - 범위 체크 실패 시 `m_humanData`/`m_humanTypeData`는 기본값(null)으로 남으며, 별도 예외 처리는 없다.
  - 클래스에 문서화용 XML 주석(`<summary>`)이 포함되어 있다.

### HumanVisual
- **파일**: `Assets/UnityXOPS/Runtime/Map/Human/HumanVisual.cs`
- **역할**: Human의 신체(Body), 팔(Arm), 다리(Leg) 메시/머티리얼/애니메이션을 로드·교체하는 시각 표현 `MonoBehaviour`.
- **주요 필드/프로퍼티**:
  - `bodyRoot`, `fixedArmRoot`, `dynamicArmRoot`, `leftArmRoot`, `rightArmRoot`, `legRoot`(Transform, `[SerializeField]`) — 각 파트의 루트 Transform. 팔은 고정 팔(fixed)과 동적 팔(dynamic) 루트를 분리하여 자식 관계를 런타임에 전환.
  - `m_leftArmMeshFilter`, `m_rightArmMeshFilter`, `m_legMeshFilter`(MeshFilter) — 팔/다리용 MeshFilter 캐시.
  - `m_leftArmMeshRenderer`, `m_rightArmMeshRenderer`, `m_legMeshRenderer`(MeshRenderer) — 팔/다리용 MeshRenderer 캐시.
  - `m_humanMaterials`(List<Material>) — 텍스처 인덱스로 접근되는 머티리얼 리스트.
  - `m_leftArmMeshes`, `m_rightArmMeshes`, `m_legMeshes`(List<Mesh>) — 인덱스로 선택 가능한 팔/다리 메시 풀.
  - `m_humanModelData`(HumanModelData) — 현재 인간의 Body 모델 파라미터.
  - `m_humanArmModelData`(HumanArmModelData) — 팔 모델 파라미터.
  - `m_humanLegModelData`(HumanLegModelData) — 다리 모델 파라미터.
  - `m_legAnimation`(List<HumanAnimation>) — `humanGeneralData.humanAnimation`에서 가져온 다리 애니메이션 세트.
  - `m_idleAnimation`, `m_walkAnimation`, `m_runAnimation`(HumanAnimation) — 각각 이름이 "Idle", "Walk", "Run"인 애니메이션 엔트리.
- **주요 메서드**:
  - `CreateHumanVisual(HumanData data)` — `HumanParameterData`의 `humanModelData`/`humanArmModelData`/`humanLegModelData`/`humanGeneralData`를 참조해:
    1. `m_humanModelData.textures`를 순회하며 `StreamingAssets` 경로에서 텍스처를 로드하고 `MaterialManager.Instance.MainMaterial`을 베이스로 머티리얼 생성. 이미 로드된 경로는 `MapLoader.Instance.HumanMaterialCache`에서 재사용.
    2. `bodyRoot`의 `localPosition`을 `(0, humanBodyHeight, 0)`, `localScale`에 `humanBodyScale`을 곱한 뒤, `modelData` 각 엔트리마다 `Body_{index}` GameObject를 생성, `MeshFilter`/`MeshRenderer` 부착 및 `ModelLoader.LoadMesh`로 메시 로드. `textureIndex` 범위 밖이면 `MainMaterial`을 fallback으로 사용.
    3. `dynamicArmRoot`에 `humanArmHeight`/`humanArmScale` 적용. `armIndex`가 유효하면 좌/우 팔 메시들을 로드하고 `armTextureIndex`로 머티리얼 지정, 마지막에 `SetArmModel(2, 2, true, true)`를 호출(주석에 "임시. 무기 손에 맞게 수정해야 함"으로 표기).
    4. `legRoot`에 `humanLegHeight`/`humanLegScale` 적용. `m_legAnimation`을 `humanGeneralData.humanAnimation`에서 가져오고 이름 매칭으로 Idle/Walk/Run을 분리. `legIndex`가 유효하면 다리 메시들을 로드하고 `legTextureIndex`로 머티리얼 지정.
    5. 초기 다리 메시는 `m_idleAnimation != null`이면 `m_idleAnimation.index[0]`, 아니면 `0`으로 `SetLegModel` 호출.
  - `SetArmModel(int leftIndex, int rightIndex, bool fixLeft, bool fixRight)` — `m_humanArmModelData`가 null이면 팔 메시를 클리어하고 종료. `fixLeft`/`fixRight`에 따라 `leftArmRoot`/`rightArmRoot`의 부모를 `fixedArmRoot` 또는 `dynamicArmRoot`로 재부착하고 `localRotation`을 `Quaternion.identity`로 리셋. 이후 각 인덱스 범위 체크 후 해당 메시를 `sharedMesh`에 할당(범위 밖이면 null).
  - `SetBodyVisible(bool visible)` — `bodyRoot`와 `legRoot` GameObject의 활성 상태를 동시에 토글.
  - `SetLegModel(int legIndex)` — `legRoot`가 비활성이면 즉시 반환. `m_legAnimation`이 null이면 다리 메시를 null로 설정하고 반환. 인덱스 범위 체크 후 `m_legMeshes[legIndex]`를 `sharedMesh`에 할당(범위 밖이면 null).
- **특이사항**:
  - 메시/텍스처 경로는 모두 `SafePath.Combine(Application.streamingAssetsPath, ...)` 로 결합. 모델은 `ModelLoader.LoadMesh`, 텍스처는 `ImageLoader.LoadTexture`로 로드.
  - 머티리얼 공유 캐시는 `MapLoader.Instance.HumanMaterialCache`(키: 풀 경로). 캐시 히트 시 새 머티리얼을 만들지 않음.
  - 팔의 부모 전환 구조(`fixedArmRoot` vs `dynamicArmRoot`)로 1인칭 카메라에 고정된 팔과 월드/캐릭터 회전을 따르는 팔을 분리해 표현한다.
  - Body는 개별 자식 GameObject(`Body_{index}`)를 동적으로 생성하지만, Arm/Leg는 하나의 루트(`leftArmRoot`/`rightArmRoot`/`legRoot`)의 `MeshFilter`를 교체하는 방식으로 애니메이션 프레임을 표현한다.
  - `SetArmModel`은 `CreateHumanVisual` 내부에서 하드코딩된 `(2, 2, true, true)`로 호출되며, 주석에 "임시. 무기 손에 맞게 수정해야 함"이 명시되어 있다.
  - 애니메이션은 이름 기반 검색("Idle"/"Walk"/"Run") + `index` 배열을 통한 프레임별 메시 인덱스 참조 방식이다(초기 포즈로 `m_idleAnimation.index[0]` 사용).
  - `using UnityEngine.SocialPlatforms;` 구문이 포함되어 있으나 클래스 내에서 실제 사용처는 보이지 않는다.
