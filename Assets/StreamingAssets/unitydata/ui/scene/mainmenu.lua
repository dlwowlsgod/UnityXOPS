-- ============================================================
--  메인메뉴 씬 UI 스크립트 (mainmenu.lua)
--  Hierarchy에 미리 만들어 둔 UI를 점진적으로 Lua 동적 생성으로 옮긴다.
-- ============================================================

local M = {}

-- ----- 레이어 -----
-- 페이드(상위 레이어)보다 먼저 그려져 페이드에 덮인다.
local TITLE_LAYER = 1001

-- ----- 타이틀 이미지 (원본 GameTitleImage 이식) -----
-- 원본 RectTransform: anchor/pivot 좌상단(0,1), pos(20,-25), size(480,80), data/title.dds
local TITLE = {
    pivot = "TopLeft",
    path  = "data/title.dds",
    x = 20, y = -25, w = 480, h = 80,
}

function M.start()
    XOPS.UI:CreateImage(TITLE_LAYER, true, TITLE.pivot, TITLE.path,
        TITLE.x, TITLE.y, TITLE.w, TITLE.h, 1, 1, 1, 1)
end

return M
