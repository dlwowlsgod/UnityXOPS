-- ============================================================
--  설정 모드 스크립트 (config.lua)
--  ConfigManager가 config.json을 로드한 직후, 화면 적용 전에 1회 실행된다.
--  여기서 모드 전용 설정을 등록한다. 유저가 config.json에서 값을 바꿔뒀으면 그 값이 우선한다.
--
--  [ 섹션 추가 ]  (선택)
--    XOPS.Config:AddSection("MyMod")
--
--  [ 설정 추가 ]  XOPS.Config:AddSetting(섹션, 이름, 타입, 기본값, 최소, 최대)
--    타입: "int" / "float" / "bool" / "string"
--    최소/최대는 int / float 에서 최소 < 최대 일 때만 클램프 범위로 쓰인다. 최소 >= 최대(예: 0, 0)면 무제한.
--      예) XOPS.Config:AddSetting("General", "showFps",    "bool",  false, 0, 0)
--      예) XOPS.Config:AddSetting("Input",   "invertY",    "bool",  false, 0, 0)
--      예) XOPS.Config:AddSetting("MyMod",   "difficulty", "int",   1,     0, 3)
--
--  [ 값 읽기 ]  GetInt / GetFloat / GetBool / GetString (섹션, 이름)
--  [ 값 쓰기 ]  SetInt / SetFloat / SetBool / SetString (섹션, 이름, 값)  →  파일 반영은 Save()
--
--  전체 API 레퍼런스는 별도 매뉴얼 문서 참고.
-- ============================================================


XOPS.Debug:Log("설정 로드 완료")
