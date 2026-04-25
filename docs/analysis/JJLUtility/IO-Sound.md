# IO/Sound

## 개요
WAV PCM 오디오 파일을 런타임에서 파싱하여 Unity `AudioClip`으로 변환하는 모듈이다. `SoundLoader` 싱글톤이 진입점이며 WAV 파싱 구현은 `WAVFile.cs` partial로 분리된다.

---

## 클래스 목록

### SoundLoader
- **파일**: `Assets/JJLUtility/Runtime/IO/Sound/SoundLoader.cs`
- **역할**: 오디오 파일 로드 진입점, AudioClip 캐시 관리 싱글톤
- **주요 필드/프로퍼티**:
  - `m_audioCache`(Dictionary\<string, int\> / Dictionary\<string, AudioClip\>) — 경로 → 캐시 인덱스(에디터) / 경로 → AudioClip(빌드)
  - `audioCacheList`(List\<AudioClip\>, `[SerializeField]`, 에디터 전용) — 인스펙터 직렬화용 AudioClip 목록
- **주요 메서드**:
  - `LoadAudio(string filepath)` → `AudioClip` — 확장자 검사 후 WAV 로더 호출, 캐시 히트 시 즉시 반환
- **특이사항**:
  - 현재 지원 확장자는 `.wav` 단독
  - 에디터/빌드 캐시 이중 구조는 `ImageLoader`, `ModelLoader`와 동일 패턴

---

### WAVFile (SoundLoader partial)
- **파일**: `Assets/JJLUtility/Runtime/IO/Sound/WAV/WAVFile.cs`
- **역할**: WAV RIFF 바이너리 파싱 후 `AudioClip` 생성
- **주요 메서드** (partial SoundLoader 내):
  - `LoadWAVFile(string filepath)` → `AudioClip`
- **파싱 흐름**:
  1. RIFF 매직("RIFF") + WAVE 타입("WAVE") 검증
  2. chunk 순회 — `fmt ` chunk에서 채널 수, 샘플 레이트, 비트 심도 읽기; `data` chunk에서 PCM 바이트 읽기; 나머지 chunk는 건너뜀
  3. `fmt ` chunk의 `audioFormat`이 1(PCM)이 아니면 오류 반환
  4. 8bpp: `(byte - 128) / 128f`로 float 변환; 16bpp: little-endian short → `/32768f`로 float 변환
  5. `AudioClip.Create(name, sampleCount, channels, sampleRate, stream: false)` + `SetData(samples, 0)` 로 AudioClip 생성
- **특이사항**:
  - 지원 비트 심도: 8비트, 16비트 PCM만 허용
  - RIFF 사양 준수: 홀수 크기 chunk 뒤에 패딩 바이트 1개 처리
  - `fmt ` chunk 크기가 16바이트 초과이면 나머지는 `ReadBytes(remaining)`으로 스킵
  - `data` chunk를 찾으면 루프 즉시 종료 (이후 chunk 무시)
