-- ============================================================
--  메인메뉴 씬 스크립트 (mainmenu.lua)
--  Hierarchy에 미리 만들어 둔 UI를 점진적으로 Lua 동적 생성으로 옮긴다.
-- ============================================================

local M = {}

-- ----- 레이어 -----
local TITLE_LAYER   = 1001
local VERSION_LAYER = 1002
local SCROLL_LAYER  = 1003
local MISSION_LAYER = 1004
local MENU_LAYER    = 1005
local FADE_LAYER  = 1010
local POINTER_LAYER = FADE_LAYER - 1

-- 레이어 스케일 모드. false면 Constant Pixel Size, scaleFactor 1,
-- true면 640x480 기준 Scale With Screen Size(높이 매치).
local SCALING = false

-- ----- 타이틀 이미지 -----
local TITLE = {
    pivot = "TopLeft",
    path  = "data/title.dds",
    x = 20, y = -25, w = 480, h = 80,
}

-- ----- 데모 배경 카메라 -----
local CAM_OFFSET = { x = 0.4, y = 2.2, z = 1.2 }  -- 플레이어 기준 월드 오프셋
local CAM_PITCH  = 25.0                           -- 고정 피치(아래로)
local CAM_YAW    = 225.0                          -- 고정 요
local CAM_FOV    = 65.0                           -- 시야각(도)

-- ----- 페이드 인 (진입 시 검정→투명. 페이드 아웃 없음 — 미션 선택 시 즉시 전환) -----
local FADE_TIME        = 2.0   -- 검정 → 투명에 걸리는 시간(초)
local CLICK_ALLOW_TIME = 0.2   -- 진입 후 이 시간(초) 지나야 좌클릭 인정(직전 씬 클릭 누수 방지)

local fadeHandle = nil

-- ----- 마우스 십자 커서 -----
local POINTER_COLOR = { r = 1, g = 0, b = 0, a = 0.5 }  -- 커서 색(0~1)
local hLineHandle = nil
local vLineHandle = nil

-- ----- 게임 버전 텍스트 (우상단) -----
local VERSION = {
    x = -10, y = -75, w = 16, h = 19,
    text   = { r = 1, g = 1, b = 1 },  -- 본문 색(0~1)
    shadow = { r = 0, g = 0, b = 0 },  -- 그림자 색(0~1)
}

-- ----- 스크롤바 (미션 리스트 옆) -----
-- 트랙(배경 홈) + 손잡이(움직이는 부분 = 테두리+안쪽 2겹). 손잡이 색은 상태별(평상/호버/눌림) 교체.
local SCROLL_TRACK = { x = 0, y = 39, w = 20, h = 300, r = 0.5, g = 0.5, b = 0.5, a = 0.5 }  -- 트랙 위치/크기/색
local SCROLL_THUMB_HEIGHT = 100   -- 손잡이 초기 높이(실제론 항목 수에 맞춰 자동 계산)
local SCROLL_BORDER = 3           -- 손잡이 테두리 두께(px)
-- 손잡이 테두리 색 / 안쪽 색 (평상/호버/눌림)
local SCROLL_OUTLINE = {
    normal  = { r = 0.6, g = 0.6,    b = 0.251 },
    hover   = { r = 0.4, g = 0.6705, b = 0.5686 },
    pressed = { r = 0.6, g = 0.3019, b = 0.2509 },
}
local SCROLL_INNER = {
    normal  = { r = 0.8,    g = 0.8,    b = 0.2509 },
    hover   = { r = 0.3803, g = 0.7686, b = 0.6392 },
    pressed = { r = 0.8,    g = 0.3019, b = 0.2509 },
}
local trackHandle = nil   -- 트랙(배경) 핸들 — 스크롤 불필요 시 색 교체
local thumbHandle = nil   -- 테두리(마우스 판정) 핸들
local innerHandle = nil   -- 안쪽 색 핸들

-- ----- 씬을 넘어 유지되는 메뉴 상태 키 (XOPS.State에 저장) -----
-- 미션 골라 브리핑 갔다 돌아와도 스크롤 위치/탭이 유지되게 하는 키들. 저장은 상호작용 시, 복원은 M.start에서.
local STATE = {
    officialScroll = "mm.officialscrollindex",
    addonScroll    = "mm.addonscrollindex",
    isAddonTab     = "mm.isaddontab",
    addonPage      = "mm.addonpage",
}

-- ----- 미션 리스트 컨테이너 (스크롤바 왼쪽, 미션 항목이 담기는 영역) -----
local MISSION_RECT = { x = -20, y = 39, w = 340, h = 300, r = 0, g = 0, b = 0, a = 0.5 }
local missionRectHandle = nil

