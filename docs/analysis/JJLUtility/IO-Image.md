# IO/Image

## 개요
BMP, TGA, DDS, JPG, PNG 이미지 파일을 런타임에서 파싱하여 Unity `Texture2D`로 변환하는 모듈이다. `ImageLoader` 싱글톤이 진입점이며 포맷별 파서는 `partial class`로 파일을 분리한다. 최대 4096px 제한을 초과하면 쌍선형 보간으로 자동 다운스케일한다.

---

## 클래스 목록

### ImageLoader
- **파일**: `Assets/JJLUtility/Runtime/IO/Image/ImageLoader.cs`
- **역할**: 이미지 파일 로드 진입점, 텍스처 캐시 관리 싱글톤
- **주요 필드/프로퍼티**:
  - `k_maxTextureSize`(const int, 4096) — 텍스처 최대 허용 크기
  - `m_textureCache`(Dictionary\<string, int\> / Dictionary\<string, Texture2D\>) — 경로 → 캐시 인덱스(에디터) / 경로 → 텍스처(빌드)
  - `textureCacheList`(List\<Texture2D\>, `[SerializeField]`, 에디터 전용) — 인스펙터 직렬화용 텍스처 목록
- **주요 메서드**:
  - `LoadTexture(string filepath, FilterMode filter)` → `Texture2D` — 확장자 분기 후 포맷별 로더 호출, 캐시 히트 시 즉시 반환
  - `GetScaledPixels(int width, int height, Color32[] pixels)` → `(int, int, Color32[])` — 4096 초과 시 비율 유지 축소
  - `ResizePixels(Color32[] src, int srcW, int srcH, int dstW, int dstH)` → `Color32[]` — 쌍선형 보간 리샘플링
- **특이사항**:
  - 에디터에서는 `Dictionary<string, int>` + `List<Texture2D>` 조합으로 인스펙터 직렬화 지원; 빌드에서는 `Dictionary<string, Texture2D>` 단일 구조 사용
  - JPG/PNG는 Unity 내장 `LoadImage()` 사용, 나머지는 자체 파서

---

### BMPFile
- **파일**: `Assets/JJLUtility/Runtime/IO/Image/BMP/BMPFile.cs`
- **역할**: BMP 파싱 결과 컨테이너 + `ImageLoader` partial 구현 (BMP 로드 로직 포함)
- **주요 필드/프로퍼티**:
  - `FileHeader`(BMPFileHeader) — 파일 헤더
  - `InfoHeader`(BMPInfoHeader) — 이미지 정보 헤더
  - `Palettes`(Color32[]) — 팔레트(8비트 이하 모드에서만 할당)
  - `Pixels`(Color32[]) — 파싱된 픽셀 데이터
- **주요 메서드** (partial ImageLoader 내):
  - `LoadBMPFile(string filepath)` → `BMPFile` — 매직 바이트 검증 후 비트 심도·압축 방식에 따라 분기
  - `LoadBMPFileHeader(BinaryReader)` → `BMPFileHeader`
  - `LoadBMPInfoHeader(BinaryReader)` → `BMPInfoHeader`
  - `Load32BitBMPFile(BinaryReader, ref BMPFile)` → `bool` — 32bpp, 비트마스크 기반 채널 추출
  - `Load24BitBMPFile(BinaryReader, ref BMPFile)` → `bool` — 24bpp, 4바이트 행 정렬 처리
  - `Load16BitBMPFile(BinaryReader, ref BMPFile)` → `bool` — 16bpp, 비트마스크 기반
  - `LoadIndexedBMPFile(BinaryReader, ref BMPFile)` → `bool` — 1/4/8bpp 팔레트 인덱스, `BitReader` 사용
  - `LoadRLE4BMPFile(BinaryReader, ref BMPFile)` → `bool` — RLE4 압축 디코딩
  - `LoadRLE8BMPFile(BinaryReader, ref BMPFile)` → `bool` — RLE8 압축 디코딩
  - `FlipHeight(ref BMPFile)` — 수직 뒤집기 (RLE 계열에서 호출)
  - `GetShiftCount(uint mask)` → `int` — 마스크 최하위 세트 비트 위치 반환
  - `CountSetBits(uint n)` → `int` — 세트 비트 수 계산 (채널 비트 깊이 산출용)
