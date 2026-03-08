# JJLUtility — Runtime

namespace: `JJLUtility`, `JJLUtility.IO`

---

## Utility

| 파일 | 클래스 | 역할 |
|---|---|---|
| `Runtime/Utility/SingletonBehavior.cs` | `SingletonBehavior<T>` | MonoBehaviour 싱글톤 베이스. 인스턴스 없으면 GameObject 자동 생성, `DontDestroyOnLoad` 적용. 앱 종료 시 `null` 반환. |
| `Runtime/Utility/SafePath.cs` | `SafePath` | `Path.Combine` 래퍼. 디렉토리 트래버설 시도 시 `UnauthorizedAccessException` 발생. |
| `Runtime/Utility/BitReader.cs` | `BitReader` | `BinaryReader` 확장 (`System.IO` 네임스페이스). 비트 단위 또는 N비트 단위 읽기. 인덱스드 BMP 로딩에 사용. |

---

## Debug

| 파일 | 클래스 | 역할 |
|---|---|---|
| `Runtime/Debug/Debugger.cs` | `Debugger` (partial, static) | `UnityEngine.Debug` 씬 래퍼. 모든 메서드에 `[Conditional("UNITY_EDITOR")]` 적용 — 빌드에서 호출 자체가 컴파일 제거됨. |

---

## IO / Image

| 파일 | 클래스 | 역할 |
|---|---|---|
| `Runtime/IO/Image/ImageLoader.cs` | `ImageLoader` (partial, singleton) | 디스크 경로에서 `Texture2D` 로드 + 인메모리 캐시. 지원 포맷: `.jpg`, `.png`, `.bmp`, `.tga`, `.dds`. 에디터 빌드에서는 캐시를 `List<Texture2D>`로 직렬화해 Inspector에 표시. |

### BMP (`Runtime/IO/Image/BMP/`)

| 파일 | 클래스 | 역할 |
|---|---|---|
| `BMPFile.cs` | `BMPFile` + `ImageLoader` (partial) | BMP 파일 파싱 로직. `ImageLoader`의 partial 메서드로 분리. |
| `BMPFileHeader.cs` | `BMPFileHeader` | BMP 파일 헤더 데이터 구조체. |
| `BMPInfoHeader.cs` | `BMPInfoHeader` | BMP 정보 헤더 데이터 구조체. |
| `BMPCompression.cs` | `BMPCompression` | BMP 압축 방식 열거형. |

### TGA (`Runtime/IO/Image/TGA/`)

| 파일 | 클래스 | 역할 |
|---|---|---|
| `TGAFile.cs` | `TGAFile` + `ImageLoader` (partial) | TGA 파일 파싱 로직. TrueColor, ColorMapped, Grayscale + 각 RLE 변형 지원. |
| `TGAHeader.cs` | `TGAHeader` | TGA 파일 헤더 데이터 구조체. |
| `TGAImageType.cs` | `TGAImageType` | TGA 이미지 타입 열거형. |

### DDS (`Runtime/IO/Image/DDS/`)

| 파일 | 클래스 | 역할 |
|---|---|---|
| `DDSFile.cs` | `DDSFile` + `ImageLoader` (partial) | DDS 파일 파싱 로직. BC1~BC5, 비압축(RGB/Luminance), DX10 확장 헤더 지원. |
| `DDSHeader.cs` | `DDSHeader` | DDS 기본 헤더 구조체. |
| `DDSHeaderDX10.cs` | `DDSHeaderDX10` | DDS DX10 확장 헤더 구조체. |
| `DDSPixelFormat.cs` | `DDSPixelFormat` | DDS 픽셀 포맷 구조체. |
| `DDSFlags.cs` | `DDSFlags` | DDS 헤더 플래그 열거형. |
| `DDSPixelFormatFlags.cs` | `DDSPixelFormatFlags` | 픽셀 포맷 플래그 열거형. |
| `DDSCaps.cs` | `DDSCaps` | DDS Caps 플래그 열거형. |
| `DDSCaps2.cs` | `DDSCaps2` | DDS Caps2 플래그 열거형 (CubeMap, Volume 등). |
| `DDSFourCC.cs` | `DDSFourCC` | DDS FourCC 코드 열거형 (DXT1~DXT5, ATI1/2, BC4/5, DX10 등). |
| `DXGIFormat.cs` | `DXGIFormat` | DXGI 포맷 열거형. DX10 헤더에서 사용. |
| `D3D10ResourceDimension.cs` | `D3D10ResourceDimension` | D3D10 리소스 차원 열거형. |