-- ----- 미션 리스트 줄 (UP 버튼 + 8칸 + DOWN 버튼 = 10줄) -----
local ITEM_COUNT   = 8      -- 한 번에 보이는 미션 칸 수
local ITEM_SPACING = 30     -- 줄 간격(px)
local ITEM_FONT_W  = 20     -- 미션 이름 글자 폭
local BUTTON_FONT_W = ITEM_FONT_W + 5  -- UP/DOWN 글자 폭
local ITEM_FONT_H  = 26     -- 글자 높이
local UP_TEXT      = "<  UP  >"
local DOWN_TEXT    = "< DOWN >"
local ITEM_SHADOW  = { r = 0,   g = 0,   b = 0 }    -- 그림자 색
local BTN_NORMAL   = { r = 1,   g = 1,   b = 1 }    -- UP/DOWN 평상(흰)
local BTN_HOVER    = { r = 0,   g = 1,   b = 1 }    -- UP/DOWN 호버(시안)
local BTN_DISABLED = { r = 0.6, g = 0.6, b = 0.6 }  -- UP/DOWN 비활성(스크롤 끝)
local ITEM_NORMAL  = { r = 0.6, g = 0.6, b = 1 }    -- 미션 항목 평상
local ITEM_HOVER   = { r = 1,   g = 0.6, b = 0.6 }  -- 미션 항목 호버
local itemSlots = {}
local upSlot = nil
local downSlot = nil

-- ----- 공식/애드온 탭 스위치 (미션 리스트 아래 버튼) -----
local SWITCH_RECT = { x = 0, y = 14, w = MISSION_RECT.w + SCROLL_TRACK.w, h = 25, r = 0, g = 0, b = 0, a = 0.5 }  -- 배경 위치/크기/색
local SWITCH_TO_ADDON    = "ADD-ON MISSIONS >>"    -- official 모드에서 표시(누르면 애드온)
local SWITCH_TO_OFFICIAL = "<< STANDARD MISSIONS"  -- addon 모드에서 표시(누르면 공식)
local SWITCH_FONT_W = 17     -- 글자 폭
local SWITCH_FONT_H = 22     -- 글자 높이
local switchBgHandle = nil
local switchSlot = nil
local addonExists = false

-- ----- 애드온 페이지 전환 바 (미션 리스트 바로 위) -----
-- << 이전 페이지 / 가운데 페이지 이름 / >> 다음 페이지. 애드온 탭일 때만 표시.
-- 폰트/크기는 공식·애드온 스위치와 동일(SWITCH_FONT_*).
local PAGE_RECT = { x = 0, y = MISSION_RECT.y + MISSION_RECT.h, w = MISSION_RECT.w + SCROLL_TRACK.w, h = SWITCH_RECT.h, r = 0, g = 0, b = 0, a = 0.5 }
local PAGE_PREV = "<<"       -- 이전 페이지 화살표
local PAGE_NEXT = ">>"       -- 다음 페이지 화살표
local PAGE_ARROW_HIT = 60    -- 화살표 클릭 판정 폭(px)
local pageBgHandle = nil
local pagePrevSlot = nil
local pageNextSlot = nil
local pageNameSlot = nil

-- ----- 좌하단 메뉴 버튼 (OPTION / CREDIT / EXIT) -----
local MENU_ROW_H = SWITCH_RECT.h   -- 버튼 한 줄 높이
local MENU_BG = { x = 5, y = 14, w = SWITCH_FONT_W * 10, h = MENU_ROW_H * 3, r = 0, g = 0, b = 0, a = 0.5 }  -- 배경 위치/크기/색
local MENU_ITEMS = { "< OPTION >", "< CREDIT >", "<  EXIT  >" }  -- 버튼 문구(위→아래)
local menuBgHandle = nil
local menuSlots = {}

-- ----- 좌하단 돌아가기 버튼 -----
local BACK_ITEM = "< BACK >"
local BACK_BG = { x = 5, y = 14, w = SWITCH_FONT_W * 8, h = MENU_ROW_H, r = 0, g = 0, b = 0, a = 0.5 }
local backBgHandle = nil
local backBgSlots = {}

-- ----- CREDIT 창 (CREDIT 버튼 → 서브 화면) -----
-- 배경: 화면 풀스트레치에서 각 가장자리 여백만큼 안쪽으로. 텍스트: GlobalData(제품/버전/회사/라이선스)를 정중앙에.
local CREDIT_INSET = { left = 152, top = 109, right = 14, bottom = 14 }  -- 화면 가장자리에서의 여백(px)
local CREDIT_BG    = { r = 0, g = 0, b = 0, a = 0.5 }                    -- 배경 색
-- 글자 크기는 Auto Size(패널 안에 맞게 자동 조절). 아래는 자동 조절 범위(pt).
local CREDIT_FONT_MIN = 1    -- 최소 글자 크기(pt)
local CREDIT_FONT_MAX = 80   -- 최대 글자 크기(pt)
local creditPanelHandle = nil