- **특이사항**:
  - 비압축 계열은 `InfoHeader.Height > 0`이면 하단-상단(bottom-up) 순서로 저장되어 있어 `pixelY = y` (자연스럽게 뒤집힘), 음수 Height이면 top-down
  - RLE 계열은 항상 top-down 데이터이므로 디코딩 후 `FlipHeight` 호출

---

### BMPFileHeader
- **파일**: `Assets/JJLUtility/Runtime/IO/Image/BMP/BMPFileHeader.cs`
- **역할**: BMP 파일 헤더 14바이트 중 매직 이후 필드를 담는 구조체
- **주요 필드/프로퍼티**:
  - `Type`(const ushort, 0x4D42) — BMP 매직 "BM"
  - `Size`(uint) — 파일 전체 크기
  - `Offset`(uint) — 픽셀 데이터 시작 오프셋

---

### BMPInfoHeader
- **파일**: `Assets/JJLUtility/Runtime/IO/Image/BMP/BMPInfoHeader.cs`
- **역할**: BMP 이미지 정보 헤더 구조체 (BITMAPINFOHEADER 이상)
- **주요 필드/프로퍼티**:
  - `Size`(uint), `Width`(int), `Height`(int) — 헤더 크기 및 이미지 크기
  - `BitCount`(ushort) — 비트 심도 (1/4/8/16/24/32)
  - `Compression`(BMPCompression) — 압축 방식
  - `ColorUsed`(uint) — 팔레트 색상 수 (0이면 2^BitCount)
  - `RedMask`, `GreenMask`, `BlueMask`, `AlphaMask`(uint) — 채널 비트마스크 (BI_BITFIELDS 등에서 사용; 없는 경우 파서가 기본값 설정)

---

### BMPCompression
- **파일**: `Assets/JJLUtility/Runtime/IO/Image/BMP/BMPCompression.cs`
- **역할**: BMP 압축 방식 열거형
- **값**: `BI_RGB`(0), `BI_RLE8`(1), `BI_RLE4`(2), `BI_BITFIELDS`(3), `BI_JPEG`(4), `BI_PNG`(5), `BI_ALPHABITFIELDS`(6), `BI_CMYK`(11), `BI_CMYKRLE8`(12), `BI_CMYKRLE4`(13)
- **특이사항**: JPEG/PNG/CMYK 계열은 파서에서 오류 처리됨 (미지원)

---

### TGAFile
- **파일**: `Assets/JJLUtility/Runtime/IO/Image/TGA/TGAFile.cs`
- **역할**: TGA 파싱 결과 컨테이너 + `ImageLoader` partial 구현 (TGA 로드 로직 포함)
- **주요 필드/프로퍼티**:
  - `Header`(TGAHeader), `Palettes`(Color32[]), `Pixels`(Color32[])
- **주요 메서드** (partial ImageLoader 내):
  - `LoadTGAFile(string filepath)` → `TGAFile` — 헤더 파싱 후 ImageType 분기
  - `LoadTGAHeader(BinaryReader)` → `TGAHeader`
  - `LoadTGAColorMap(BinaryReader, ref TGAFile)` — 팔레트 로드
  - `LoadTrueColorTGA`, `LoadColorMappedTGA`, `LoadGrayscaleTGA` → `bool` — 각 ImageType 분기
  - `ReadTGARawPixels(BinaryReader, ref TGAFile, int depth, Color32[] palette, bool isGrayscale)` → `bool`
  - `ReadTGARLEPixels(BinaryReader, ref TGAFile, int depth, Color32[] palette, bool isGrayscale)` → `bool`
  - `ReadTGAColor(BinaryReader, int depth, int alphaBits)` → `Color32` — 32/24/16/15bpp 지원
  - `ReadTGAGrayscale(BinaryReader)` → `Color32`
  - `ReadTGAPaletteColor(BinaryReader, int indexDepth, Color32[] palette, int colorMapStart)` → `Color32`
- **특이사항**:
  - `TGAHeader.IsTopToBottom`이 true이면 파일 데이터가 top-down → Unity(bottom-up)로 변환 시 `dstY = height - 1 - y`
  - RLE 디코딩은 선형 배열에 먼저 채운 뒤 방향 보정 적용
  - 16bpp: `[A]RRRRRGGGGGBBBBB` 포맷, `alphaBits > 0`이면 최상위 1비트를 알파로 사용

