# UnityXOPS — Runtime

namespace: `UnityXOPS`

---

## Font

| 파일 | 클래스 | 역할 |
|---|---|---|
| `Runtime/Font/FontManager.cs` | `FontManager` (singleton) | OS 설치 폰트에서 TMP_FontAsset 생성. 시스템 언어(한국어/일본어/기타)에 따라 폰트 자동 선택. `data/char.dds`에서 스프라이트 폰트 텍스처 로드. |
| `Runtime/Font/XOPSSpriteText.cs` | `XOPSSpriteText` | `MaskableGraphic` 기반 UI 텍스트 컴포넌트. `data/char.dds` 16×16 글자 아틀라스를 사용해 ASCII 문자를 쿼드로 렌더링. 정렬(`TextAnchor`), 글자 크기, 자간 설정 지원. |

---

## Map

| 파일 | 클래스 | 역할 |
|---|---|---|
| `Runtime/Map/MapLoader.cs` | `MapLoader` (partial, singleton) | BD1 블록 데이터 및 스카이 로드/언로드 진입점. `blockRoot`, `skyRoot` Transform을 Inspector에서 설정. `LateUpdate`에서 `skyRoot`를 `Camera.main` 위치로 동기화(프러스텀 컬링 방지). |
| `Runtime/Map/Block/BlockData.cs` | `BlockData` + `MapLoader` (partial) | BD1 파일 파싱 로직. 텍스처 경로 10개, `RawBlockData` 배열, 빌드된 `Block` 배열 보관. `MapLoader`의 partial 메서드로 분리. |
| `Runtime/Map/Block/RawBlockData.cs` | `RawBlockData` | BD1 파일에서 읽은 원시 블록 데이터 구조체. 정점 8개, UV 24개(6면×4), 텍스처 인덱스 6개, 활성 플래그. |
| `Runtime/Map/Block/Block.cs` | `Block` | 빌드 완료된 블록. `Mesh`(서브메시 포함), `subMeshTextureIndices`, 월드 `position`, `collider` 플래그. |
| `Runtime/Map/Sky/SkyData.cs` | `SkyData` | 스카이 데이터 JSON 구조체. `skyMeshPath`(메시 경로), `skyTexturePath`(텍스처 변형 목록). 인덱스 0은 빈 문자열(검은 하늘). |

### MapLoader 공개 API

| 메서드 | 설명 |
|---|---|
| `LoadBlockData(string filepath)` | BD1 파일 경로에서 블록 메시 + 텍스처 로드. `blockRoot` 하위에 GameObject 생성. |
| `UnloadBlockData()` | `blockRoot` 하위 오브젝트 전부 제거. |
| `LoadSkyData(SkyData skyData, int textureIndex)` | 스카이 메시 + 지정 인덱스 텍스처 로드. `skyRoot` 하위에 GameObject 생성. `RenderSettings.skybox = null` 적용. |
| `UnloadSkyData()` | `skyRoot` 하위 오브젝트 전부 제거. |

### BD1 바이너리 포맷 (`openxops.net/filesystem-bd1.php`)

```
[텍스처 경로] 10개 × 31바이트 (ASCII null 종료)
[블록 수]    uint16 리틀 엔디안
[블록 반복]
  X[8] float32 → Y[8] float32 → Z[8] float32  (정점 좌표, 축별 분리)
  U[24] float32 → V[24] float32               (UV, 6면×4개 분리)
  텍스처 인덱스[6] int32                        (유효값 최하위 1바이트)
  활성 플래그 int32
```

- **좌표 변환**: `(-x, y, -z)` — Y축 기준 180° 회전 (XOPS 원점 정렬)
- **UV 변환**: `v = 1f - v` (DirectX V=0 상단 → Unity V=0 하단)
- **메시 빌드**: 블록당 1개의 `Mesh`. 사용된 고유 텍스처 수 = 서브메시 수. 각 서브메시에 해당 텍스처 면의 삼각형이 모임.
- **GameObject 위치**: 8개 정점의 평균 = 블록 중심. 메시 정점은 중심 기준 로컬 좌표.

### SkyData JSON 형식 (`StreamingAssets/unitydata/sky_data.json`)

```json
{
    "skyMeshPath": "data/sky/sky.x",
    "skyTexturePath": [
        "",
        "data/sky/sky1.bmp",
        "data/sky/sky2.bmp",
        ...
    ]
}
```

---

## Shaders

| 파일 | 셰이더 이름 | 역할 |
|---|---|---|
| `Shaders/SkyMesh.shader` | `UnityXOPS/SkyMesh` | 스카이 메시 전용 Unlit 셰이더. |

### SkyMesh 구현 메모

- **Queue**: `Background` — 씬 오브젝트보다 먼저 렌더링.
- **ZWrite Off / ZTest Always**: Unity 6 Reversed-Z(far=0.0) 환경에서 `ZTest LEqual`은 항상 실패하므로 `Always` 사용.
- **Cull Back**: sky.x 큐브는 내부 시점용으로 설계되어 안쪽에서 면이 front face(CCW). `Cull Front`를 사용하면 보이는 면이 모두 제거됨.
- **카메라 고정**: 버텍스 셰이더에서 `_WorldSpaceCameraPos + vertex`로 월드 위치 계산 — 오브젝트 트랜스폼 무시. `skyRoot`의 LateUpdate 위치 동기화는 프러스텀 컬링 방지 전용.
- **180° 보정**: 원본 XOPS 기준 정렬을 위해 X, Z 반전 (`float3(-x, y, -z)`) 적용.
- **UV**: sky.x UV의 U값이 음수 범위 — Repeat 래핑으로 아틀라스 정상 샘플링.
