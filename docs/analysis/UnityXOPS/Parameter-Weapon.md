# Parameter — Weapon

## 개요

Weapon 파라미터 시스템은 `DataManager`가 보유하는 `WeaponParameterData` 단일 객체를 루트로 하며, 무기 전투 수치·탄환·스코프·시각 표현에 필요한 모든 데이터를 직렬화된 리스트로 관리한다. 각 리스트 항목은 인덱스로 교차 참조된다.

---

## 클래스 목록

### WeaponParameterData
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/WeaponParameterData.cs`
- **역할**: Weapon 파라미터 전체를 하나로 묶는 최상위 컨테이너
- **주요 필드/프로퍼티**:
  - `weaponGeneralData` (WeaponGeneralData) — 전 무기 공통 정확도·반동 페널티 설정
  - `weaponData` (List\<WeaponData\>) — 개별 무기 정의 목록
  - `bulletData` (List\<BulletData\>) — 탄환 정의 목록
  - `scopeData` (List\<ScopeData\>) — 스코프 정의 목록
  - `weaponModelData` (List\<WeaponModelData\>) — 무기 모델 정의 목록
- **특이사항**: `[Serializable]`

---

### WeaponGeneralData
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/Weapon/WeaponGeneralData.cs`
- **역할**: 모든 무기에 공통으로 적용되는 정확도 페널티·반동·손상 수치
- **주요 필드/프로퍼티**:
  - `noneWeaponIndex` (int) — '무기 없음' 상태를 나타내는 무기 인덱스
  - `caseWeaponIndex` (List\<int\>) — 케이스(격투 등) 무기 인덱스 목록
  - `walkAccuracyPenalty` (int) — 걷기 중 정확도 페널티
  - `forwardAccuracyPenalty` (int) — 전진 중 정확도 페널티
  - `backAccuracyPenalty` (int) — 후진 중 정확도 페널티
  - `strafeAccuracyPenalty` (int) — 측면 이동 중 정확도 페널티
  - `airborneAccuracyPenalty` (int) — 공중 정확도 페널티
  - `injuryAccuracyPenalty` (int) — 부상 상태 정확도 페널티
  - `injuryHpThreshold` (int) — 부상 판정 HP 임계값
  - `reactionDecayPerSecond` (int) — 초당 반응 감쇠량
  - `errorAngleDegrees` (float) — 오차 각도 (도)
- **특이사항**: `[Serializable]`

---

### WeaponData
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/Weapon/WeaponData.cs`
- **역할**: 무기 한 종류의 전투·발사·재장전·사운드·스코프 전체 파라미터
- **주요 필드/프로퍼티**:
  - `name` (string) — 무기 식별 이름
  - `modelIndex` (int) — `weaponModelData` 리스트 인덱스
  - `damage` (float) — 기본 데미지
  - `penetration` (int) — 관통력
  - `bulletIndex` (int) — `bulletData` 리스트 인덱스
  - `fireRate` (float) — 연사 속도 (발/초 또는 간격)
  - `bulletSpeed` (float) — 탄환 초기 속도
  - `magazineSize` (int) — 탄창 용량
  - `pelletCount` (int) — 발사당 탄환 수 (산탄총 등)
  - `burstMode` (WeaponBurstMode) — 발사 모드 (FullAuto / SemiAuto / Burst)
  - `burstCount` (int) — Burst 모드 시 연속 발사 수
  - `reloadStyle` (WeaponReloadStyle) — 재장전 방식
  - `reloadTime` (float) — 재장전 소요 시간
  - `recoil` (float) — 반동 크기
  - `recoilAimVerticalMin` (float) — 조준 중 수직 반동 최솟값
  - `recoilAimVerticalMax` (float) — 조준 중 수직 반동 최댓값
  - `recoilAimHorizontalMin` (float) — 조준 중 수평 반동 최솟값
  - `recoilAimHorizontalMax` (float) — 조준 중 수평 반동 최댓값
  - `errorRangeMin` (float) — 탄퍼짐 오차 최솟값
  - `errorRangeMax` (float) — 탄퍼짐 오차 최댓값
  - `scope` (bool) — 스코프 사용 여부
  - `scopeIndex` (int) — `scopeData` 리스트 인덱스
  - `position` (Vector3) — 손에 장착될 로컬 위치
  - `size` (float) — 무기 표시 크기
  - `soundPath` (string) — 발사음 파일 경로
  - `soundVolume` (float) — 발사음 볼륨
  - `suppressor` (bool) — 소음기 장착 여부
  - `previousWeaponIndex` (int) — 이전 무기 전환 인덱스
  - `nextWeaponIndex` (int) — 다음 무기 전환 인덱스
  - `switchTime` (float) — 무기 전환 소요 시간
- **특이사항**: `[Serializable]`. `WeaponBurstMode`, `WeaponReloadStyle` 열거형이 같은 파일에 정의됨

---

### WeaponReloadStyle (enum)
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/Weapon/WeaponData.cs`
- **역할**: 재장전 방식 구분
- **값**: `DiscardAndReload` / `RetainAndReload` / `ShellByShellReload` / `AutoReload`