---

### TGAHeader
- **파일**: `Assets/JJLUtility/Runtime/IO/Image/TGA/TGAHeader.cs`
- **역할**: TGA 파일 헤더 18바이트 구조체
- **주요 필드/프로퍼티**:
  - `IDLength`(byte), `ColorMapType`(byte), `ImageType`(TGAImageType)
  - `ColorMapStart`(ushort), `ColorMapLength`(ushort), `ColorMapDepth`(byte)
  - `XOrigin`(ushort), `YOrigin`(ushort), `Width`(ushort), `Height`(ushort)
  - `PixelDepth`(byte), `ImageDescriptor`(byte)
  - `AlphaBits`(int, 프로퍼티) — `ImageDescriptor & 0x0F`
  - `IsTopToBottom`(bool, 프로퍼티) — `(ImageDescriptor & 0x20) != 0`

---

### TGAImageType
- **파일**: `Assets/JJLUtility/Runtime/IO/Image/TGA/TGAImageType.cs`
- **역할**: TGA 이미지 데이터 타입 열거형
- **값**: `NoImage`(0), `ColorMapped`(1), `TrueColor`(2), `Grayscale`(3), `ColorMappedRLE`(9), `TrueColorRLE`(10), `GrayscaleRLE`(11)

---

### DDSFile
- **파일**: `Assets/JJLUtility/Runtime/IO/Image/DDS/DDSFile.cs`
- **역할**: DDS 파싱 결과 컨테이너 + `ImageLoader` partial 구현 (DDS 로드 전체 로직 포함)
- **주요 필드/프로퍼티**:
  - `Header`(DDSHeader), `DX10Header`(DDSHeaderDX10), `HasDX10Header`(bool), `Pixels`(Color32[])
- **주요 메서드** (partial ImageLoader 내):
  - `LoadDDSFile(string filepath)` → `DDSFile` — 매직 0x20534444 검증, DX10 확장 헤더 분기, 포맷 분기
  - `LoadDDSHeader`, `LoadDDSPixelFormat`, `LoadDDSHeaderDX10` — 헤더 파싱
  - `LoadFourCCDDS` — FourCC 분기 (DXT1~5, ATI1/2, BC4/5, DX10)
  - `LoadDX10DDS` — DXGI 포맷 분기
  - `LoadBC1DDS`~`LoadBC5DDS` — BC 블록 압축 디코딩
  - `LoadUncompressedDDS` — 비압축 RGB/RGBA (비트마스크 기반)
  - `LoadLuminanceDDS` — 루미넌스(단일 채널 회색조)
  - `LoadDX10RGBA8DDS`, `LoadDX10BGRA8DDS`, `LoadDX10B5G6R5DDS`, `LoadDX10B5G5R5A1DDS`, `LoadDX10B4G4R4A4DDS`, `LoadDX10R8DDS` — DX10 포맷별 디코더
  - `DecodeBC1Block`, `DecodeBC2Block`, `DecodeBC3Block`, `DecodeBC4Block`, `DecodeBC5Block` — 블록 단위 디코더
  - `DecodeBC3AlphaBlock` → `byte[16]` — BC3/BC4/BC5 공용 알파 보간 블록 디코딩
  - `DDSExpand565(ushort)` → `Color32` — RGB565 → RGB888 변환
  - `FlipDDS(ref DDSFile)` — DDS 픽셀 수직 뒤집기 (모든 포맷 공통 후처리)
- **특이사항**:
  - DDS 파일은 top-down 저장이므로 디코딩 후 `FlipDDS`로 항상 수직 뒤집기
  - 큐브맵, 볼륨 텍스처, 텍스처 배열은 미지원(오류 반환)
  - BC1에서 `c0 <= c1`이면 투명 픽셀(index=3 → alpha=0) 포함 4색 모드 → BC1_ARGB처럼 동작

---

