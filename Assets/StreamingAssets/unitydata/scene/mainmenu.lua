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

-- UIScale(설정) 배수를 적용할 컨텐츠 레이어들(전부 Constant Pixel Size). 포인터(scaling=true)/페이드는 제외.
local UI_SCALE_LAYERS = { TITLE_LAYER, VERSION_LAYER, SCROLL_LAYER, MISSION_LAYER, MENU_LAYER }

-- config General.UIScale 값을 위 레이어들에 배수로 적용한다(1=기본, 2/3/4=확대). 시작·변경·BACK·RESET 시 호출.
local function applyUIScale()
    local s = XOPS.Config:GetFloat("General", "UIScale")
    for i = 1, #UI_SCALE_LAYERS do
        XOPS.UI:SetScaleFactor(UI_SCALE_LAYERS[i], s)
    end
end

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

-- ----- EXIT 확인 팝업 (하단 < EXIT > 버튼 / ESC → 종료 확인) -----
-- 미션 선택을 숨기고 화면 중앙에 백패널 + 영문 질문 + < EXIT >(종료) / < ABORT >(취소). ESC로 열고 닫는다.
local EXIT_QUESTION = "Do you want to quit the game?"   -- 팝업 질문(영문)
local EXIT_YES = "< EXIT >"    -- 종료(Application.Quit)
local EXIT_NO  = "< ABORT >"   -- 취소(미션 선택으로 복귀)
-- 질문 글자 폭/높이, 패널 높이/색. 패널 폭은 질문 길이에 맞춰 자동 계산.
local EXIT_PANEL = { qFontW = 14, qFontH = 19, h = 130, r = 0, g = 0, b = 0, a = 0.5 }
local exitPanelHandle = nil
local exitYesSlot = nil
local exitNoSlot = nil

-- ----- OPTION 섹션 선택 바 (제목 아래) -----
-- << / 섹션 3개 / >> 로 설정 섹션(XOPS.Config)을 3개씩 페이지 넘기며 고른다. option 화면에서만 표시.
-- 폰트/높이는 하단 버튼(< BACK >)과 동일(SWITCH_FONT_*, MENU_ROW_H). 폭은 일단 타이틀(480)로 테스트.
local SECTION_PER_PAGE = 3
local SECTION_BAR = {
    x = TITLE.x, y = TITLE.y - TITLE.h - 10,   -- 제목 좌상단 기준으로 제목 높이만큼 내리고 10px 더 아래
    w = 640 - TITLE.x * 2, h = MENU_ROW_H,     -- 좌우 여백(Title.x)만큼 뺀 화면 폭(640 기준) = 600
    r = 0, g = 0, b = 0, a = 0.5,
}
local sectionBgHandle = nil
local sectionPrevSlot = nil
local sectionNextSlot = nil
local sectionSlots = {}
local sectionNames = nil     -- 섹션 이름 목록(로드 시 1회 캐시)
local sectionPage = 0        -- 섹션 선택 바 현재 페이지
local selectedSection = ""   -- 현재 선택된 설정 섹션 이름

-- ----- OPTION 설정 행 공통 레이아웃 (섹션 바 아래로 세로 나열) -----
-- 각 행 = 자기 배경 패널 하나 + 라벨 + 컨트롤(전부 한 패널 안). 폭은 내용에 맞춰 계산.
local OPTION_ROW_TOP = SECTION_BAR.y - SECTION_BAR.h - 10   -- 첫 행 top y(섹션 바 아래 10px)
local OPTION_ROW_PITCH = MENU_ROW_H + 5                     -- 행 간 세로 간격(패널 높이 + 5px)
local OPTION_GAP = 10                                       -- 컨트롤 사이 기본 이격(px)
local OPTION_LABEL_GAP = 40                                 -- 라벨과 첫 컨트롤 사이 이격(px)
local CHECK_ON  = "[*]"                                     -- 체크 켜짐
local CHECK_OFF = "[ ]"                                     -- 체크 꺼짐

-- General/Input/Graphic 탭의 상수·상태·로직은 모두 아래쪽 'General'/'Input'/'Graphic' 테이블(섹션)에 통합돼 있다.
-- 탭을 추가·수정하려면 그 테이블 하나만 보면 된다.

-- OPTION 초기값(RESET용). Lua로 등록 → C#에 저장, RESET 시 이 값으로 복구.
local OPTION_DEFAULTS = {
    { "General", "ShowFPS", false },
    { "General", "UIScale", 1 },
    { "General", "aimLength", 10 },
    { "General", "aimGap", 3 },
    { "General", "aimThick", 1 },
    { "General", "StaticAim", false },
    { "General", "aimColorR", 1 },
    { "General", "aimColorG", 0 },
    { "General", "aimColorB", 0 },
    { "General", "aimColorA", 1 },
    { "General", "playerName", "xopsPlayer" },
    { "Input", "sensitivity", 0.1 },
    { "Input", "invertY", false },
    { "Graphic", "fullscreen", true },
    { "Graphic", "resolution", 0 },
    { "Graphic", "vsync", false },
    { "Graphic", "limitFrame", true },
    { "Graphic", "frameLimit", 60 },
    { "Graphic", "brightness", 1.0 },
    { "Graphic", "gamma", 1.0 },
    { "Graphic", "fov", 65 },
    { "Sound", "MasterVolume", 1.0 },
}

-- ----- OPTION SAVE / RESET 버튼 (우측 하단, < BACK >과 같은 이격/기준만 BottomRight) -----
-- 글자 폭에 맞춘 두 버튼을 가로로 < SAVE >< RESET >. 배경은 두 버튼 합친 폭. SAVE=저장, RESET=초기화(추후).
local SAVE_ITEM  = "< SAVE >"
local RESET_ITEM = "< RESET >"
local saveResetBgHandle = nil
local saveSlot = nil
local resetSlot = nil

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
-- x=앵커 기준 수평 오프셋(생략 0, 한 줄에 여러 칸 배치할 때 사용). baseX/baseY로 눌림 원위치 복원.
local function makeTextPair(parent, y, text, color, fontW, fontH, hitW, hitH, pivot, align, x)
    pivot = pivot or "TopLeft"
    align = align or "TopLeft"
    x = x or 0
    local s = ITEM_SHADOW
    local shadow = parent:CreateChildText(pivot, align, text,
        x + 1, y - 1, fontW, fontH, 0, s.r, s.g, s.b, 1)
    shadow:SetSize(hitW, hitH)
    local main = parent:CreateChildText(pivot, align, text,
        x, y, fontW, fontH, 0, color.r, color.g, color.b, 1)
    main:SetSize(hitW, hitH)
    return { shadow = shadow, main = main, baseY = y, baseX = x }
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
    local bx = slot.baseX or 0
    if pressed then
        slot.main:SetPosition(bx + 1, slot.baseY - 1)
    else
        slot.main:SetPosition(bx, slot.baseY)
    end
end

-- ----- 누름 캡처 -----
-- 마우스를 누른 순간 커서 아래 있던 요소를 기억한다. 뗄 때(클릭)나 누른 채 유지(홀드)에는 "그 요소"만 반응한다.
-- A에서 누르고 B로 끌고 가서 떼도 B는 실행되지 않는다. (스크롤바를 끌다 미션 목록으로 삐져나와 떼면 미션이 로드되던 문제)
-- 빈 공간에서 누르면 캡처가 없어(nil) 어떤 요소도 반응하지 않는다.
local pressCapture = nil

-- 누른 순간 이 요소가 hover였으면 캡처한다. 반환: 이 요소가 지금 누름을 소유하는지.
-- id: 요소를 구분하는 고유 테이블(슬롯/핸들) / hovered: 현재 호버 / pressed: 이번 프레임 눌림
local function pressOwner(id, hovered, pressed)
    if pressed and hovered then
        pressCapture = id
    end
    return pressCapture == id
end

-- ===== 공용 옵션 위젯(모더가 재사용) =====

-- 체크 슬롯 텍스트를 on/off 표시로 갱신([*]/[ ]).
local function setCheckText(slot, on)
    local txt = on and CHECK_ON or CHECK_OFF
    slot.shadow:SetText(txt)
    slot.main:SetText(txt)
end

-- 체크박스 행 생성: 배경 패널 + 라벨 + 체크 슬롯(라벨→체크 이격 40px). 반환: bg 핸들, 체크 슬롯.
local function makeCheckbox(x, y, label)
    local labelW = #label * SWITCH_FONT_W
    local checkW = #CHECK_OFF * SWITCH_FONT_W
    local bg = XOPS.UI:CreateImage(MENU_LAYER, SCALING, "TopLeft", "",
        x, y, labelW + OPTION_LABEL_GAP + checkW, MENU_ROW_H, 0, 0, 0, 0.5)
    makeTextPair(bg, 0, label, BTN_NORMAL, SWITCH_FONT_W, SWITCH_FONT_H, labelW, MENU_ROW_H, "TopLeft", "MiddleCenter", 0)
    local check = makeTextPair(bg, 0, CHECK_OFF, BTN_NORMAL, SWITCH_FONT_W, SWITCH_FONT_H, checkW, MENU_ROW_H, "TopLeft", "MiddleCenter", labelW + OPTION_LABEL_GAP)
    return bg, check
end

-- 체크박스 상호작용: 색+눌림 갱신 후, 이번 프레임 클릭됐으면 true(토글은 호출 측이 수행).
-- 누름 캡처 적용 — 이 체크박스에서 눌러서 이 체크박스에서 뗐을 때만 true.
local function checkboxHit(slot, pressed, clicked, held)
    local h = slot.main:IsHovered()
    local own = pressOwner(slot, h, pressed)
    applyButtonColor(slot, false, h)
    setPressed(slot, h and held and own)
    return clicked and h and own
