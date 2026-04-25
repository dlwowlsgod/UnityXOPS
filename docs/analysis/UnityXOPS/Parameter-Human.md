# Parameter — Human

## 개요

Human 파라미터 시스템은 `DataManager`가 보유하는 `HumanParameterData` 단일 객체를 루트로 하며, 캐릭터 외형·전투·애니메이션에 필요한 모든 수치를 직렬화된 리스트로 관리한다. `MapLoader`가 PD1 포인트를 파싱해 `Human`을 스폰할 때 이 파라미터를 인덱스로 참조한다.

---

## 클래스 목록

### HumanParameterData
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/HumanParameterData.cs`
- **역할**: Human 파라미터 전체를 하나로 묶는 최상위 컨테이너
- **주요 필드/프로퍼티**:
  - `humanGeneralData` (HumanGeneralData) — 전 캐릭터 공통 스케일·컨트롤러 설정
  - `humanData` (List\<HumanData\>) — 개별 캐릭터 정의 목록
  - `humanModelData` (List\<HumanModelData\>) — 신체 모델 정의 목록
  - `humanArmModelData` (List\<HumanArmModelData\>) — 팔 모델 정의 목록
  - `humanLegModelData` (List\<HumanLegModelData\>) — 다리 모델 정의 목록
  - `humanTypeData` (List\<HumanTypeData\>) — 타입별 피격·사망 설정 목록
- **특이사항**: `[Serializable]`. 모든 하위 리스트는 인덱스로 교차 참조됨

---

### HumanGeneralData
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/Human/HumanGeneralData.cs`
- **역할**: 모든 캐릭터에 공통으로 적용되는 스케일·높이·CharacterController 파라미터
- **주요 필드/프로퍼티**:
  - `humanBodyScale` (float) — 신체 메시 로컬 스케일 배수
  - `humanArmScale` (float) — 팔 메시 로컬 스케일 배수
  - `humanLegScale` (float) — 다리 메시 로컬 스케일 배수
  - `controllerHeight` (float) — CharacterController 캡슐 높이
  - `humanBodyHeight` (float) — bodyRoot 로컬 Y 오프셋
  - `humanArmHeight` (float) — dynamicArmRoot 로컬 Y 오프셋
  - `humanLegHeight` (float) — legRoot 로컬 Y 오프셋
  - `cameraAttachPosition` (float) — 카메라 부착 Y 위치
  - `controllerRadiusControllerToMap` (float) — 맵과의 충돌 반지름
  - `controllerRadiusControllerToController` (float) — 캐릭터 간 충돌 반지름
  - `controllerStepOffset` (float) — 계단 오르기 최대 높이
  - `controllerStepClimbSpeed` (float) — 계단 오르기 속도
  - `controllerSlopeLimit` (float) — 오를 수 있는 경사 한계(도)
  - `humanAnimation` (List\<HumanAnimation\>) — 다리 애니메이션 프레임 정의 목록
- **특이사항**: `[Serializable]`. `HumanVisual.CreateHumanVisual()`이 스케일·높이 값을 직접 읽어 Transform에 적용

---

### HumanData
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/Human/HumanData.cs`
- **역할**: 캐릭터 한 종류의 이름·체력·인덱스 묶음
- **주요 필드/프로퍼티**:
  - `name` (string) — 캐릭터 식별 이름
  - `hp` (int) — 초기 체력
  - `modelIndex` (int) — `humanModelData` 리스트 인덱스
  - `weaponIndex0` (int) — 주 무기 인덱스
  - `weaponIndex1` (int) — 보조 무기 인덱스
  - `aiIndex` (int) — AI 파라미터 인덱스
  - `typeIndex` (int) — `humanTypeData` 리스트 인덱스
- **특이사항**: `[Serializable]`. `Human.CreateHuman()`에서 `humanDataParam.param1`을 `humanIndex`로 사용해 조회

---

### HumanTypeData
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/Human/HumanTypeData.cs`
- **역할**: 타입(Human/Robot/Zombie)별 피격 배율·부활·이펙트 파라미터
- **주요 필드/프로퍼티**:
  - `headDamageMultiplier` (float) — 머리 피격 데미지 배율
  - `bodyDamageMultiplier` (float) — 몸통 피격 데미지 배율
  - `legDamageMultiplier` (float) — 다리 피격 데미지 배율
  - `maxFallDamage` (float) — 낙하 데미지 최대값
  - `bloodEffectIndex` (int) — 피 이펙트 인덱스
  - `bloodEffectThreshold` (float) — 피 이펙트 발생 데미지 임계값
  - `hitEffectIndex` (int) — 피격 이펙트 인덱스
  - `deathEffectIndex` (int) — 사망 이펙트 인덱스
  - `zombie` (bool) — 좀비 타입 여부
  - `resurrectable` (bool) — 부활 가능 여부
  - `resurrectableChance` (float) — 부활 확률
  - `resurrectableChanceDecrease` (float) — 부활 후 확률 감소량
  - `resurrectHealthPercentage` (float) — 부활 시 체력 비율
