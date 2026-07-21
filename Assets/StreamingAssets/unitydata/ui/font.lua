-- ============================================================
--  폰트 모드 스크립트 (font.lua)
--  게임 시작 시 1회 실행된다. 여기서 게임 전체가 쓸 글꼴을 바꿀 수 있다.
--  아무것도 안 하면 OS 언어에 맞는 기본 글꼴이 쓰이고, 빠진 글자는 나머지 기본
--  글꼴이 자동으로 메우므로 두부(□)로 보이는 일은 없다.
--  전체 API 레퍼런스는 docs/lua-font-guide.md 참고.
-- ============================================================


-- ----- 스프라이트 시트 폰트 -----
-- HUD 숫자와 짧은 라벨에 쓰는 시트 글꼴. 시트 한 장은 16x16칸 = 칸 256개고, 칸 하나가 32x32px이다.
-- 0번 시트는 ASCII라 반드시 등록해야 한다. 안 하면 HUD 숫자와 라벨이 전부 사라진다.

-- 직접 그린 시트를 쓰고 싶으면 번호를 붙여 등록하고, 텍스트마다 SetPage로 골라 쓴다.
-- 텍스트 하나는 시트 한 장만 쓴다. 글자 코드가 곧 그 시트 안의 칸 번호(0~255)다.
-- XOPS.Font:SetSpritePage(1, "addon/icons.dds")
--
--   local t = XOPS.UI:CreateText(...)
--   t:SetPage(1)

XOPS.Font:SetSpritePage(0, "data/char.dds")
XOPS.Font:SetSpritePage(1, "unitydata/ui/char_ex.png")

-- ----- 사용 예시 -----
-- 시스템에 설치된 글꼴을 기본으로 쓰고, 굵기별 글꼴을 따로 지정한다.
--
-- local regular = XOPS.Font:CreateFromOS("Segoe UI", "Regular")
-- local italic  = XOPS.Font:CreateFromOS("Segoe UI", "Italic")
-- local bold    = XOPS.Font:CreateFromOS("Segoe UI", "Bold")
--
-- if regular:IsValid() then
--     regular:SetItalic(400, italic)
--     regular:SetRegular(700, bold)
--     XOPS.Font:SetOSFont(regular)
-- end

-- 모드에 동봉한 글꼴 파일을 쓴다. 경로는 StreamingAssets 기준.
--
-- local myfont = XOPS.Font:CreateFromFile("addon/myfont.ttf", 0)
-- if myfont:IsValid() then
--     XOPS.Font:SetOSFont(myfont)
-- end


XOPS.Debug:Log("폰트 로드 완료")