-- ----- 현재 뷰/상호작용 상태 (일부는 XOPS.State에 저장) -----
local isAddon     = false   -- 공식(false)/애드온(true) 탭
local page        = 0       -- 애드온 페이지
local scrollIndex = 0       -- 맨 위에 보이는 미션 인덱스
local scrollable  = false   -- 항목이 8칸 초과라 스크롤이 필요한지
local dragging    = false   -- 스크롤바 손잡이 드래그 중
local grabOffset  = 0       -- 드래그 시작 시 손잡이에서 잡은 지점(px)
local screen      = "main"  -- 현재 화면: "main"(미션 리스트) / "option" / "credit"

-- 스크롤 썸 상태(normal/hover/pressed)에 맞춰 테두리·이너 색을 적용한다.
-- state: SCROLL_OUTLINE/SCROLL_INNER의 키 문자열
local function applyScrollState(state)
    local o, i = SCROLL_OUTLINE[state], SCROLL_INNER[state]
    thumbHandle:SetColor(o.r, o.g, o.b, 1)
    innerHandle:SetColor(i.r, i.g, i.b, 1)
end

-- 현재 뷰(official 또는 addon[page])의 미션 개수.
local function currentCount()
    if isAddon then return XOPS.Data:GetAddonMissionCount(page) end
    return XOPS.Data:GetOfficialMissionCount()
end

-- 현재 뷰에서 미션 인덱스 mi의 이름.
local function currentName(mi)
    if isAddon then return XOPS.Data:GetAddonMissionName(page, mi) end
    return XOPS.Data:GetOfficialMissionName(mi)
end

-- 애드온 탭/스위치를 열 수 있는지: 유저 추가 페이지가 있거나(폴백 외 페이지), 폴백 page 0에 미션이 있으면 true.
-- 경로가 하나도 없어도 폴백에 미션이 있으면 page 0만 보여준다.
local function addonBrowsable()
    return XOPS.Data:GetAddonPageCount() > 1 or XOPS.Data:GetAddonMissionCount(0) > 0
end

-- 페이지 전환 바(<< >>)를 띄울지: 폴백 외 추가 페이지가 있어(페이지 2개 이상) 넘길 대상이 있으면 true.
local function multiplePages()
    return XOPS.Data:GetAddonPageCount() > 1
end

-- parent 아래에 그림자(먼저, +1/-1)+본문 텍스트 쌍 생성. 그림자가 본문 아래에 깔린다.
-- fontW/fontH=글자 크기, hitW/hitH=히트 rect(줄 전체, 폰트 폭과 별개 — 호버/클릭만 줄 전체).
-- pivot=UI 요소 앵커/피벗 기준점(생략 시 TopLeft), align=글자 정렬 기준점(생략 시 TopLeft).
-- baseY=본문 원위치 y를 함께 반환(눌림 효과에서 원위치 복원용).
local function makeTextPair(parent, y, text, color, fontW, fontH, hitW, hitH, pivot, align)
    pivot = pivot or "TopLeft"
    align = align or "TopLeft"
    local s = ITEM_SHADOW
    local shadow = parent:CreateChildText(pivot, align, text,
        1, y - 1, fontW, fontH, 0, s.r, s.g, s.b, 1)
    shadow:SetSize(hitW, hitH)
    local main = parent:CreateChildText(pivot, align, text,
        0, y, fontW, fontH, 0, color.r, color.g, color.b, 1)
    main:SetSize(hitW, hitH)
    return { shadow = shadow, main = main, baseY = y }
end

-- 미션 리스트 줄(컨테이너 자식). fontWidth 생략 시 ITEM_FONT_W. 히트는 줄 전체(컨테이너 폭 × 간격).
local function makeItemText(y, text, color, fontWidth)
    return makeTextPair(missionRectHandle, y, text, color, fontWidth or ITEM_FONT_W, ITEM_FONT_H, MISSION_RECT.w, ITEM_SPACING)
end

-- UP/DOWN 버튼 색 적용: 스크롤 끝이면 비활성 회색 우선, 아니면 호버 시안/평상 흰색.
local function applyButtonColor(slot, disabled, hovered)
    local c = disabled and BTN_DISABLED or (hovered and BTN_HOVER or BTN_NORMAL)
    slot.main:SetColor(c.r, c.g, c.b, 1)
end

-- 눌림 효과: pressed면 본문을 (1,-1) 이동해 그림자와 겹쳐 눌린 느낌, 아니면 원위치(baseY).
local function setPressed(slot, pressed)
    if pressed then
        slot.main:SetPosition(1, slot.baseY - 1)
    else
        slot.main:SetPosition(0, slot.baseY)
    end
end