end

-- "라벨 << [값] >>" 셀렉터 행 생성. valueW=값칸 폭, labelGap=라벨~화살표 이격(생략 시 기본 40).
-- 반환: bg, prev, value, next 슬롯.
local function makeSelector(x, y, label, valueW, labelGap)
    labelGap = labelGap or OPTION_LABEL_GAP
    local labelW = #label * SWITCH_FONT_W
    local arrowW = #PAGE_PREV * SWITCH_FONT_W
    local bg = XOPS.UI:CreateImage(MENU_LAYER, SCALING, "TopLeft", "",
        x, y, labelW + labelGap + arrowW + OPTION_GAP + valueW + OPTION_GAP + arrowW, MENU_ROW_H, 0, 0, 0, 0.5)
    makeTextPair(bg, 0, label, BTN_NORMAL, SWITCH_FONT_W, SWITCH_FONT_H, labelW, MENU_ROW_H, "TopLeft", "MiddleCenter", 0)
    local cx = labelW + labelGap
    local prev = makeTextPair(bg, 0, PAGE_PREV, BTN_NORMAL, SWITCH_FONT_W, SWITCH_FONT_H, arrowW, MENU_ROW_H, "TopLeft", "MiddleCenter", cx)
    local value = makeTextPair(bg, 0, "", BTN_NORMAL, SWITCH_FONT_W, SWITCH_FONT_H, valueW, MENU_ROW_H, "TopLeft", "MiddleCenter", cx + arrowW + OPTION_GAP)
    local nxt = makeTextPair(bg, 0, PAGE_NEXT, BTN_NORMAL, SWITCH_FONT_W, SWITCH_FONT_H, arrowW, MENU_ROW_H, "TopLeft", "MiddleCenter", cx + arrowW + OPTION_GAP + valueW + OPTION_GAP)
    return bg, prev, value, nxt
end

-- 셀렉터/스테퍼 한 개의 전체 폭(px). 두 개를 한 줄에 배치할 때 x 오프셋 계산용.
local function selectorWidth(label, valueChars, labelGap)
    labelGap = labelGap or OPTION_LABEL_GAP
    local arrowW = #PAGE_PREV * SWITCH_FONT_W
    return #label * SWITCH_FONT_W + labelGap + arrowW + OPTION_GAP + valueChars * SWITCH_FONT_W + OPTION_GAP + arrowW
end

-- 체크박스 한 개의 전체 폭(px).
local function checkboxWidth(label)
    return #label * SWITCH_FONT_W + OPTION_LABEL_GAP + #CHECK_OFF * SWITCH_FONT_W
end

-- << >> 화살표 쌍: 색+눌림 갱신 후, 이번 프레임 클릭 방향 반환(-1 prev / 0 없음 / 1 next).
-- canPrev/canNext: 각 방향 활성 여부(경계에서 false면 회색·클릭 무시). 누름 캡처 적용(그 화살표에서 눌러 뗐을 때만).
local function arrowDir(prevSlot, nextSlot, canPrev, canNext, pressed, clicked, held)
    local ph = prevSlot.main:IsHovered()
    local nh = nextSlot.main:IsHovered()
    local pOwn = pressOwner(prevSlot, ph, pressed)
    local nOwn = pressOwner(nextSlot, nh, pressed)
    applyButtonColor(prevSlot, not canPrev, ph)
    applyButtonColor(nextSlot, not canNext, nh)
    setPressed(prevSlot, ph and held and canPrev and pOwn)
    setPressed(nextSlot, nh and held and canNext and nOwn)
    if clicked then
        if ph and canPrev and pOwn then return -1 end
        if nh and canNext and nOwn then return 1 end
    end
    return 0
end

-- 숫자 스테퍼: "라벨 << [값] >>" + 꾹 누름 반복. 값은 config(section/key)에서 직접 읽고 쓴다.
-- 화살표를 눌러 즉시 1스텝, 계속 누르고 있으면 DELAY 뒤부터 INTERVAL마다 빠르게 증감(범위는 config min/max로 클램프).
-- 인자: x,y=위치 / valueChars=값칸 글자수 / step=증감량 / fmt=표시 포맷("%.2f") / isInt=정수 여부 /
--       labelGap=라벨 이격(생략 시 기본) / onChange=값 변경 시 호출할 콜백(생략 가능, 예: applyUIScale).
-- 반환: 스테퍼 객체 { bg, prev, value, next, refresh(), update(dt, held) }.
local STEP_HOLD_DELAY = 0.5      -- 꾹 누르고 이 시간(초) 뒤부터 반복 증감
local STEP_HOLD_INTERVAL = 0.03  -- 반복 구간에서의 스텝 간격(초)

local function makeStepper(x, y, label, valueChars, section, key, step, fmt, isInt, labelGap, onChange)
    local st = { holdDir = 0, holdTimer = 0 }
    st.bg, st.prev, st.value, st.next = makeSelector(x, y, label, valueChars * SWITCH_FONT_W, labelGap)

    -- 현재 config 값(int/float).
    function st.get()
        if isInt then return XOPS.Config:GetInt(section, key) end
        return XOPS.Config:GetFloat(section, key)
    end

    -- 값 표시 갱신([포맷]).
    function st.refresh()
        local txt = "[" .. string.format(fmt, st.get()) .. "]"
        st.value.shadow:SetText(txt)
        st.value.main:SetText(txt)
    end

    -- dir(±1) 방향으로 한 스텝 증감 + 표시 갱신(범위는 SetInt/SetFloat가 클램프).
    function st.stepBy(dir)
        if isInt then
            XOPS.Config:SetInt(section, key, st.get() + dir * step)
        else
            XOPS.Config:SetFloat(section, key, st.get() + dir * step)
        end
        st.refresh()
        if onChange then onChange() end
    end

    -- 상호작용(색+눌림 + 꾹 누름 반복). dt=프레임 델타초 / pressed=이번 프레임 눌림 / held=좌클릭 유지.
    -- min/max는 매번 config에서 읽는다 — UIScale처럼 다른 설정(해상도)에 따라 범위가 바뀌는 값이 있기 때문.
    -- 누름 캡처 적용 — 그 화살표에서 눌러야만 홀드 반복이 동작한다(다른 곳에서 눌러 끌고 오면 무반응).
    function st.update(dt, pressed, held)
        local v = st.get()
        local min = XOPS.Config:GetMin(section, key)
        local max = XOPS.Config:GetMax(section, key)
        local atMin = v <= min
        local atMax = v >= max
        local ph = st.prev.main:IsHovered()
        local nh = st.next.main:IsHovered()
        local pOwn = pressOwner(st.prev, ph, pressed)
        local nOwn = pressOwner(st.next, nh, pressed)
        applyButtonColor(st.prev, atMin, ph)
        applyButtonColor(st.next, atMax, nh)
        setPressed(st.prev, ph and held and pOwn and not atMin)
        setPressed(st.next, nh and held and nOwn and not atMax)
        local dir = 0
        if held and ph and pOwn and not atMin then dir = -1
        elseif held and nh and nOwn and not atMax then dir = 1 end
        if dir ~= st.holdDir then
            st.holdDir = dir
            st.holdTimer = 0
            if dir ~= 0 then st.stepBy(dir) end
        elseif dir ~= 0 then
            st.holdTimer = st.holdTimer + dt
            while st.holdTimer >= STEP_HOLD_DELAY do
                st.stepBy(dir)
                st.holdTimer = st.holdTimer - STEP_HOLD_INTERVAL
            end
        end
    end

    return st
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
-- 진입 후 CLICK_ALLOW_TIME 초가 지난 뒤의 입력만 인정한다. 스크롤바 드래그 시작 등 "눌리는 순간"이 필요할 때 쓴다.
-- t: 경과초 / 반환: 이번 프레임에 눌렸으면 true
local function firePressed(t)
    return t >= CLICK_ALLOW_TIME and XOPS.Input:WasPressed("fire")
end

-- 버튼 클릭 판정 게이트. 버튼은 "누를 때"가 아니라 "뗄 때" 동작한다(누른 채 벗어나면 취소 가능).
-- t: 경과초 / 반환: 이번 프레임에 뗐으면 true
local function fireReleased(t)
    return t >= CLICK_ALLOW_TIME and XOPS.Input:WasReleased("fire")
end

-- ESC 소비 게이트(오프닝 스킵 ESC 누수 방지). t: 경과초 / 반환: 이번 프레임에 ESC 눌렸으면 true
local function escPressed(t)
    return t >= CLICK_ALLOW_TIME and XOPS.Input:WasPressed("escape")
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

-- 섹션 선택 바의 3칸을 현재 sectionPage에 맞춰 채운다. 데이터보다 많은 칸은 비운다.
local function refreshSectionBar()
    local count = #sectionNames
    for i = 0, SECTION_PER_PAGE - 1 do
        local slot = sectionSlots[i]
        local si = sectionPage * SECTION_PER_PAGE + i
        if si < count then
            local name = sectionNames[si + 1]
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