---

### WeaponBurstMode (enum)
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/Weapon/WeaponData.cs`
- **역할**: 발사 모드 구분
- **값**: `FullAuto` / `SemiAuto` / `Burst`

---

### WeaponModelData
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/Weapon/WeaponModelData.cs`
- **역할**: 무기 메시·텍스처, 총구 섬광, 탄피 배출, 팔 연동 파라미터
- **주요 필드/프로퍼티**:
  - `name` (string) — 모델 식별 이름
  - `textures` (List\<string\>) — 텍스처 파일 경로 목록
  - `modelData` (List\<ModelData\>) — 무기 메시 파트 목록
  - `muzzleFlashTexture` (string) — 총구 섬광 텍스처 경로
  - `muzzleFlashOffset` (Vector3) — 총구 섬광 위치 오프셋
  - `muzzleFlashSize` (float) — 총구 섬광 크기
  - `shellTexture` (string) — 탄피 텍스처 경로
  - `shellEjectOffset` (Vector3) — 탄피 배출 위치 오프셋
  - `shellEjectDirection` (Vector3) — 탄피 배출 방향
  - `shellEjectSpeed` (float) — 탄피 배출 속도
  - `shellEjectDelay` (float) — 탄피 배출 지연 시간
  - `shellSize` (float) — 탄피 크기
  - `leftArmIndex` (int) — 무기 장착 시 왼팔 프레임 인덱스
  - `fixLeftArm` (bool) — 왼팔을 fixedArmRoot에 고정할지 여부
  - `rightArmIndex` (int) — 무기 장착 시 오른팔 프레임 인덱스
  - `fixRightArm` (bool) — 오른팔을 fixedArmRoot에 고정할지 여부
- **특이사항**: `[Serializable]`. `ShellEjectMode` 열거형이 같은 파일에 정의되나 현재 `WeaponModelData` 필드에서는 미사용

---

### ShellEjectMode (enum)
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/Weapon/WeaponModelData.cs`
- **역할**: 탄피 배출 시점 구분
- **값**: `OnFire` / `OnReload` / `None`

---

### BulletData
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/Weapon/BulletData.cs`
- **역할**: 탄환의 시각·물리·폭발·사운드 파라미터
- **주요 필드/프로퍼티**:
  - `name` (string) — 탄환 식별 이름
  - `texturePath` (string) — 탄환 텍스처 경로
  - `modelPath` (string) — 탄환 메시 경로
  - `modelScale` (float) — 탄환 메시 스케일
  - `useGravity` (bool) — 중력 영향 여부
  - `gravityScale` (float) — 중력 배율
  - `hasExplosion` (bool) — 폭발 여부
  - `explosionRadius` (float) — 폭발 범위 반지름
  - `explosionDamageMax` (float) — 폭발 최대 데미지
  - `explosionknockbackMax` (float) — 폭발 최대 넉백
  - `explosionSound` (string) — 폭발음 파일 경로
  - `wallHitSounds` (List\<string\>) — 벽 충돌음 파일 경로 목록
- **특이사항**: `[Serializable]`

---

### ScopeData
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/Weapon/ScopeData.cs`
- **역할**: 스코프의 FOV·정확도·반동 보정, 오버레이 텍스처, 조준선 정의
- **주요 필드/프로퍼티**:
  - `fovMultiplier` (float) — FOV 배율 (줌 배율)
  - `errorRangeAdjustMin` (float) — 조준 중 오차 최솟값 보정
  - `errorRangeAdjustMax` (float) — 조준 중 오차 최댓값 보정
  - `recoilAimVerticalAdjustMin` (float) — 조준 중 수직 반동 최솟값 보정
  - `recoilAimVerticalAdjustMax` (float) — 조준 중 수직 반동 최댓값 보정
  - `recoilAimHorizontalAdjustMin` (float) — 조준 중 수평 반동 최솟값 보정
  - `recoilAimHorizontalAdjustMax` (float) — 조준 중 수평 반동 최댓값 보정
  - `texturePath` (string) — 스코프 오버레이 텍스처 경로
  - `textureAspect` (float) — 텍스처 종횡비
  - `lines` (List\<ScopeLine\>) — 조준선 선분 목록
- **특이사항**: `[Serializable]`. `ScopeLine` 클래스가 같은 파일에 정의됨

---

### ScopeLine
- **파일**: `Assets/UnityXOPS/Runtime/Parameter/Weapon/ScopeData.cs`
- **역할**: 스코프 오버레이에 그릴 선분 하나의 정의
- **주요 필드/프로퍼티**:
  - `start` (Vector2) — 선분 시작점 (정규화 화면 좌표)
  - `end` (Vector2) — 선분 끝점 (정규화 화면 좌표)
  - `color` (Color) — 선 색상
  - `width` (float) — 선 너비
- **특이사항**: `[Serializable]`