-- scrollIndex~+7 미션 이름을 8칸(그림자+본문)에 반영. 데이터보다 많은 칸은 숨긴다.
local function refreshItems()
    local count = currentCount()
    for i = 0, ITEM_COUNT - 1 do
        local slot = itemSlots[i]
        local mi = scrollIndex + i
        if mi < count then
            local name = currentName(mi)
            slot.shadow:SetText(name)
            slot.main:SetText(name)
            slot.shadow:SetActive(true)
            slot.main:SetActive(true)
        else
            slot.shadow:SetActive(false)
            slot.main:SetActive(false)
        end
    end
end

-- 스크롤바 손잡이를 현재 미션 수 + scrollIndex에 맞춘다.
--  스크롤 필요(항목>8칸): 손잡이 높이=보이는 비율, 위치=scrollIndex 비례, 트랙 회색.
--  불필요(항목<=8칸): 손잡이 숨김 + 트랙을 컨테이너 색으로 → 스크롤바가 배경에 녹아 사라진 듯.
local function updateScrollThumb()
    local count = currentCount()
    scrollable = count > ITEM_COUNT
    if scrollable then
        local barHeight = SCROLL_TRACK.h * ITEM_COUNT / count
        local maxIndex = count - ITEM_COUNT
        local trackRange = SCROLL_TRACK.h - barHeight
        thumbHandle:SetActive(true)
        thumbHandle:SetSize(0, barHeight)
        thumbHandle:SetPosition(0, -trackRange * scrollIndex / maxIndex)
        trackHandle:SetColor(SCROLL_TRACK.r, SCROLL_TRACK.g, SCROLL_TRACK.b, SCROLL_TRACK.a)
    else
        thumbHandle:SetActive(false)
        trackHandle:SetColor(MISSION_RECT.r, MISSION_RECT.g, MISSION_RECT.b, MISSION_RECT.a)
    end
end

-- 현재 모드에 맞춰 스위치 문구 갱신(official→"ADD-ON >>", addon→"<< STANDARD").
local function updateSwitchText()
    local txt = isAddon and SWITCH_TO_OFFICIAL or SWITCH_TO_ADDON
    switchSlot.shadow:SetText(txt)
    switchSlot.main:SetText(txt)
end

-- 현재 페이지 이름을 페이지 바 가운데 텍스트에 반영. 이름이 없으면(nil/"") 빈칸.
local function updatePageName()
    local name = XOPS.Data:GetAddonPageName(page) or ""
    pageNameSlot.shadow:SetText(name)
    pageNameSlot.main:SetText(name)
end

-- 페이지 바 표시/숨김. 표시될 때 이름도 함께 갱신.
-- visible: 표시 여부
local function setPageBarVisible(visible)
    pageBgHandle:SetActive(visible)
    if visible then updatePageName() end
end

-- 탭 전환. 현재 탭 스크롤을 저장하고 새 탭 스크롤을 복원한 뒤 리스트/스크롤바/문구/페이지바를 갱신한다.
local function switchTab(toAddon)
    XOPS.State:Set(isAddon and STATE.addonScroll or STATE.officialScroll, scrollIndex)
    isAddon = toAddon
    XOPS.State:Set(STATE.isAddonTab, isAddon)
    scrollIndex = XOPS.State:Get(isAddon and STATE.addonScroll or STATE.officialScroll, 0)
    local maxIndex = math.max(0, currentCount() - ITEM_COUNT)
    if scrollIndex > maxIndex then scrollIndex = maxIndex end
    refreshItems()
    updateScrollThumb()
    updateSwitchText()
    setPageBarVisible(isAddon and multiplePages())
end

-- 페이드 인 알파(1 검정 → 0 투명): 0~FADE_TIME 동안 1→0, 이후 0.
-- t: 경과초
local function fadeInValue(t)
    if t >= FADE_TIME then return 0 end
    return 1 - t / FADE_TIME
end

-- 좌클릭 소비 게이트. 오프닝에서 스킵으로 누른 클릭이 새어 들어와 즉시 처리되는 것을 막는다.
-- 진입 후 CLICK_ALLOW_TIME 초가 지난 뒤의 fire 입력만 true. 앞으로 Lua 버튼/스크롤바가
-- WasPressed("fire") 대신 이 게이트를 통해 클릭을 받는다.
-- t: 경과초 / 반환: 이번 프레임에 인정된 클릭이면 true
local function firePressed(t)
    return t >= CLICK_ALLOW_TIME and XOPS.Input:WasPressed("fire")
end