-- ===== General 탭 섹션 =====
-- 모더 참고: 이 테이블 하나가 General 탭 전체(상수/상태/생성/갱신/입력)를 담는다.
-- ShowFPS 체크박스 + UIScale 라디오 + Aim(파라미터/색상) + PlayerName 입력.
local General = {
    -- Aim 파라미터 행 구성(Length 단독, 나머지 2개씩). label=표시, key=config 이름.
    AIM_ROWS = {
        { { label = "Length", key = "aimLength" } },
        { { label = "Gap", key = "aimGap" }, { label = "Thick", key = "aimThick" } },
    },
    -- Aim 색상 프리셋(<< >>로 순환, 알파 1 고정, 기본 Red). name=표시, r/g/b=0~1.
    COLOR_PRESETS = {
        { name = "White",   r = 1, g = 1, b = 1 },
        { name = "Black",   r = 0, g = 0, b = 0 },
        { name = "Red",     r = 1, g = 0, b = 0 },
        { name = "Green",   r = 0, g = 1, b = 0 },
        { name = "Blue",    r = 0, g = 0, b = 1 },
        { name = "Cyan",    r = 0, g = 1, b = 1 },
        { name = "Magenta", r = 1, g = 0, b = 1 },
        { name = "Yellow",  r = 1, g = 1, b = 0 },
    },
    PLAYERNAME_MAX = 16,               -- PlayerName 최대 글자 수
    showFpsBg = nil, showFpsCheck = nil,
    uiScaleStepper = nil,              -- UIScale << [값] >> 스테퍼(0.1씩)
    aimPanels = {},                    -- 헤더 + 각 행 배경(가시성 토글)
    aimUnits = {},                     -- { key, min, max, step, isFloat, prevSlot, valueSlot, nextSlot }
    staticCheck = nil,                 -- Static(정적 조준) 체크박스 — Length 오른쪽
    colorPrev = nil, colorValue = nil, colorNext = nil,
    playerNameBg = nil, playerNameBox = nil, playerNameText = nil,
    playerNameFocused = false,
}