- **특이사항**: `[Serializable]`. 주석으로 타입 순서가 0=Human, 1=Robot, 2=Zombie 임을 명시

---

### HumanAnimation
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/Human/HumanAnimation.cs`
- **역할**: 다리 애니메이션의 프레임 인덱스 시퀀스와 이동 속도 정의
- **주요 필드/프로퍼티**:
  - `name` (string) — 애니메이션 이름 (예: "Idle", "Walk", "Run")
  - `index` (List\<int\>) — 재생할 메시 프레임 인덱스 목록
  - `forwardSpeed` (float) — 전진 시 재생 속도
  - `strafeSpeed` (float) — 측면 이동 시 재생 속도
  - `backwardSpeed` (float) — 후진 시 재생 속도
- **특이사항**: `[Serializable]`. `HumanVisual`에서 name == "Idle"/"Walk"/"Run"으로 LINQ FirstOrDefault 조회

---

### HumanModelData
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/Human/HumanModelData.cs`
- **역할**: 신체 메시·텍스처 및 팔·다리 모델 인덱스 묶음
- **주요 필드/프로퍼티**:
  - `name` (string) — 모델 식별 이름
  - `textures` (List\<string\>) — 텍스처 파일 경로 목록 (StreamingAssets 상대 경로)
  - `modelData` (List\<ModelData\>) — 신체 메시 파트 목록
  - `armIndex` (int) — `humanArmModelData` 리스트 인덱스
  - `armTextureIndex` (int) — 팔에 사용할 `textures` 내 인덱스
  - `legIndex` (int) — `humanLegModelData` 리스트 인덱스
  - `legTextureIndex` (int) — 다리에 사용할 `textures` 내 인덱스
- **특이사항**: `[Serializable]`. 텍스처는 `MapLoader.HumanMaterialCache`로 중복 로드 방지

---

### HumanArmModelData
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/Human/HumanArmModelData.cs`
- **역할**: 팔 애니메이션 프레임별 좌·우 메시 경로 목록
- **주요 필드/프로퍼티**:
  - `name` (string) — 팔 모델 세트 식별 이름
  - `leftArms` (List\<string\>) — 왼팔 메시 파일 경로 목록 (프레임 순서)
  - `rightArms` (List\<string\>) — 오른팔 메시 파일 경로 목록 (프레임 순서)
- **특이사항**: `[Serializable]`. `HumanVisual.SetArmModel(leftIndex, rightIndex, fixLeft, fixRight)`으로 프레임 선택

---

### HumanLegModelData
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/Human/HumanLegModelData.cs`
- **역할**: 다리 애니메이션 프레임별 메시 경로 목록
- **주요 필드/프로퍼티**:
  - `name` (string) — 다리 모델 세트 식별 이름
  - `legs` (List\<string\>) — 다리 메시 파일 경로 목록 (프레임 순서)
- **특이사항**: `[Serializable]`. `HumanVisual.SetLegModel(legIndex)`으로 프레임 선택

---

### ModelData
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/ModelData.cs`
- **역할**: 메시 파트 하나의 경로·변환·텍스처 인덱스 묶음 (Human·Weapon 공용)
- **주요 필드/프로퍼티**:
  - `modelPath` (string) — 메시 파일 경로 (StreamingAssets 상대 경로)
  - `position` (Vector3) — 부모 기준 로컬 위치
  - `rotation` (Vector3) — 오일러 각도 로컬 회전
  - `scale` (Vector3) — 로컬 스케일
  - `textureIndex` (int) — 부모 모델의 `textures` 리스트 내 인덱스
- **특이사항**: `[Serializable]`. `HumanModelData.modelData`, `WeaponModelData.modelData` 양쪽에서 재사용