-- 메인(미션 리스트) ↔ 서브(OPTION/CREDIT) 화면 전환.
-- GlobalData(제품/버전/회사/라이선스)를 여러 줄로 이어붙여 CREDIT 본문 문자열을 만든다.
local function buildCreditText()
    local parts = {
        XOPS.Data:GetProductName() .. "  v" .. XOPS.Data:GetVersion(),
        XOPS.Data:GetCompanyName(),
        "",
        XOPS.Data:GetLicenseType() .. " " .. XOPS.Data:GetCompanyName(),
        XOPS.Data:GetLicenseName(),
        "",
    }
    local lines = XOPS.Data:GetLicenseLines()
    for i = 1, #lines do
        parts[#parts + 1] = lines[i]
    end
    return table.concat(parts, "\n")
end

-- 화면 전환. s = "main" / "option" / "credit".
-- main: 미션 컨테이너/스크롤바/탭 스위치 + 하단 메뉴 ON, BACK/서브패널 OFF.
-- 서브: 위 전부 OFF, BACK ON + 해당 서브 패널만 ON (credit만 구현, option은 아직 빈 화면).
local function setScreen(s)
    screen = s
    local main = (s == "main")
    missionRectHandle:SetActive(main)
    trackHandle:SetActive(main)
    switchBgHandle:SetActive(main and addonExists)   -- 탭 스위치는 메인 + 애드온 있을 때만
    menuBgHandle:SetActive(main)
    backBgHandle:SetActive(not main)
    creditPanelHandle:SetActive(s == "credit")
    setPageBarVisible(main and isAddon and multiplePages())   -- 페이지 바는 메인 + 애드온 탭 + 페이지 2개 이상일 때만
end

function M.start()
    -- 씬 넘어 유지된 메뉴 상태 복원(없으면 기본값 official/page0/top).
    isAddon     = XOPS.State:Get(STATE.isAddonTab, false)
    addonExists = addonBrowsable()
    if isAddon and not addonExists then isAddon = false end   -- 애드온 없는데 addon 모드로 복원되면 official로
    page        = XOPS.State:Get(STATE.addonPage, 0)
    local maxPage = XOPS.Data:GetAddonPageCount() - 1   -- 페이지가 줄었을 수 있으니 범위 클램프
    if page > maxPage then page = math.max(0, maxPage) end
    scrollIndex = XOPS.State:Get(isAddon and STATE.addonScroll or STATE.officialScroll, 0)

    -- 마우스 커서: 창 안 숨김, 자유 이동(고정 안 함), 시작 시 중앙으로.
    XOPS.Input:SetMouseCursor(true, false, true)

    XOPS.UI:CreateImage(TITLE_LAYER, SCALING, TITLE.pivot, TITLE.path,
        TITLE.x, TITLE.y, TITLE.w, TITLE.h, 1, 1, 1, 1)

    XOPS.Camera:SetFieldOfView(CAM_FOV)

    -- 게임 버전: 그림자 먼저(아래), 본문 나중(위). 둘 다 우상단 pivot + 우측 정렬.
    local ver = XOPS.Data:GetVersion()
    local vt, vs = VERSION.text, VERSION.shadow
    XOPS.UI:CreateText(VERSION_LAYER, SCALING, "TopRight", "TopRight", ver,
        VERSION.x + 1, VERSION.y - 1, VERSION.w, VERSION.h, 0, vs.r, vs.g, vs.b, 1)
    XOPS.UI:CreateText(VERSION_LAYER, SCALING, "TopRight", "TopRight", ver,
        VERSION.x, VERSION.y, VERSION.w, VERSION.h, 0, vt.r, vt.g, vt.b, 1)

    -- 스크롤바: 트랙(레이어 직속) → 썸(트랙 자식, 가로 스트레치) → 이너(썸 자식, 3px 인셋 풀스트레치).
    local trk = SCROLL_TRACK
    trackHandle = XOPS.UI:CreateImage(SCROLL_LAYER, SCALING, "BottomRight", "",
        trk.x, trk.y, trk.w, trk.h, trk.r, trk.g, trk.b, trk.a)
    -- 썸: 가로 풀폭(StretchTop) + 높이 SCROLL_THUMB_HEIGHT, 트랙 상단 정렬. 테두리색(normal)으로 시작.
    local on = SCROLL_OUTLINE.normal
    thumbHandle = trackHandle:CreateChildImage("StretchTop", "", 0, 0, 0, SCROLL_THUMB_HEIGHT, on.r, on.g, on.b, 1)
    -- 이너: 썸을 3px 인셋(풀스트레치 sizeDelta = -2*border). 안쪽색(normal)으로 시작.
    local inset = -2 * SCROLL_BORDER
    local ic = SCROLL_INNER.normal
    innerHandle = thumbHandle:CreateChildImage("StretchFull", "", 0, 0, inset, inset, ic.r, ic.g, ic.b, 1)

    -- 미션 아이템 컨테이너: 스크롤바 왼쪽 340x300. 아이템을 이 핸들 자식으로 채운다.
    -- 지금은 배치 확인용 반투명 패널(순수 컨테이너로 쓰려면 a=0).
    local mr = MISSION_RECT
    missionRectHandle = XOPS.UI:CreateImage(MISSION_LAYER, SCALING, "BottomRight", "",
        mr.x, mr.y, mr.w, mr.h, mr.r, mr.g, mr.b, mr.a)

    -- 10줄: 0=UP, 1~8=미션 아이템(변동), 9=DOWN. 각 줄 그림자+본문. 초기색은 normal, 호버/비활성은 update에서.
    upSlot = makeItemText(0, UP_TEXT, BTN_NORMAL, BUTTON_FONT_W)
    for i = 0, ITEM_COUNT - 1 do
        itemSlots[i] = makeItemText(-(i + 1) * ITEM_SPACING, "", ITEM_NORMAL)
    end
    downSlot = makeItemText(-(ITEM_COUNT + 1) * ITEM_SPACING, DOWN_TEXT, BTN_NORMAL, BUTTON_FONT_W)

    refreshItems()        -- 현재 뷰(복원된 isAddon/page/scroll)의 미션 이름 채우기
    updateScrollThumb()   -- 썸 높이를 현재 페이지 미션 수에 맞춤

    -- 탭 스위치: 미션 컨테이너 아래 배경 스트립 + 텍스트(자식). addon 0개면 배경째 숨김.
    local sw = SWITCH_RECT
    switchBgHandle = XOPS.UI:CreateImage(MISSION_LAYER, SCALING, "BottomRight", "",
        sw.x, sw.y, sw.w, sw.h, sw.r, sw.g, sw.b, sw.a)
    switchSlot = makeTextPair(switchBgHandle, 0, "", BTN_NORMAL, SWITCH_FONT_W, SWITCH_FONT_H, sw.w, sw.h)
    switchBgHandle:SetActive(addonExists)
    updateSwitchText()

    -- 애드온 페이지 전환 바: 미션 리스트 바로 위. 배경 스트립 + << / 페이지명 / >> (자식). addon 탭에서만 표시.
    local pg = PAGE_RECT
    pageBgHandle = XOPS.UI:CreateImage(MISSION_LAYER, SCALING, "BottomRight", "",
        pg.x, pg.y, pg.w, pg.h, pg.r, pg.g, pg.b, pg.a)
    pagePrevSlot = makeTextPair(pageBgHandle, 0, PAGE_PREV, BTN_NORMAL,
        SWITCH_FONT_W, SWITCH_FONT_H, PAGE_ARROW_HIT, pg.h, "left", "left")
    pageNextSlot = makeTextPair(pageBgHandle, 0, PAGE_NEXT, BTN_NORMAL,
        SWITCH_FONT_W, SWITCH_FONT_H, PAGE_ARROW_HIT, pg.h, "right", "right")
    pageNameSlot = makeTextPair(pageBgHandle, 0, "", BTN_NORMAL,
        SWITCH_FONT_W, SWITCH_FONT_H, pg.w, pg.h, "center", "center")
    setPageBarVisible(isAddon and multiplePages())

    -- 하단 메뉴 버튼: 반투명 검정 배경(BottomLeft) + OPTION/CREDIT/EXIT 텍스트(자식, 그림자+본문).
    -- 위에서부터 순서대로 쌓되 pivot이 BottomLeft라 y는 아래(0)에서 위로 증가 → 첫 항목이 맨 위.
    local mb = MENU_BG
    menuBgHandle = XOPS.UI:CreateImage(MENU_LAYER, SCALING, "BottomLeft", "",
        mb.x, mb.y, mb.w, mb.h, mb.r, mb.g, mb.b, mb.a)
    local menuCount = #MENU_ITEMS
    for i = 1, menuCount do
        local y = (menuCount - i) * MENU_ROW_H
        menuSlots[i] = makeTextPair(menuBgHandle, y, MENU_ITEMS[i], BTN_NORMAL,
            SWITCH_FONT_W, SWITCH_FONT_H, mb.w, MENU_ROW_H, "BottomLeft", "BottomLeft")
    end

    -- 돌아가기 버튼: 메뉴 버튼과 같은 좌하단 자리(스왑용). 시작은 메인 화면이라 숨김.
    local bb = BACK_BG
    backBgHandle = XOPS.UI:CreateImage(MENU_LAYER, SCALING, "BottomLeft", "",
        bb.x, bb.y, bb.w, bb.h, bb.r, bb.g, bb.b, bb.a)
    backBgSlots[1] = makeTextPair(backBgHandle, 0, BACK_ITEM, BTN_NORMAL,
        SWITCH_FONT_W, SWITCH_FONT_H, bb.w, MENU_ROW_H, "BottomLeft", "BottomLeft")
    backBgHandle:SetActive(false)

    -- CREDIT 창: 풀스트레치 배경(가장자리 인셋) + 정중앙 OS폰트 텍스트(GlobalData). 시작은 숨김.
    -- 인셋(left/top/right/bottom) → 풀스트레치 sizeDelta/anchoredPosition(pivot 중앙 기준) 환산.
    local ci = CREDIT_INSET
    creditPanelHandle = XOPS.UI:CreateImage(MENU_LAYER, SCALING, "StretchFull", "",
        (ci.left - ci.right) / 2, (ci.bottom - ci.top) / 2, -(ci.left + ci.right), -(ci.top + ci.bottom),
        CREDIT_BG.r, CREDIT_BG.g, CREDIT_BG.b, CREDIT_BG.a)
    local creditText = creditPanelHandle:CreateChildOSText("StretchFull", "Center", buildCreditText(),
        0, 0, CREDIT_FONT_MAX, 1, 1, 1, 1)
    creditText:SetAutoSize(true, CREDIT_FONT_MIN, CREDIT_FONT_MAX)   -- 패널 안에 맞게 글자 크기 자동 조절
    creditPanelHandle:SetActive(false)

    -- 마우스 십자 커서 두 줄. 가로선=풀폭 1px, 세로선=풀높이 1px (스트레치 축은 size 0 = 풀, 반대 축이 두께).
    local c = POINTER_COLOR
    hLineHandle = XOPS.UI:CreateImage(POINTER_LAYER, true, "StretchMiddle", "", 0, 0, 0, 1, c.r, c.g, c.b, c.a)
    vLineHandle = XOPS.UI:CreateImage(POINTER_LAYER, true, "StretchCenter", "", 0, 0, 1, 0, c.r, c.g, c.b, c.a)

    -- 풀스크린 검정(알파 1)으로 시작 → update에서 fadeInValue로 0까지 투명화.
    fadeHandle = XOPS.UI:CreateImage(FADE_LAYER, SCALING, "StretchFull", "", 0, 0, 0, 0, 0, 0, 0, 1)
end

function M.update(t, dt)
    -- 플레이어 위치 + 고정 오프셋으로 카메라를 따라붙이고 각도는 고정.
    -- 플레이어는 FixedUpdate에서 이동하므로 update 시점엔 위치가 확정돼 있다(LateUpdate 불필요).
    local p = XOPS.Map:GetPlayerTransform()
    if p then
        XOPS.Camera:GoTo(p.x + CAM_OFFSET.x, p.y + CAM_OFFSET.y, p.z + CAM_OFFSET.z)
        XOPS.Camera:SetEuler(CAM_PITCH, CAM_YAW, 0)
    end

    -- 페이드 인 알파 갱신
    fadeHandle:SetAlpha(fadeInValue(t))

    -- 십자 커서를 포인터 위치로 (가로선은 y만, 세로선은 x만 이동).
    local px = XOPS.UI:GetPointerX(POINTER_LAYER, SCALING)
    local py = XOPS.UI:GetPointerY(POINTER_LAYER, SCALING)
    hLineHandle:SetPosition(0, py)
    vLineHandle:SetPosition(px, 0)

    local clicked = firePressed(t)              -- 게이트 통과 좌클릭 1회
    local held = XOPS.Input:IsPressed("fire")   -- 좌클릭 유지 여부

    if screen == "main" then
        -- === 미션 리스트 화면 ===
        local count = currentCount()
        local maxIndex = math.max(0, count - ITEM_COUNT)

        -- UP/DOWN 클릭 → scrollIndex 수동 ±1 (경계 클램프).
        if clicked then
            local changed = false
            if upSlot.main:IsHovered() and scrollIndex > 0 then
                scrollIndex = scrollIndex - 1
                changed = true
            elseif downSlot.main:IsHovered() and scrollIndex < maxIndex then
                scrollIndex = scrollIndex + 1
                changed = true
            end
            if changed then
                refreshItems()
                updateScrollThumb()
            end
        end

        -- 탭 스위치 클릭 → official↔addon 토글 (addon 있을 때만).
        if clicked and addonExists and switchSlot.main:IsHovered() then
            switchTab(not isAddon)
        end

        -- 스크롤바 드래그: 트랙 위 클릭으로 시작. 잡은 지점(grabOffset) 유지하며 손잡이가 커서를 따라간다.
        -- 손잡이는 부드럽게 이동, 미션 인덱스는 내림. 놓으면 정수 scrollIndex 위치로 스냅.
        if scrollable then
            local barHeight  = SCROLL_TRACK.h * ITEM_COUNT / count
            local trackRange = SCROLL_TRACK.h - barHeight
            if clicked and trackHandle:IsHovered() then
                local barTopFromTop = trackRange * scrollIndex / maxIndex
                grabOffset = (SCROLL_TRACK.h - trackHandle:PointerLocalY()) - barTopFromTop
                if grabOffset < 0 then grabOffset = 0 elseif grabOffset > barHeight then grabOffset = barHeight end
                dragging = true
            end
            if dragging then
                if XOPS.Input:IsPressed("fire") then
                    local fromTop = (SCROLL_TRACK.h - trackHandle:PointerLocalY()) - grabOffset
                    local n = fromTop / trackRange
                    if n < 0 then n = 0 elseif n > 1 then n = 1 end
                    thumbHandle:SetPosition(0, -trackRange * n)
                    local newIndex = math.floor(n * maxIndex)
                    if newIndex ~= scrollIndex then
                        scrollIndex = newIndex
                        refreshItems()
                    end
                else
                    dragging = false
                    updateScrollThumb()
                end
            end
        end

        -- UP/DOWN 색+눌림 (드래그 중 억제, 눌림은 활성일 때만).
        local upHover = not dragging and upSlot.main:IsHovered()
        local downHover = not dragging and downSlot.main:IsHovered()
        applyButtonColor(upSlot, scrollIndex <= 0, upHover)
        applyButtonColor(downSlot, scrollIndex >= maxIndex, downHover)
        setPressed(upSlot, upHover and held and scrollIndex > 0)
        setPressed(downSlot, downHover and held and scrollIndex < maxIndex)

        -- 탭 스위치 색+눌림 (addon 있을 때만).
        if addonExists then
            local swHover = not dragging and switchSlot.main:IsHovered()
            applyButtonColor(switchSlot, false, swHover)
            setPressed(switchSlot, swHover and held)
        end

        -- 애드온 페이지 << 이전 / >> 다음: 경계에서 비활성(회색), 가능할 때만 클릭 처리. 페이지 바뀌면 스크롤 초기화.
        if isAddon and multiplePages() then
            local pageCount = XOPS.Data:GetAddonPageCount()
            local prevHover = not dragging and pagePrevSlot.main:IsHovered()
            local nextHover = not dragging and pageNextSlot.main:IsHovered()
            applyButtonColor(pagePrevSlot, page <= 0, prevHover)
            applyButtonColor(pageNextSlot, page >= pageCount - 1, nextHover)
            setPressed(pagePrevSlot, prevHover and held and page > 0)
            setPressed(pageNextSlot, nextHover and held and page < pageCount - 1)
            if clicked then
                local changed = false
                if prevHover and page > 0 then
                    page = page - 1
                    changed = true
                elseif nextHover and page < pageCount - 1 then
                    page = page + 1
                    changed = true
                end
                if changed then
                    scrollIndex = 0
                    XOPS.State:Set(STATE.addonPage, page)
                    XOPS.State:Set(STATE.addonScroll, scrollIndex)
                    refreshItems()
                    updateScrollThumb()
                    updatePageName()
                end
            end
        end

        -- 하단 메뉴 버튼: 색+눌림 + 클릭. OPTION(1)/CREDIT(2) → 서브 화면(컨텐츠 미구현), EXIT(3)은 아직 X.
        for i = 1, #menuSlots do
            local slot = menuSlots[i]
            local hovered = not dragging and slot.main:IsHovered()
            applyButtonColor(slot, false, hovered)
            setPressed(slot, hovered and held)
            if clicked and hovered then
                if i == 1 then          -- OPTION (컨텐츠 미구현 — 빈 서브 화면)
                    setScreen("option")
                    return
                elseif i == 2 then      -- CREDIT
                    setScreen("credit")
                    return
                end                     -- i == 3 (EXIT): 아직 미구현
            end
        end

        -- 미션 항목: 색+눌림 + 클릭 시 로드 (보이는 칸만).
        for i = 0, ITEM_COUNT - 1 do
            if scrollIndex + i < count then
                local slot = itemSlots[i]
                local hovered = not dragging and slot.main:IsHovered()
                local c = hovered and ITEM_HOVER or ITEM_NORMAL
                slot.main:SetColor(c.r, c.g, c.b, 1)
                setPressed(slot, hovered and held)
                if clicked and hovered then
                    XOPS.Scene:LoadMission(scrollIndex + i, isAddon, page)   -- 씬 전환 → 이후 처리 중단
                    return
                end
            end
        end

        -- 스크롤 썸 색: 드래그 중 pressed, 호버 hover, 아니면 normal.
        if scrollable then
            if dragging then
                applyScrollState("pressed")
            elseif thumbHandle:IsHovered() then
                applyScrollState("hover")
            else
                applyScrollState("normal")
            end
        end
    else
        -- === 서브 화면 (OPTION/CREDIT): 지금은 BACK 버튼만 ===
        local slot = backBgSlots[1]
        local hovered = slot.main:IsHovered()
        applyButtonColor(slot, false, hovered)
        setPressed(slot, hovered and held)
        if clicked and hovered then
            setScreen("main")   -- 미션 리스트로 복귀
            return
        end
    end
end

return M