-- ShowFPS/UIScale/Aim/PlayerName 행을 섹션 바 아래로 세로 나열해 생성한다.
function General.create()
    local SHOWFPS_LABEL = "ShowFPS"
    local UISCALE_LABEL = "UIScale"
    local AIM_LABEL = "Aim"
    local AIM_VALUE_CHARS = 5
    local AIM_UNIT_GAP = 10
    local COLOR_LABEL = "Color"
    local COLOR_VALUE_CHARS = 7
    local PLAYERNAME_LABEL = "PlayerName"
    local PLAYERNAME_BOX = { r = 0.5, g = 0.5, b = 0.5, a = 0.5 }

    -- ShowFPS 행: 라벨 + 체크박스.
    General.showFpsBg, General.showFpsCheck = makeCheckbox(TITLE.x, OPTION_ROW_TOP, SHOWFPS_LABEL)

    -- UIScale 행: << [값] >> 스테퍼(0.1씩). 변경 시 applyUIScale로 즉시 라이브 반영.
    General.uiScaleStepper = makeStepper(TITLE.x, OPTION_ROW_TOP - OPTION_ROW_PITCH, UISCALE_LABEL, 5,
        "General", "UIScale", 0.1, "%.1f", false, nil, applyUIScale)

    -- Aim: 헤더 라벨 + 파라미터 행들. 각 유닛 "라벨 << [값] >>".
    local aimArrowW = #PAGE_PREV * SWITCH_FONT_W
    local aimValueW = AIM_VALUE_CHARS * SWITCH_FONT_W
    local aimRowY = OPTION_ROW_TOP - 3 * OPTION_ROW_PITCH   -- ShowFPS(0)/UIScale(1)/공백(2) 다음, 4번째 줄부터
    local aimHeaderW = #AIM_LABEL * SWITCH_FONT_W
    local aimHeader = XOPS.UI:CreateImage(MENU_LAYER, SCALING, "TopLeft", "",
        TITLE.x, aimRowY, aimHeaderW, MENU_ROW_H, 0, 0, 0, 0.5)
    makeTextPair(aimHeader, 0, AIM_LABEL, BTN_NORMAL,
        SWITCH_FONT_W, SWITCH_FONT_H, aimHeaderW, MENU_ROW_H, "TopLeft", "MiddleCenter", 0)
    General.aimPanels[#General.aimPanels + 1] = aimHeader
    for r = 1, #General.AIM_ROWS do
        local row = General.AIM_ROWS[r]
        -- 행 폭 선계산(유닛 = 라벨 + 40 + << [값] >>).
        local rowW = 0
        for i = 1, #row do
            local uw = #row[i].label * SWITCH_FONT_W + OPTION_LABEL_GAP + aimArrowW + OPTION_GAP + aimValueW + OPTION_GAP + aimArrowW
            row[i].uw = uw
            rowW = rowW + uw + (i > 1 and AIM_UNIT_GAP or 0)
        end
        aimRowY = aimRowY - OPTION_ROW_PITCH
        local bg = XOPS.UI:CreateImage(MENU_LAYER, SCALING, "TopLeft", "",
            TITLE.x, aimRowY, rowW, MENU_ROW_H, 0, 0, 0, 0.5)
        General.aimPanels[#General.aimPanels + 1] = bg
        local x = 0
        for i = 1, #row do
            local lw = #row[i].label * SWITCH_FONT_W
            makeTextPair(bg, 0, row[i].label, BTN_NORMAL,
                SWITCH_FONT_W, SWITCH_FONT_H, lw, MENU_ROW_H, "TopLeft", "MiddleCenter", x)
            local cx = x + lw + OPTION_LABEL_GAP
            local prev = makeTextPair(bg, 0, PAGE_PREV, BTN_NORMAL,
                SWITCH_FONT_W, SWITCH_FONT_H, aimArrowW, MENU_ROW_H, "TopLeft", "MiddleCenter", cx)
            local val = makeTextPair(bg, 0, "", BTN_NORMAL,
                SWITCH_FONT_W, SWITCH_FONT_H, aimValueW, MENU_ROW_H, "TopLeft", "MiddleCenter", cx + aimArrowW + OPTION_GAP)
            local nxt = makeTextPair(bg, 0, PAGE_NEXT, BTN_NORMAL,
                SWITCH_FONT_W, SWITCH_FONT_H, aimArrowW, MENU_ROW_H, "TopLeft", "MiddleCenter", cx + aimArrowW + OPTION_GAP + aimValueW + OPTION_GAP)
            local isFloat = XOPS.Config:GetSettingType("General", row[i].key) == "float"
            General.aimUnits[#General.aimUnits + 1] = { key = row[i].key, prevSlot = prev, valueSlot = val, nextSlot = nxt,
                min = XOPS.Config:GetMin("General", row[i].key), max = XOPS.Config:GetMax("General", row[i].key),
                isFloat = isFloat, step = isFloat and 0.1 or 1 }
            x = x + row[i].uw + AIM_UNIT_GAP
        end

        if r == 1 then
            -- Length 오른쪽에 Static 체크박스(켜짐=정적 조준 / 꺼짐=반동으로 벌어짐)
            local stBg
            stBg, General.staticCheck = makeCheckbox(TITLE.x + rowW + AIM_UNIT_GAP, aimRowY, "Static")
            General.aimPanels[#General.aimPanels + 1] = stBg
        end
    end

    -- Aim Color 프리셋 행: "Color << [name] >>".
    aimRowY = aimRowY - OPTION_ROW_PITCH
    local colorBg
    colorBg, General.colorPrev, General.colorValue, General.colorNext =
        makeSelector(TITLE.x, aimRowY, COLOR_LABEL, COLOR_VALUE_CHARS * SWITCH_FONT_W)
    General.aimPanels[#General.aimPanels + 1] = colorBg

    -- PlayerName 행: 라벨 + 회색 입력 박스(16글자). Lua가 타이핑을 받아 편집.
    local pnLabelW = #PLAYERNAME_LABEL * SWITCH_FONT_W
    local pnBoxW = General.PLAYERNAME_MAX * SWITCH_FONT_W
    aimRowY = aimRowY - 2 * OPTION_ROW_PITCH   -- Aim 다음, 한 칸 띄우고
    General.playerNameBg = XOPS.UI:CreateImage(MENU_LAYER, SCALING, "TopLeft", "",
        TITLE.x, aimRowY, pnLabelW + OPTION_LABEL_GAP + pnBoxW, MENU_ROW_H, 0, 0, 0, 0.5)
    makeTextPair(General.playerNameBg, 0, PLAYERNAME_LABEL, BTN_NORMAL,
        SWITCH_FONT_W, SWITCH_FONT_H, pnLabelW, MENU_ROW_H, "TopLeft", "MiddleCenter", 0)
    General.playerNameBox = General.playerNameBg:CreateChildImage("TopLeft", "",
        pnLabelW + OPTION_LABEL_GAP, 0, pnBoxW, MENU_ROW_H, PLAYERNAME_BOX.r, PLAYERNAME_BOX.g, PLAYERNAME_BOX.b, PLAYERNAME_BOX.a)
    General.playerNameText = makeTextPair(General.playerNameBg, 0, "", BTN_NORMAL,
        SWITCH_FONT_W, SWITCH_FONT_H, pnBoxW, MENU_ROW_H, "TopLeft", "MiddleLeft", pnLabelW + OPTION_LABEL_GAP)

    General.refreshAll()
    General.setVisible(false)
end

-- ShowFPS 체크박스 문자열을 현재 설정값에 맞춘다([*]=켜짐 / [ ]=꺼짐).
function General.refreshShowFps()
    setCheckText(General.showFpsCheck, XOPS.Config:GetBool("General", "ShowFPS"))
end

-- UIScale 스테퍼 값 표시 갱신.
function General.refreshUIScale()
    General.uiScaleStepper.refresh()
end

-- Aim 각 파라미터 값 표시 갱신. int는 정수, 컬러(float)는 소수1자리. [ ]로 감쌈.
function General.refreshAim()
    for i = 1, #General.aimUnits do
        local u = General.aimUnits[i]
        local v = XOPS.Config:GetFloat("General", u.key)
        local txt = "[" .. string.format(u.isFloat and "%.1f" or "%.0f", v) .. "]"
        u.valueSlot.shadow:SetText(txt)
        u.valueSlot.main:SetText(txt)
    end
end

-- 현재 aim 색(RGB)이 일치하는 프리셋 인덱스. 없으면 0(Custom).
function General.colorIndex()
    local r = XOPS.Config:GetFloat("General", "aimColorR")
    local g = XOPS.Config:GetFloat("General", "aimColorG")
    local b = XOPS.Config:GetFloat("General", "aimColorB")
    for i = 1, #General.COLOR_PRESETS do
        local p = General.COLOR_PRESETS[i]
        if p.r == r and p.g == g and p.b == b then
            return i
        end
    end
    return 0
end

-- Aim 색상 선택기 표시 갱신. 프리셋과 맞으면 그 이름, 아니면 Custom. 텍스트 색은 현재 aim 색으로.
function General.refreshColor()
    local idx = General.colorIndex()
    local name = (idx > 0) and General.COLOR_PRESETS[idx].name or "Custom"
    General.colorValue.shadow:SetText(name)
    General.colorValue.main:SetText(name)
    General.colorValue.main:SetColor(
        XOPS.Config:GetFloat("General", "aimColorR"),
        XOPS.Config:GetFloat("General", "aimColorG"),
        XOPS.Config:GetFloat("General", "aimColorB"), 1)
end

-- PlayerName 입력 박스 표시 갱신(포커스 중이고 꽉 차지 않았으면 끝에 커서 _).
function General.refreshPlayerName()
    local txt = XOPS.Config:GetString("General", "playerName")
    if General.playerNameFocused and #txt < General.PLAYERNAME_MAX then
        txt = txt .. "_"
    end
    General.playerNameText.shadow:SetText(txt)
    General.playerNameText.main:SetText(txt)
end

-- 모든 General 표시 갱신(setVisible/RESET에서 호출).
function General.refreshAll()
    General.refreshShowFps()
    General.refreshUIScale()
    General.refreshAim()
    General.refreshColor()
    General.refreshPlayerName()
    setCheckText(General.staticCheck, XOPS.Config:GetBool("General", "StaticAim"))
end

-- 탭 표시/숨김. 보일 때 값도 갱신, 숨길 때 입력 포커스 해제.
-- visible: 표시 여부
function General.setVisible(visible)
    General.showFpsBg:SetActive(visible)
    General.uiScaleStepper.bg:SetActive(visible)
    for i = 1, #General.aimPanels do General.aimPanels[i]:SetActive(visible) end
    General.playerNameBg:SetActive(visible)
    if visible then
        General.refreshAll()
    else
        General.playerNameFocused = false
    end
end

-- 상호작용(ShowFPS/Static 토글 / UIScale 스테퍼 / Aim ± / Color 순환 / PlayerName 타이핑). 저장/적용은 SAVE에서.
-- (숨겨진 요소도 IsHovered는 기하학적으로 true가 될 수 있어, 표시 조건과 동일하게 호출을 막아야 오작동이 없다.)
-- dt: 프레임 델타초 / pressed: 이번 프레임 눌림 / held: 좌클릭 유지 / clicked: 이번 프레임 클릭(뗌)
function General.update(dt, pressed, held, clicked)
    -- ShowFPS 체크박스: 클릭 시 설정 토글(저장은 SAVE에서).
    if checkboxHit(General.showFpsCheck, pressed, clicked, held) then
        XOPS.Config:SetBool("General", "ShowFPS", not XOPS.Config:GetBool("General", "ShowFPS"))
        General.refreshShowFps()
    end

    -- Static 체크박스: 정적/동적 조준 토글.
    if checkboxHit(General.staticCheck, pressed, clicked, held) then
        XOPS.Config:SetBool("General", "StaticAim", not XOPS.Config:GetBool("General", "StaticAim"))
        setCheckText(General.staticCheck, XOPS.Config:GetBool("General", "StaticAim"))
    end

    -- UIScale 스테퍼(0.1씩, 꾹 누름 반복). 변경 시 onChange=applyUIScale로 즉시 반영.
    General.uiScaleStepper.update(dt, pressed, held)

    -- Aim 파라미터: << −(step) / >> +(step) (범위 클램프, 경계 비활성).
    for i = 1, #General.aimUnits do
        local u = General.aimUnits[i]
        local v = XOPS.Config:GetFloat("General", u.key)
        local d = arrowDir(u.prevSlot, u.nextSlot, v > u.min, v < u.max, pressed, clicked, held)
        if d ~= 0 then
            XOPS.Config:SetFloat("General", u.key, v + d * u.step)
            General.refreshAim()
        end
    end

    -- Aim Color 프리셋: << 이전 / >> 다음 (양 끝 비활성). 선택 시 RGB 세팅, 알파 1 고정.
    local ci = General.colorIndex()
    local cd = arrowDir(General.colorPrev, General.colorNext, ci > 1, ci < #General.COLOR_PRESETS, pressed, clicked, held)
    if cd ~= 0 then
        local p = General.COLOR_PRESETS[(ci == 0) and 1 or (ci + cd)]   -- custom(0)에서 >>면 첫 프리셋
        XOPS.Config:SetFloat("General", "aimColorR", p.r)
        XOPS.Config:SetFloat("General", "aimColorG", p.g)
        XOPS.Config:SetFloat("General", "aimColorB", p.b)
        XOPS.Config:SetFloat("General", "aimColorA", 1)
        General.refreshColor()
    end

    -- PlayerName 입력 박스: 클릭으로 포커스(박스 밖 클릭=해제), 포커스 중 타이핑 편집(16자, ASCII 32~126).
    -- 누름 캡처 — 박스에서 눌러서 박스에서 뗐을 때만 포커스가 잡힌다.
    local pnHover = General.playerNameBox:IsHovered()
    local pnOwn = pressOwner(General.playerNameBox, pnHover, pressed)
    if clicked then
        General.playerNameFocused = pnHover and pnOwn
        General.refreshPlayerName()
    end
    if General.playerNameFocused then
        local name = XOPS.Config:GetString("General", "playerName")
        local changed = false
        if XOPS.Input:WasBackspacePressed() and #name > 0 then
            name = name:sub(1, #name - 1)
            changed = true
        end
        local typed = XOPS.Input:GetTypedText()
        for i = 1, #typed do
            local b = string.byte(typed, i)
            if b >= 32 and b <= 126 and #name < General.PLAYERNAME_MAX then
                name = name .. string.char(b)
                changed = true
            end
        end
        if changed then
            XOPS.Config:SetString("General", "playerName", name)
            General.refreshPlayerName()
        end
    end
end

-- ===== Input 탭 섹션 =====
-- 모더 참고: 이 테이블 하나가 Input 탭 전체(상수/상태/생성/갱신/입력)를 담는다.
-- 마우스 감도(꾹 누름 반복) + Invert Mouse 체크 + 키 바인딩(클릭→리스닝→키 캡처).
local Input = {
    SENS_STEP = 0.01,               -- 감도 한 스텝 증감량
    SENS_HOLD_DELAY = 0.5,          -- 꾹 누르고 이 시간(초) 뒤부터 값이 반복 증감
    SENS_HOLD_INTERVAL = 0.03,      -- 반복 구간에서의 스텝 간격(초)
    -- 리바인드 대상 액션(순수 버튼). 2개씩 배치. escape/방향/이동 등 합성 액션은 제외(Esc/F1~F12 키도 캡처 제외).
    BIND_ACTIONS = { "jump", "walk", "drop", "fire", "zoom", "previous",
                     "next", "reload", "first", "second", "interact" },
    -- 바인딩 경로 → 짧은 표시. "<Keyboard>/w"→"W", "<Mouse>/leftButton"→"LMB". 없으면 마지막 토큰 대문자.
    BIND_ABBREV = {
        leftButton = "LMB", rightButton = "RMB", middleButton = "MMB",
        leftShift = "LShift", rightShift = "RShift", leftCtrl = "LCtrl", rightCtrl = "RCtrl",
        leftAlt = "LAlt", rightAlt = "RAlt", space = "Space", escape = "Esc", enter = "Enter",
        tab = "Tab", upArrow = "Up", downArrow = "Down", leftArrow = "Left", rightArrow = "Right",
    },
    sensBg = nil, sensPrev = nil, sensValue = nil, sensNext = nil,
    sensMin = 0, sensMax = 1,
    sensHoldDir = 0,                -- 현재 꾹 누르고 있는 방향(-1 감소 / 0 없음 / 1 증가)
    sensHoldTimer = 0,              -- 꾹 누른 누적 시간(초)
    invertBg = nil, invertCheck = nil,
    bindSlots = {},                 -- action → 키 텍스트 슬롯
    bindListening = nil,            -- 리바인드 대기 중인 action(없으면 nil)
    panels = {},                    -- 가시성 토글용 패널
}

-- 감도(1행) + Invert(2행) + 키 바인딩(3행부터 2개씩)을 생성한다.
function Input.create()
    local SENS_LABEL = "Sensitivity"
    local SENS_VALUE_CHARS = 6
    local INVERT_LABEL = "Invert Mouse"
    local BIND_PER_ROW = 2
    local BIND_LABEL_CHARS = 8
    local BIND_KEY_CHARS = 7
    local BIND_LABEL_GAP = 15
    local BIND_UNIT_GAP = 30

    -- 1행: 마우스 감도 셀렉터.
    Input.sensMin = XOPS.Config:GetMin("Input", "sensitivity")
    Input.sensMax = XOPS.Config:GetMax("Input", "sensitivity")
    Input.sensBg, Input.sensPrev, Input.sensValue, Input.sensNext =
        makeSelector(TITLE.x, OPTION_ROW_TOP, SENS_LABEL, SENS_VALUE_CHARS * SWITCH_FONT_W)
    Input.panels[#Input.panels + 1] = Input.sensBg

    -- 2행: Invert Mouse 체크박스(자체 행).
    Input.invertBg, Input.invertCheck = makeCheckbox(TITLE.x, OPTION_ROW_TOP - OPTION_ROW_PITCH, INVERT_LABEL)
    Input.panels[#Input.panels + 1] = Input.invertBg

    -- 3행부터: 키 바인딩(2개씩). 각 유닛 = 액션 라벨 + [키].
    local bindLabelW = BIND_LABEL_CHARS * SWITCH_FONT_W
    local bindKeyW = BIND_KEY_CHARS * SWITCH_FONT_W
    local bindUnitW = bindLabelW + BIND_LABEL_GAP + bindKeyW
    local bindRowW = BIND_PER_ROW * bindUnitW + (BIND_PER_ROW - 1) * BIND_UNIT_GAP
    local bindRowY = OPTION_ROW_TOP - OPTION_ROW_PITCH   -- Invert(2번째 줄) 다음 → 바인딩은 3번째 줄부터
    local bindRowBg = nil
    for i = 1, #Input.BIND_ACTIONS do
        local col = (i - 1) % BIND_PER_ROW
        if col == 0 then
            bindRowY = bindRowY - OPTION_ROW_PITCH
            bindRowBg = XOPS.UI:CreateImage(MENU_LAYER, SCALING, "TopLeft", "",
                TITLE.x, bindRowY, bindRowW, MENU_ROW_H, 0, 0, 0, 0.5)
            Input.panels[#Input.panels + 1] = bindRowBg
        end
        local action = Input.BIND_ACTIONS[i]
        local ux = col * (bindUnitW + BIND_UNIT_GAP)
        makeTextPair(bindRowBg, 0, action, BTN_NORMAL,
            SWITCH_FONT_W, SWITCH_FONT_H, bindLabelW, MENU_ROW_H, "TopLeft", "MiddleLeft", ux)
        Input.bindSlots[action] = makeTextPair(bindRowBg, 0, "", BTN_NORMAL,
            SWITCH_FONT_W, SWITCH_FONT_H, bindKeyW, MENU_ROW_H, "TopLeft", "MiddleCenter", ux + bindLabelW + BIND_LABEL_GAP)
    end

    Input.refreshAll()
    Input.setVisible(false)
end

-- Input 감도 값 표시 갱신([0.10]).
function Input.refreshSensitivity()
    local txt = "[" .. string.format("%.2f", XOPS.Config:GetFloat("Input", "sensitivity")) .. "]"
    Input.sensValue.shadow:SetText(txt)
    Input.sensValue.main:SetText(txt)
end

-- 감도를 dir(±1) 방향으로 한 스텝 증감(범위는 SetFloat가 클램프) + 표시 갱신.
function Input.stepSensitivity(dir)
    local v = XOPS.Config:GetFloat("Input", "sensitivity")
    XOPS.Config:SetFloat("Input", "sensitivity", v + dir * Input.SENS_STEP)
    Input.refreshSensitivity()
end

-- 바인딩 경로를 짧은 표시로 줄인다.
function Input.keyAbbrev(path)
    if not path or path == "" then return "?" end
    local key = path:match("/([^/]+)$") or path
    return Input.BIND_ABBREV[key] or key:upper()
end

-- 한 바인딩 슬롯 텍스트 갱신(리스닝 중이면 [...], 아니면 현재 키).
function Input.refreshBind(action)
    local slot = Input.bindSlots[action]
    local txt
    if Input.bindListening == action then
        txt = "[...]"
    else
        local paths = XOPS.Input:GetBindings(action)
        txt = "[" .. Input.keyAbbrev(paths[1]) .. "]"
    end
    slot.shadow:SetText(txt)
    slot.main:SetText(txt)
end

-- Invert Mouse 체크박스 표시 갱신.
function Input.refreshInvert()
    setCheckText(Input.invertCheck, XOPS.Config:GetBool("Input", "invertY"))
end

-- 모든 Input 표시 갱신(setVisible/RESET에서 호출).
function Input.refreshAll()
    Input.refreshSensitivity()
    Input.refreshInvert()
    for i = 1, #Input.BIND_ACTIONS do Input.refreshBind(Input.BIND_ACTIONS[i]) end
end

-- 탭 표시/숨김. 보일 때 값 갱신, 숨길 때 꾹 누름/리스닝 상태 리셋.
-- visible: 표시 여부
function Input.setVisible(visible)
    for i = 1, #Input.panels do Input.panels[i]:SetActive(visible) end
    if visible then
        Input.refreshAll()
    else
        Input.sensHoldDir = 0
        Input.bindListening = nil
    end
end

-- 상호작용. 감도는 꾹 누름 반복(즉시 1스텝 → DELAY 후 INTERVAL마다). 바인딩은 클릭→리스닝→키 캡처.
-- dt: 프레임 델타초 / pressed: 이번 프레임 눌림 / held: 좌클릭 유지 / clicked: 이번 프레임 클릭(뗌)
function Input.update(dt, pressed, held, clicked)
    -- 마우스 감도(꾹 누름). 누름 캡처 — 그 화살표에서 눌러야만 반복이 돈다.
    local v = XOPS.Config:GetFloat("Input", "sensitivity")
    local atMin = v <= Input.sensMin
    local atMax = v >= Input.sensMax
    local ph = Input.sensPrev.main:IsHovered()
    local nh = Input.sensNext.main:IsHovered()
    local pOwn = pressOwner(Input.sensPrev, ph, pressed)
    local nOwn = pressOwner(Input.sensNext, nh, pressed)
    applyButtonColor(Input.sensPrev, atMin, ph)
    applyButtonColor(Input.sensNext, atMax, nh)
    setPressed(Input.sensPrev, ph and held and pOwn and not atMin)
    setPressed(Input.sensNext, nh and held and nOwn and not atMax)
    local dir = 0
    if held and ph and pOwn and not atMin then dir = -1
    elseif held and nh and nOwn and not atMax then dir = 1 end
    if dir ~= Input.sensHoldDir then
        Input.sensHoldDir = dir
        Input.sensHoldTimer = 0
        if dir ~= 0 then Input.stepSensitivity(dir) end
    elseif dir ~= 0 then
        Input.sensHoldTimer = Input.sensHoldTimer + dt
        while Input.sensHoldTimer >= Input.SENS_HOLD_DELAY do
            Input.stepSensitivity(dir)
            Input.sensHoldTimer = Input.sensHoldTimer - Input.SENS_HOLD_INTERVAL
        end
    end

    -- Invert Mouse 체크박스
    if checkboxHit(Input.invertCheck, pressed, clicked, held) then
        XOPS.Config:SetBool("Input", "invertY", not XOPS.Config:GetBool("Input", "invertY"))
        Input.refreshInvert()
    end

    -- 키 바인딩: 리스닝 중이면 눌린 키를 캡처해 리바인드.
    if Input.bindListening then
        local path = XOPS.Input:GetFirstPressedKeyPath()
        if path ~= "" then
            XOPS.Input:SetActionBinding(Input.bindListening, path)
            local done = Input.bindListening
            Input.bindListening = nil
            Input.refreshBind(done)
        end
    end
    -- 각 슬롯 색(리스닝=회색 / 호버=시안 / 평상=흰). 클릭 시 리스닝 시작.
    for i = 1, #Input.BIND_ACTIONS do
        local action = Input.BIND_ACTIONS[i]
        local slot = Input.bindSlots[action]
        local hov = slot.main:IsHovered()
        local own = pressOwner(slot, hov, pressed)
        if Input.bindListening == action then
            slot.main:SetColor(BTN_DISABLED.r, BTN_DISABLED.g, BTN_DISABLED.b, 1)
        else
            local c = hov and BTN_HOVER or BTN_NORMAL
            slot.main:SetColor(c.r, c.g, c.b, 1)
        end
        if clicked and not Input.bindListening and hov and own then
            Input.bindListening = action
            Input.refreshBind(action)
        end
    end
end

-- ===== Graphic 탭 섹션 =====
-- 모더 참고: 이 테이블 하나가 Graphic 탭 전체(상수/상태/생성/갱신/입력)를 담는다.
-- 줄 구성: Fullscreen 체크 / Resolution << >> / VSync·LimitFrame 체크 / FrameLimit / Brightness·Gamma / FOV.
--          값 항목은 화살표를 꾹 누르면 빠르게 증감. 적용은 SAVE에서. (near/far Clipping은 옵션 제외, config.json 직접 편집)
local Graphic = {
    RES_VALUE_CHARS = 11,           -- "[3840x2160]"
    panels = {},                    -- 가시성 토글용 패널
    fullscreenCheck = nil,
    resPrev = nil, resValue = nil, resNext = nil,
    resOptions = {},                -- 1-based: { index=저장인덱스, label="640x480" }
    vsyncCheck = nil,
    limitFrameCheck = nil,
    steppers = {},                  -- 값 스테퍼 목록(refresh/update 일괄 처리)
}

-- 모든 줄을 세로로 생성한다. rowTop=1행 top y, pitch=행 간격.
-- 한 줄에 2개 놓는 항목(VSync·LimitFrame / Bright·Gamma / Near·Far)은 두 번째를 첫 번째 폭만큼 오른쪽에 붙인다.
-- 값 스테퍼는 makeStepper가 config(section/key)에 직접 읽고 쓴다.
function Graphic.create(rowTop, pitch)
    -- 해상도 옵션 캐시(디스플레이 지원분만).
    for i = 0, XOPS.Config:GetResolutionOptionCount() - 1 do
        Graphic.resOptions[i + 1] = { index = XOPS.Config:GetResolutionOptionIndex(i), label = XOPS.Config:GetResolutionOptionLabel(i) }
    end

    -- 1행: Fullscreen 체크
    local fsBg
    fsBg, Graphic.fullscreenCheck = makeCheckbox(TITLE.x, rowTop, "Fullscreen")
    Graphic.panels[#Graphic.panels + 1] = fsBg

    -- 2행: Resolution << >>
    local resBg
    resBg, Graphic.resPrev, Graphic.resValue, Graphic.resNext =
        makeSelector(TITLE.x, rowTop - pitch, "Resolution", Graphic.RES_VALUE_CHARS * SWITCH_FONT_W)
    Graphic.panels[#Graphic.panels + 1] = resBg

    -- 3행: VSync + LimitFrame 체크(한 줄)
    local y3 = rowTop - 2 * pitch
    local vsBg
    vsBg, Graphic.vsyncCheck = makeCheckbox(TITLE.x, y3, "VSync")
    Graphic.panels[#Graphic.panels + 1] = vsBg
    local lfBg
    lfBg, Graphic.limitFrameCheck = makeCheckbox(TITLE.x + checkboxWidth("VSync") + OPTION_GAP, y3, "LimitFrame")
    Graphic.panels[#Graphic.panels + 1] = lfBg

    -- 4행: FrameLimit(1씩)
    Graphic.addStepper(makeStepper(TITLE.x, rowTop - 3 * pitch, "FrameLimit", 5, "Graphic", "frameLimit", 1, "%.0f", true))

    -- 5행: Brightness(0.01씩) + Gamma(0.1씩) — 라벨 이격을 좁혀 한 줄에.
    local y5 = rowTop - 4 * pitch
    Graphic.addStepper(makeStepper(TITLE.x, y5, "Bright", 6, "Graphic", "brightness", 0.01, "%.2f", false, OPTION_GAP))
    Graphic.addStepper(makeStepper(TITLE.x + selectorWidth("Bright", 6, OPTION_GAP) + OPTION_GAP, y5, "Gamma", 5, "Graphic", "gamma", 0.1, "%.1f", false, OPTION_GAP))

    -- 6행: FOV(1씩). near/far Clipping은 옵션에서 제외 — config.json 직접 편집으로 조절(far는 fog 거리보다 넉넉히).
    Graphic.addStepper(makeStepper(TITLE.x, rowTop - 5 * pitch, "FOV", 4, "Graphic", "fov", 1, "%.0f", true))

    Graphic.refresh()
    Graphic.setVisible(false)
end

-- 스테퍼를 목록에 등록하고 그 배경 패널을 가시성 토글에 편입한다.
function Graphic.addStepper(st)
    Graphic.steppers[#Graphic.steppers + 1] = st
    Graphic.panels[#Graphic.panels + 1] = st.bg
end

-- 현재 resolution 값의 옵션 순번(1-based). 못 찾으면 1.
function Graphic.resPosition()
    local cur = XOPS.Config:GetInt("Graphic", "resolution")
    for i = 1, #Graphic.resOptions do
        if Graphic.resOptions[i].index == cur then return i end
    end
    return 1
end

-- 표시 갱신(모든 체크/셀렉터/스테퍼).
function Graphic.refresh()
    setCheckText(Graphic.fullscreenCheck, XOPS.Config:GetBool("Graphic", "fullscreen"))
    local txt = "[" .. Graphic.resOptions[Graphic.resPosition()].label .. "]"
    Graphic.resValue.shadow:SetText(txt)
    Graphic.resValue.main:SetText(txt)
    setCheckText(Graphic.vsyncCheck, XOPS.Config:GetBool("Graphic", "vsync"))
    setCheckText(Graphic.limitFrameCheck, XOPS.Config:GetBool("Graphic", "limitFrame"))
    for i = 1, #Graphic.steppers do Graphic.steppers[i].refresh() end
end

-- 탭 표시/숨김. 보일 때 값 갱신.
function Graphic.setVisible(visible)
    for i = 1, #Graphic.panels do Graphic.panels[i]:SetActive(visible) end
    if visible then Graphic.refresh() end
end

-- 상호작용(체크 토글 + 해상도 << >> + 값 스테퍼 꾹 누름). 적용은 SAVE에서.
-- dt: 프레임 델타초 / pressed: 이번 프레임 눌림 / held: 좌클릭 유지 / clicked: 이번 프레임 클릭(뗌)
function Graphic.update(dt, pressed, held, clicked)
    -- Fullscreen 토글
    if checkboxHit(Graphic.fullscreenCheck, pressed, clicked, held) then
        XOPS.Config:SetBool("Graphic", "fullscreen", not XOPS.Config:GetBool("Graphic", "fullscreen"))
        setCheckText(Graphic.fullscreenCheck, XOPS.Config:GetBool("Graphic", "fullscreen"))
    end

    -- 해상도 << >>
    local pos = Graphic.resPosition()
    local d = arrowDir(Graphic.resPrev, Graphic.resNext, pos > 1, pos < #Graphic.resOptions, pressed, clicked, held)
    if d ~= 0 then
        XOPS.Config:SetInt("Graphic", "resolution", Graphic.resOptions[pos + d].index)
        Graphic.refresh()
        -- 해상도마다 UIScale 상한이 달라, C#이 초과분을 상한으로 내렸을 수 있으니 UI 배수를 다시 적용한다.
        applyUIScale()
    end

    -- VSync / LimitFrame 토글
    if checkboxHit(Graphic.vsyncCheck, pressed, clicked, held) then
        XOPS.Config:SetBool("Graphic", "vsync", not XOPS.Config:GetBool("Graphic", "vsync"))
        setCheckText(Graphic.vsyncCheck, XOPS.Config:GetBool("Graphic", "vsync"))
    end
    if checkboxHit(Graphic.limitFrameCheck, pressed, clicked, held) then
        XOPS.Config:SetBool("Graphic", "limitFrame", not XOPS.Config:GetBool("Graphic", "limitFrame"))
        setCheckText(Graphic.limitFrameCheck, XOPS.Config:GetBool("Graphic", "limitFrame"))
    end

    -- 값 스테퍼(꾹 누름 반복)
    for i = 1, #Graphic.steppers do Graphic.steppers[i].update(dt, pressed, held) end
end

-- ===== Sound 탭 섹션 =====
-- 모더 참고: 이 테이블이 Sound 탭 전체를 담는다. MasterVolume 스테퍼(0~1, 0.01씩). 적용은 SAVE에서.
local Sound = {
    panels = {},
    steppers = {},
}

-- MasterVolume 스테퍼 한 줄을 생성한다. rowTop=1행 top y.
function Sound.create(rowTop, pitch)
    local st = makeStepper(TITLE.x, rowTop, "MasterVolume", 6, "Sound", "MasterVolume", 0.01, "%.2f", false)
    Sound.steppers[#Sound.steppers + 1] = st
    Sound.panels[#Sound.panels + 1] = st.bg
    Sound.refresh()
    Sound.setVisible(false)
end

-- 표시 갱신(모든 스테퍼).
function Sound.refresh()
    for i = 1, #Sound.steppers do Sound.steppers[i].refresh() end
end

-- 탭 표시/숨김. 보일 때 값 갱신.
function Sound.setVisible(visible)
    for i = 1, #Sound.panels do Sound.panels[i]:SetActive(visible) end
    if visible then Sound.refresh() end
end

-- 상호작용(값 스테퍼 꾹 누름). dt: 프레임 델타초 / pressed: 이번 프레임 눌림 / held: 좌클릭 유지.
function Sound.update(dt, pressed, held)
    for i = 1, #Sound.steppers do Sound.steppers[i].update(dt, pressed, held) end
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
    backBgHandle:SetActive(not main and s ~= "exit")   -- EXIT 팝업은 ABORT로 돌아가므로 BACK 숨김
    creditPanelHandle:SetActive(s == "credit")
    exitPanelHandle:SetActive(s == "exit")
    sectionBgHandle:SetActive(s == "option")   -- 섹션 선택 바는 OPTION 화면에서만
    saveResetBgHandle:SetActive(s == "option") -- SAVE / RESET 버튼
    General.setVisible(s == "option" and selectedSection == "General")   -- General 옵션은 General 탭에서만
    Input.setVisible(s == "option" and selectedSection == "Input")       -- Input 옵션은 Input 탭에서만
    Graphic.setVisible(s == "option" and selectedSection == "Graphic")   -- Graphic 옵션은 Graphic 탭에서만
    Sound.setVisible(s == "option" and selectedSection == "Sound")       -- Sound 옵션은 Sound 탭에서만
    if s == "option" then
        refreshSectionBar()
    end
    setPageBarVisible(main and isAddon and multiplePages())   -- 페이지 바는 메인 + 애드온 탭 + 페이지 2개 이상일 때만
end

-- 서브 화면(OPTION/CREDIT) → 미션 리스트로 복귀. < BACK > 클릭과 ESC가 공유한다.
-- OPTION이었으면 저장하지 않은 변경을 취소하고 UI 배수도 저장값으로 되돌린다.
local function backToMain()
    if screen == "option" then
        XOPS.Config:RevertToSaved()   -- 저장 안 한 변경 취소
        applyUIScale()                -- UIScale도 저장값으로 되돌림
    end
    setScreen("main")
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
            SWITCH_FONT_W, SWITCH_FONT_H, mb.w, MENU_ROW_H, "BottomLeft", "MiddleLeft")
    end

    -- 돌아가기 버튼: 메뉴 버튼과 같은 좌하단 자리(스왑용). 시작은 메인 화면이라 숨김.
    local bb = BACK_BG
    backBgHandle = XOPS.UI:CreateImage(MENU_LAYER, SCALING, "BottomLeft", "",
        bb.x, bb.y, bb.w, bb.h, bb.r, bb.g, bb.b, bb.a)
    backBgSlots[1] = makeTextPair(backBgHandle, 0, BACK_ITEM, BTN_NORMAL,
        SWITCH_FONT_W, SWITCH_FONT_H, bb.w, MENU_ROW_H, "BottomLeft", "MiddleLeft")
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

    -- EXIT 확인 팝업: 중앙 백패널(폭은 질문에 맞춤) + 영문 질문(상단) + < EXIT > / < ABORT >(하단). 시작은 숨김.
    local eqW = #EXIT_QUESTION * EXIT_PANEL.qFontW
    exitPanelHandle = XOPS.UI:CreateImage(MENU_LAYER, SCALING, "Center", "",
        0, 0, eqW + 40, EXIT_PANEL.h, EXIT_PANEL.r, EXIT_PANEL.g, EXIT_PANEL.b, EXIT_PANEL.a)
    makeTextPair(exitPanelHandle, EXIT_PANEL.h * 0.28, EXIT_QUESTION, BTN_NORMAL,
        EXIT_PANEL.qFontW, EXIT_PANEL.qFontH, eqW, MENU_ROW_H, "Center", "MiddleCenter", 0)
    local exYesW = #EXIT_YES * SWITCH_FONT_W
    local exNoW = #EXIT_NO * SWITCH_FONT_W
    local exBtnY = -EXIT_PANEL.h * 0.28
    local exGap = 20
    exitYesSlot = makeTextPair(exitPanelHandle, exBtnY, EXIT_YES, BTN_NORMAL,
        SWITCH_FONT_W, SWITCH_FONT_H, exYesW, MENU_ROW_H, "Center", "MiddleCenter", -(exYesW * 0.5 + exGap * 0.5))
    exitNoSlot = makeTextPair(exitPanelHandle, exBtnY, EXIT_NO, BTN_NORMAL,
        SWITCH_FONT_W, SWITCH_FONT_H, exNoW, MENU_ROW_H, "Center", "MiddleCenter", exNoW * 0.5 + exGap * 0.5)
    exitPanelHandle:SetActive(false)

    -- OPTION 섹션 선택 바: 제목 아래 배경 스트립 + << / 섹션 3개 / >> (자식).
    -- 화살표(<< >>)는 글자 폭에 맞춰 칸을 자르고, 남은 가운데 폭을 섹션 3칸이 균등 분할. 섹션 글자는 MiddleCenter 정렬.
    local se = SECTION_BAR
    sectionBgHandle = XOPS.UI:CreateImage(MENU_LAYER, SCALING, "TopLeft", "",
        se.x, se.y, se.w, se.h, se.r, se.g, se.b, se.a)
    local prevW = #PAGE_PREV * SWITCH_FONT_W   -- << 글자 폭에 맞춘 칸
    local nextW = #PAGE_NEXT * SWITCH_FONT_W   -- >> 글자 폭에 맞춘 칸
    local secMid = (se.w - prevW - nextW) / SECTION_PER_PAGE   -- 남은 가운데 폭을 섹션 3칸이 균등 분할
    sectionPrevSlot = makeTextPair(sectionBgHandle, 0, PAGE_PREV, BTN_NORMAL,
        SWITCH_FONT_W, SWITCH_FONT_H, prevW, se.h, "TopLeft", "TopLeft", 0)
    for i = 0, SECTION_PER_PAGE - 1 do
        sectionSlots[i] = makeTextPair(sectionBgHandle, 0, "", BTN_NORMAL,
            SWITCH_FONT_W, SWITCH_FONT_H, secMid, se.h, "TopLeft", "MiddleCenter", prevW + secMid * i)
    end
    sectionNextSlot = makeTextPair(sectionBgHandle, 0, PAGE_NEXT, BTN_NORMAL,
        SWITCH_FONT_W, SWITCH_FONT_H, nextW, se.h, "TopRight", "TopRight", 0)
    sectionNames = XOPS.Config:GetSectionNames()
    selectedSection = sectionNames[1] or ""   -- 기본 선택: 첫 섹션
    refreshSectionBar()
    sectionBgHandle:SetActive(false)

    -- OPTION General 탭 생성(섹션 테이블).
    General.create()

    -- 옵션 초기값 등록(RESET에서 이 값으로 복구).
    for i = 1, #OPTION_DEFAULTS do
        local d = OPTION_DEFAULTS[i]
        XOPS.Config:SetDefault(d[1], d[2], d[3])
    end

    -- OPTION Input 탭 생성(섹션 테이블).
    Input.create()

    -- OPTION Graphic 탭 생성(섹션 테이블).
    Graphic.create(OPTION_ROW_TOP, OPTION_ROW_PITCH)

    -- OPTION Sound 탭 생성(섹션 테이블).
    Sound.create(OPTION_ROW_TOP, OPTION_ROW_PITCH)

    -- OPTION SAVE / RESET: 우측 하단(BottomRight, < BACK >과 같은 이격). < SAVE >< RESET > 가로 배치.
    local saveW = #SAVE_ITEM * SWITCH_FONT_W
    local resetW = #RESET_ITEM * SWITCH_FONT_W
    saveResetBgHandle = XOPS.UI:CreateImage(MENU_LAYER, SCALING, "BottomRight", "",
        -BACK_BG.x, BACK_BG.y, saveW + resetW, MENU_ROW_H, BACK_BG.r, BACK_BG.g, BACK_BG.b, BACK_BG.a)
    resetSlot = makeTextPair(saveResetBgHandle, 0, RESET_ITEM, BTN_NORMAL,
        SWITCH_FONT_W, SWITCH_FONT_H, resetW, MENU_ROW_H, "BottomRight", "MiddleRight", 0)
    saveSlot = makeTextPair(saveResetBgHandle, 0, SAVE_ITEM, BTN_NORMAL,
        SWITCH_FONT_W, SWITCH_FONT_H, saveW, MENU_ROW_H, "BottomRight", "MiddleRight", -resetW)
    saveResetBgHandle:SetActive(false)

    -- 마우스 십자 커서 두 줄. 가로선=풀폭 1px, 세로선=풀높이 1px (스트레치 축은 size 0 = 풀, 반대 축이 두께).
    local c = POINTER_COLOR
    hLineHandle = XOPS.UI:CreateImage(POINTER_LAYER, true, "StretchMiddle", "", 0, 0, 0, 1, c.r, c.g, c.b, c.a)
    vLineHandle = XOPS.UI:CreateImage(POINTER_LAYER, true, "StretchCenter", "", 0, 0, 1, 0, c.r, c.g, c.b, c.a)

    -- 풀스크린 검정(알파 1)으로 시작 → update에서 fadeInValue로 0까지 투명화.
    fadeHandle = XOPS.UI:CreateImage(FADE_LAYER, SCALING, "StretchFull", "", 0, 0, 0, 0, 0, 0, 0, 1)

    applyUIScale()   -- 저장된 UIScale을 컨텐츠 레이어에 적용
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

    local pressed = firePressed(t)              -- 눌린 순간(누름 캡처 + 스크롤바 드래그 시작)
    local clicked = fireReleased(t)             -- 뗀 순간(버튼 클릭 판정)
    local held = XOPS.Input:IsPressed("fire")   -- 좌클릭 유지 여부

    -- 새로 누르면 캡처를 비운다 → 이번 프레임 hover된 요소가 다시 잡는다(빈 공간이면 캡처 없음).
    if pressed then
        pressCapture = nil
    end

    if screen == "main" then
        -- === 미션 리스트 화면 ===
        -- ESC → 종료 확인 팝업
        if escPressed(t) then
            setScreen("exit")
            return
        end

        local count = currentCount()
        local maxIndex = math.max(0, count - ITEM_COUNT)

        -- 스크롤바 드래그: 트랙 위에서 누르면 시작. 잡은 지점(grabOffset) 유지하며 손잡이가 커서를 따라간다.
        -- 다른 요소보다 먼저 처리해야 dragging 상태가 이번 프레임 hover 억제에 반영된다.
        if scrollable then
            local barHeight  = SCROLL_TRACK.h * ITEM_COUNT / count
            local trackRange = SCROLL_TRACK.h - barHeight
            if pressed and trackHandle:IsHovered() then
                local barTopFromTop = trackRange * scrollIndex / maxIndex
                grabOffset = (SCROLL_TRACK.h - trackHandle:PointerLocalY()) - barTopFromTop
                if grabOffset < 0 then grabOffset = 0 elseif grabOffset > barHeight then grabOffset = barHeight end
                dragging = true
                pressCapture = trackHandle   -- 트랙이 누름 소유 → 끌다 다른 요소 위에서 떼도 그 요소는 무반응
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

        -- UP/DOWN: 색+눌림 + 클릭(±1). 누름 캡처로 "여기서 눌러 여기서 뗀" 경우만 동작.
        local upHover = not dragging and upSlot.main:IsHovered()
        local downHover = not dragging and downSlot.main:IsHovered()
        local upOwn = pressOwner(upSlot, upHover, pressed)
        local downOwn = pressOwner(downSlot, downHover, pressed)
        applyButtonColor(upSlot, scrollIndex <= 0, upHover)
        applyButtonColor(downSlot, scrollIndex >= maxIndex, downHover)
        setPressed(upSlot, upHover and held and upOwn and scrollIndex > 0)
        setPressed(downSlot, downHover and held and downOwn and scrollIndex < maxIndex)
        if clicked then
            local changed = false
            if upHover and upOwn and scrollIndex > 0 then
                scrollIndex = scrollIndex - 1
                changed = true
            elseif downHover and downOwn and scrollIndex < maxIndex then
                scrollIndex = scrollIndex + 1
                changed = true
            end
            if changed then
                refreshItems()
                updateScrollThumb()
            end
        end

        -- 탭 스위치: 색+눌림 + 클릭 → official↔addon 토글 (addon 있을 때만).
        if addonExists then
            local swHover = not dragging and switchSlot.main:IsHovered()
            local swOwn = pressOwner(switchSlot, swHover, pressed)
            applyButtonColor(switchSlot, false, swHover)
            setPressed(switchSlot, swHover and held and swOwn)
            if clicked and swHover and swOwn then
                switchTab(not isAddon)
            end
        end

        -- 애드온 페이지 << 이전 / >> 다음: 경계에서 비활성(회색), 가능할 때만 클릭 처리. 페이지 바뀌면 스크롤 초기화.
        if isAddon and multiplePages() then
            local pageCount = XOPS.Data:GetAddonPageCount()
            local prevHover = not dragging and pagePrevSlot.main:IsHovered()
            local nextHover = not dragging and pageNextSlot.main:IsHovered()
            local prevOwn = pressOwner(pagePrevSlot, prevHover, pressed)
            local nextOwn = pressOwner(pageNextSlot, nextHover, pressed)
            applyButtonColor(pagePrevSlot, page <= 0, prevHover)
            applyButtonColor(pageNextSlot, page >= pageCount - 1, nextHover)
            setPressed(pagePrevSlot, prevHover and held and prevOwn and page > 0)
            setPressed(pageNextSlot, nextHover and held and nextOwn and page < pageCount - 1)
            if clicked then
                local changed = false
                if prevHover and prevOwn and page > 0 then
                    page = page - 1
                    changed = true
                elseif nextHover and nextOwn and page < pageCount - 1 then
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

        -- 하단 메뉴 버튼: 색+눌림 + 클릭. OPTION(1)/CREDIT(2)/EXIT(3).
        for i = 1, #menuSlots do
            local slot = menuSlots[i]
            local hovered = not dragging and slot.main:IsHovered()
            local own = pressOwner(slot, hovered, pressed)
            applyButtonColor(slot, false, hovered)
            setPressed(slot, hovered and held and own)
            if clicked and hovered and own then
                if i == 1 then          -- OPTION
                    setScreen("option")
                    return
                elseif i == 2 then      -- CREDIT
                    setScreen("credit")
                    return
                elseif i == 3 then      -- EXIT → 종료 확인 팝업
                    setScreen("exit")
                    return
                end
            end
        end

        -- 미션 항목: 색+눌림 + 클릭 시 로드 (보이는 칸만). 스크롤을 끌다 여기로 삐져나와 떼도 캡처가 달라 로드되지 않는다.
        for i = 0, ITEM_COUNT - 1 do
            if scrollIndex + i < count then
                local slot = itemSlots[i]
                local hovered = not dragging and slot.main:IsHovered()
                local own = pressOwner(slot, hovered, pressed)
                local c = hovered and ITEM_HOVER or ITEM_NORMAL
                slot.main:SetColor(c.r, c.g, c.b, 1)
                setPressed(slot, hovered and held and own)
                if clicked and hovered and own then
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
    elseif screen == "exit" then
        -- === EXIT 확인 팝업: < EXIT >=종료 / < ABORT >=취소, ESC로도 취소 ===
        local yesHover = exitYesSlot.main:IsHovered()
        local noHover = exitNoSlot.main:IsHovered()
        local yesOwn = pressOwner(exitYesSlot, yesHover, pressed)
        local noOwn = pressOwner(exitNoSlot, noHover, pressed)
        applyButtonColor(exitYesSlot, false, yesHover)
        applyButtonColor(exitNoSlot, false, noHover)
        setPressed(exitYesSlot, yesHover and held and yesOwn)
        setPressed(exitNoSlot, noHover and held and noOwn)
        if clicked then
            if yesHover and yesOwn then
                XOPS.Scene:Quit()       -- 게임 종료
                return
            elseif noHover and noOwn then
                setScreen("main")       -- 취소 → 미션 선택으로 복귀
                return
            end
        end
        if escPressed(t) then           -- ESC → 팝업 닫기(취소)
            setScreen("main")
            return
        end
    else
        -- === 서브 화면 (OPTION/CREDIT) ===
        -- BACK 버튼(공통): 미션 리스트로 복귀. ESC도 < BACK > 클릭과 동일하게 동작한다.
        local slot = backBgSlots[1]
        local hovered = slot.main:IsHovered()
        local backOwn = pressOwner(slot, hovered, pressed)
        applyButtonColor(slot, false, hovered)
        setPressed(slot, hovered and held and backOwn)
        if (clicked and hovered and backOwn) or escPressed(t) then
            backToMain()
            return
        end

        -- OPTION: 섹션 선택 바 (<< 섹션3개 >>). 섹션을 3개씩 페이지 넘기며 하나를 고른다.
        if screen == "option" then
            local sectionCount = #sectionNames
            local pageCount = math.max(1, math.ceil(sectionCount / SECTION_PER_PAGE))

            -- << 이전 / >> 다음: 경계에서 비활성(회색), 색+눌림 + 클릭 방향.
            local sectionDir = arrowDir(sectionPrevSlot, sectionNextSlot,
                sectionPage > 0, sectionPage < pageCount - 1, pressed, clicked, held)

            -- 섹션 칸: 선택된 섹션은 강조(호버색), 클릭 시 선택. 보이는 칸만.
            for i = 0, SECTION_PER_PAGE - 1 do
                local si = sectionPage * SECTION_PER_PAGE + i
                if si < sectionCount then
                    local sslot = sectionSlots[i]
                    local name = sectionNames[si + 1]
                    local isSelected = (name == selectedSection)
                    local shover = sslot.main:IsHovered()
                    local sOwn = pressOwner(sslot, shover, pressed)
                    applyButtonColor(sslot, isSelected, shover)   -- 선택된 섹션은 비활성 회색
                    setPressed(sslot, shover and held and sOwn and not isSelected)
                    if clicked and shover and sOwn and not isSelected then
                        selectedSection = name
                        General.setVisible(selectedSection == "General")   -- 탭 바뀌면 옵션 표시 전환
                        Input.setVisible(selectedSection == "Input")
                        Graphic.setVisible(selectedSection == "Graphic")
                        Sound.setVisible(selectedSection == "Sound")
                    end
                end
            end

            -- << / >> 클릭 → 페이지 이동(가능할 때만).
            if sectionDir ~= 0 then
                sectionPage = sectionPage + sectionDir
                refreshSectionBar()
            end

            -- 탭별 옵션은 해당 섹션이 선택됐을 때만 처리(숨은 요소 오클릭 방지).
            if selectedSection == "General" then
                General.update(dt, pressed, held, clicked)
            elseif selectedSection == "Input" then
                Input.update(dt, pressed, held, clicked)
            elseif selectedSection == "Graphic" then
                Graphic.update(dt, pressed, held, clicked)
            elseif selectedSection == "Sound" then
                Sound.update(dt, pressed, held)
            end

            -- SAVE / RESET 버튼: 색+눌림 + 클릭. SAVE=저장, RESET=초기화.
            local saveHover = saveSlot.main:IsHovered()
            local resetHover = resetSlot.main:IsHovered()
            local saveOwn = pressOwner(saveSlot, saveHover, pressed)
            local resetOwn = pressOwner(resetSlot, resetHover, pressed)
            applyButtonColor(saveSlot, false, saveHover)
            applyButtonColor(resetSlot, false, resetHover)
            setPressed(saveSlot, saveHover and held and saveOwn)
            setPressed(resetSlot, resetHover and held and resetOwn)
            if clicked then
                if saveHover and saveOwn then
                    XOPS.Config:Save()          -- 설정 저장
                    XOPS.Config:ApplyGraphic()  -- 그래픽(전체화면/해상도) 적용
                elseif resetHover and resetOwn then
                    XOPS.Config:ResetToDefaults()   -- 초기값으로 복구
                    General.refreshAll()
                    Input.refreshAll()
                    Graphic.refresh()
                    Sound.refresh()
                    applyUIScale()   -- UIScale도 초기값으로 적용
                end
            end
        end
    end
end

return M