### DDSHeader
- **파일**: `Assets/JJLUtility/Runtime/IO/Image/DDS/DDSHeader.cs`
- **역할**: DDS 기본 헤더 124바이트 구조체
- **주요 필드/프로퍼티**: `Size`, `Flags`(DDSFlags), `Height`, `Width`, `PitchOrLinearSize`, `Depth`, `MipMapCount`, `PixelFormat`(DDSPixelFormat), `Caps`(DDSCaps), `Caps2`(DDSCaps2)
- **메서드**: `HasFlag(DDSFlags)`, `HasCaps2(DDSCaps2)` — 플래그 확인 헬퍼

---

### DDSHeaderDX10
- **파일**: `Assets/JJLUtility/Runtime/IO/Image/DDS/DDSHeaderDX10.cs`
- **역할**: DX10 확장 헤더 20바이트 구조체 (FourCC == DX10일 때 추가로 읽음)
- **주요 필드/프로퍼티**: `DxgiFormat`(DXGIFormat), `ResourceDimension`(D3D10ResourceDimension), `MiscFlag`(uint), `ArraySize`(uint), `MiscFlags2`(uint)
- **메서드**: `IsCubeMap`(bool 프로퍼티) — `(MiscFlag & 0x4) != 0`

---

### DDSPixelFormat
- **파일**: `Assets/JJLUtility/Runtime/IO/Image/DDS/DDSPixelFormat.cs`
- **역할**: DDS 픽셀 포맷 32바이트 내부 구조체
- **주요 필드/프로퍼티**: `Size`, `Flags`(DDSPixelFormatFlags), `FourCC`(DDSFourCC), `RGBBitCount`, `RBitMask`, `GBitMask`, `BBitMask`, `ABitMask`
- **메서드**: `HasFlag(DDSPixelFormatFlags)` — 플래그 확인 헬퍼

---

### DDSFlags
- **파일**: `Assets/JJLUtility/Runtime/IO/Image/DDS/DDSFlags.cs`
- **역할**: DDS 헤더 유효 필드 플래그 열거형 (`[Flags]`)
- **값**: `Caps`, `Height`, `Width`, `Pitch`, `PixelFormat`, `MipMapCount`, `LinearSize`, `Depth`

---

### DDSPixelFormatFlags
- **파일**: `Assets/JJLUtility/Runtime/IO/Image/DDS/DDSPixelFormatFlags.cs`
- **역할**: 픽셀 포맷 구조체 유효 필드 플래그 열거형 (`[Flags]`)
- **값**: `AlphaPixels`, `Alpha`, `FourCC`, `RGB`, `YUV`, `Luminance`

---

### DDSCaps
- **파일**: `Assets/JJLUtility/Runtime/IO/Image/DDS/DDSCaps.cs`
- **역할**: DDS 텍스처 복잡도 플래그 열거형 (`[Flags]`)
- **값**: `Complex`(0x8), `MipMap`(0x400000), `Texture`(0x1000)

---

### DDSCaps2
- **파일**: `Assets/JJLUtility/Runtime/IO/Image/DDS/DDSCaps2.cs`
- **역할**: 큐브맵/볼륨 텍스처 여부 플래그 열거형 (`[Flags]`)
- **값**: `CubeMap`, `CubeMapPositiveX/NegativeX/Y/Z`, `Volume`

---

### DDSFourCC
- **파일**: `Assets/JJLUtility/Runtime/IO/Image/DDS/DDSFourCC.cs`
- **역할**: DDS FourCC 압축 포맷 식별자 열거형
- **값**: `DXT1`~`DXT5`, `ATI1`, `ATI2`, `BC4U`, `BC4S`, `BC5U`, `BC5S`, `DX10`

---

### D3D10ResourceDimension
- **파일**: `Assets/JJLUtility/Runtime/IO/Image/DDS/D3D10ResourceDimension.cs`
- **역할**: DX10 헤더의 리소스 차원 열거형
- **값**: `Unknown`(0), `Buffer`(1), `Texture1D`(2), `Texture2D`(3), `Texture3D`(4)

---

### DXGIFormat
- **파일**: `Assets/JJLUtility/Runtime/IO/Image/DDS/DXGIFormat.cs`
- **역할**: DXGI 텍스처 픽셀 포맷 열거형
- **특이사항**: 파서에서 실제 지원하는 포맷은 BC1~BC5(UNorm/SNorm/sRGB), R8G8B8A8, B8G8R8A8/X8, B5G6R5, B5G5R5A1, B4G4R4A4, R8, A8 에 한정됨
