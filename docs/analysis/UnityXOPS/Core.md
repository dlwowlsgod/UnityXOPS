# Core

## 개요
게임 전반에서 공유되는 데이터와 머티리얼을 관리하는 싱글톤 레이어. `DataManager`는 StreamingAssets의 JSON/MIF 파일을 파싱해 파라미터 데이터를 제공하고, `MaterialManager`는 씬 렌더링에 필요한 머티리얼 레퍼런스를 노출한다. 둘 다 `SingletonBehavior<T>` 기반이므로 전역에서 `Instance`로 접근 가능하다.

## 클래스 목록

### DataManager
- **파일**: `Assets/UnityXOPS/Runtime/DataManager.cs`
- **역할**: StreamingAssets의 JSON 파일 4종을 `Start()`에서 로드하고 게임 전역에 데이터를 제공하는 싱글톤 매니저
- **주요 필드/프로퍼티**:
  - `humanParameterData`(HumanParameterData) — 인간형 캐릭터 파라미터 데이터 (`[SerializeField]`)
  - `weaponParameterData`(WeaponParameterData) — 무기 파라미터 데이터 (`[SerializeField]`)
  - `skyData`(SkyData) — 스카이박스 데이터 (`[SerializeField]`)
  - `missionData`(MissionData) — 미션 목록 데이터 (`[SerializeField]`)
  - `k_humanParameterDataPath`(const string) — `"unitydata/human_parameter_data.json"`
  - `k_weaponParameterDataPath`(const string) — `"unitydata/weapon_parameter_data.json"`
  - `k_skyDataPath`(const string) — `"unitydata/sky_data.json"`
  - `k_missionDataPath`(const string) — `"unitydata/mission_data.json"`
- **주요 메서드**:
  - `Start()` — 4개 로드 메서드를 순서대로 호출
  - `LoadHumanParameterData()` — StreamingAssets에서 JSON 읽어 `JsonUtility.FromJson` 역직렬화
  - `LoadWeaponParameterData()` — 동일 방식
  - `LoadSkyData()` — 동일 방식
  - `LoadMissionData()` — JSON 로드 후 `StreamingAssets/addon/*.mif` 파일을 스캔해 `missionData.addonMissions`에 추가; MIF 첫 번째 줄을 미션 이름으로 사용
- **특이사항**:
  - `[SerializeField]` 필드는 `Start()` 실행 전 인스펙터에서 값이 할당되어 있어도 로드 시점에 덮어씌워진다
  - `LoadMissionData()`에서 addon 폴더가 없으면 빈 리스트만 초기화하고 넘어감 (`Directory.Exists` 체크)
  - MIF 경로 결합에 `SafePath.Combine` 사용 (JJLUtility 유틸리티)

---

### MaterialManager
- **파일**: `Assets/UnityXOPS/Runtime/MaterialManager.cs`
- **역할**: 미션 렌더링에 사용되는 4종 머티리얼 레퍼런스를 전역 제공하는 싱글톤 매니저
- **주요 필드/프로퍼티**:
  - `mainMaterial`(Material) — 불투명 지형 메시용 머티리얼 (`[SerializeField]`)
  - `transparentMaterial`(Material) — 투명 메시용 머티리얼 (`[SerializeField]`)
  - `effectMaterial`(Material) — 이펙트용 머티리얼 (`[SerializeField]`)
  - `skyMaterial`(Material) — 스카이박스용 머티리얼 (`[SerializeField]`)
  - `MainMaterial`, `TransparentMaterial`, `EffectMaterial`, `SkyMaterial` — 각 필드의 읽기 전용 프로퍼티
- **주요 메서드**: 없음 (데이터 컨테이너 역할만 수행)
- **특이사항**: 런타임 로직 없이 인스펙터에서 머티리얼을 할당해 전역으로 노출하는 구조
