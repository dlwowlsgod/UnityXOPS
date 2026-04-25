# DataManager

## 클래스 개요

- **역할**: 게임 전역에서 사용하는 파라미터 데이터를 StreamingAssets의 JSON 파일에서 로드하고 전역 접근점을 제공하는 싱글톤 매니저
- **위치**: `Assets/UnityXOPS/Runtime/DataManager.cs`
- **상속**: `SingletonBehavior<DataManager>` (JJLUtility)

## 직렬화 필드

| 타입 | 이름 | JSON 경로 |
|---|---|---|
| `HumanParameterData` | `humanParameterData` | `unitydata/human_parameter_data.json` |
| `WeaponParameterData` | `weaponParameterData` | `unitydata/weapon_parameter_data.json` |
| `SkyData` | `skyData` | `unitydata/sky_data.json` |
| `MissionData` | `missionData` | `unitydata/mission_data.json` |

## 퍼블릭 프로퍼티

| 타입 | 이름 |
|---|---|
| `HumanParameterData` | `HumanParameterData` |
| `WeaponParameterData` | `WeaponParameterData` |
| `SkyData` | `SkyData` |
| `MissionData` | `MissionData` |

## 로드 흐름

`Start()`에서 4개 파일 순서대로 로드:

1. `LoadHumanParameterData()` → `JsonUtility.FromJson<HumanParameterData>`
2. `LoadWeaponParameterData()` → `JsonUtility.FromJson<WeaponParameterData>`
3. `LoadSkyData()` → `JsonUtility.FromJson<SkyData>`
4. `LoadMissionData()` → `JsonUtility.FromJson<MissionData>` + StreamingAssets/addon/*.mif 스캔

## OpenXOPS 원본과의 대응

OpenXOPS의 `ParameterInfo` 클래스가 하드코딩된 C++ 배열로 관리하던 파라미터들을 JSON 기반으로 외부화한 구조.

- `HumanParameterData` ← `ParameterInfo::Human[]`, `HumanTexturePath[]`
- `WeaponParameterData` ← `ParameterInfo::Weapon[]`, `ParameterInfo::Scope[]`, `ParameterInfo::Bullet[]`
- `MissionData` ← `ParameterInfo::MissionData[]` + 애드온 .mif 확장

## 특이사항

- `SkyData`는 `Assets/UnityXOPS/Runtime/Map/Sky/SkyData.cs`, `MissionData` 및 관련 클래스는 `Assets/UnityXOPS/Runtime/Map/Mission/`에 위치 (Parameter 폴더 아님)
- `LoadMissionData()`에서 `addonMissions`는 JSON에 저장되지 않으며, 런타임에 StreamingAssets/addon 폴더의 *.mif 파일 목록을 직접 스캔해서 채움. `MissionData.addonMissions`는 `#if !UNITY_EDITOR` 조건으로 `[NonSerialized]`가 붙어 빌드에서는 직렬화 제외되고 에디터에서만 Inspector에서 확인 가능 (디버깅 목적)
- 에러 처리 없음 — 파일 미존재 시 예외 미처리
