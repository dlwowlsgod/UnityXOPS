-- ============================================================
--  메인게임 HUD 스크립트 (maingame.lua)
--  게임 플레이 중 화면 위에 그려지는 HUD와 조준 표시를 그린다.
--  F2로 표시모드 순환: Normal(전체) → Simple(간이) → Off(끔). zoom 입력으로 스코프 토글.
-- ============================================================

local M = {}

-- ----- 레이어 (숫자 클수록 위에 그림) -----
-- 피격 번쩍은 크로스헤어/HUD보다 아래라 그 둘은 빨갛게 물들지 않는다.
local BLIND_LAYER     = 1000   -- 벽 블라인드(맨 아래 — 게임 화면만 가리고 HUD는 그대로 보인다)
local CENTER_LAYER    = 1001   -- 재장전/교체(화면 중앙)
local SCOPE_LAYER     = 1002   -- 스코프 그림/바깥 검정 바/조준선
local MESSAGE_LAYER   = 1003   -- 이벤트 메시지(화면 하단)
local FPS_LAYER       = 1004   -- FPS 카운터
local FLASH_LAYER     = 1005   -- 피격 번쩍(화면 전체)
local CROSSHAIR_LAYER = 1006   -- 크로스헤어
local FRAME_LAYER     = 1007   -- 하단 장식 프레임(Normal 전용)
local WEAPON_LAYER    = 1008   -- 무기 뷰(Normal 전용 — 프레임 위, 글자 아래)
local HUD_LAYER       = 1009   -- 체력/탄약/무기명(Normal)
local SIMPLE_LAYER    = 1010   -- 간이 HUD(체력 테두리/명판/무기명)
local FADE_LAYER      = 1011   -- 화면 암전(HUD까지 덮는다)
local ENDING_LAYER    = 1012   -- 미션 종료 문구(맨 위)

-- HUD/크로스헤어/번쩍/무기 뷰는 640x480 픽셀 1:1 고정(UIScale 배수 적용).
-- 스코프/중앙 표시/메시지는 화면 높이에 맞춰 확대(UIScale 미적용).
local SCALING = false
local SCALED = true
local UI_SCALE_LAYERS = { FPS_LAYER, FLASH_LAYER, CROSSHAIR_LAYER, FRAME_LAYER, WEAPON_LAYER, HUD_LAYER, SIMPLE_LAYER }

-- ----- 표시 모드 (F2로 순환) -----
-- "normal" = 전체 HUD, "simple" = 간이 HUD, "off" = 끔. 재장전/교체 표시는 모드와 무관하게 뜬다.
local UIMODE_ACTION  = "uimode"
local UIMODE_BINDING = "<Keyboard>/f2"
local MODE_ORDER = { "normal", "simple", "off" }
local mode = "normal"

-- ----- 입력 -----
-- F1 시점 전환 / F12 재시작 / ESC 나가기. 미션이 (재)시작되고 INPUT_LOCK 초 동안은 나가기·재시작을 막는다
-- (화면이 바뀌는 순간 눌려 있던 키가 새어 들어와 바로 튕기는 것 방지).
local VIEWMODE_ACTION  = "viewmode"
local VIEWMODE_BINDING = "<Keyboard>/f1"
local RESTART_ACTION   = "restart"
local RESTART_BINDING  = "<Keyboard>/f12"
local INPUT_LOCK = 1.0
local MAINMENU_SCENE = 2
local inputLock = INPUT_LOCK

-- ----- 하단 장식 프레임 (Normal 전용) -----
-- char.dds의 박스 글리프(코드 0xB0~0xD2)로 좌/우하단 테두리를 그린다.
-- pivot=화면 기준점, x/y=첫 글자 기준 오프셋, codes=글리프 코드열.
local FRAME_FONT  = { w = 32, h = 32 }
local FRAME_COLOR = { r = 1, g = 1, b = 1, a = 0.75 }
local FRAME_LINES = {
    { pivot = "BottomLeft",  align = "BottomLeft",  x = 15, y = 73, codes = { 0xB3, 0xB4, 0xB4, 0xB4, 0xB4, 0xB4, 0xB4, 0xB5 } },
    { pivot = "BottomLeft",  align = "BottomLeft",  x = 15, y = 41, codes = { 0xC3, 0xC4, 0xC4, 0xC4, 0xC4, 0xC4, 0xC4, 0xC5 } },
    { pivot = "BottomLeft",  align = "BottomLeft",  x = 15, y = 23, codes = { 0xB3, 0xB4, 0xB4, 0xB6, 0xB7, 0xB7, 0xB7, 0xB8, 0xB9 } },
    { pivot = "BottomLeft",  align = "BottomLeft",  x = 15, y = -9, codes = { 0xC3, 0xC4, 0xC4, 0xC6, 0xC7, 0xC7, 0xC7, 0xC8, 0xC9 } },
    { pivot = "BottomRight", align = "BottomRight", x = 0,  y = 66, codes = { 0xB0, 0xB1, 0xB1, 0xB1, 0xB1, 0xB1, 0xB1, 0xB2 } },
    { pivot = "BottomRight", align = "BottomRight", x = 0,  y = 34, codes = { 0xC0, 0xC1, 0xC1, 0xC1, 0xC1, 0xC1, 0xC1, 0xC2 } },
    { pivot = "BottomRight", align = "BottomRight", x = 0,  y = 2,  codes = { 0xD0, 0xD1, 0xD1, 0xD1, 0xD1, 0xD1, 0xD1, 0xD2 } },
}
local frames = {}   -- 프레임 핸들(모드 전환 시 표시 토글)

-- ----- 정상(Normal) HUD 레이아웃 -----
-- 좌하단: STATE 라벨 + 체력 상태 + 탄약. 우하단(우측 가장자리 기준): 무기명. 중앙: 재장전/교체.
local N = {
    stateX = 23, stateY = 21, stateW = 18, stateH = 24,   -- "STATE"
    hpY = 21, hpW = 18, hpH = 24,                          -- 체력 상태(x는 hpX())
    ammoX = 25, ammoY = 72, ammoW = 23, ammoH = 24,        -- 탄약 "»탄창 º예비"
    wnX = -234, wnY = 75, wnW = 16, wnH = 20,              -- 무기명(우측기준, 좌측정렬, 세로중앙 y=85)
    centerFontW = 32, centerFontH = 34,                    -- 재장전/교체(중앙)
    reloadMainX = 0, reloadMainY = -60,
    reloadShadowX = 3, reloadShadowY = -63,
    shadow = { r = 0.2, g = 0.2, b = 0.2 },
    state = nil, hp = nil, ammo = nil, weaponName = nil,
    reloadMain = nil, reloadShadow = nil, changeMain = nil, changeShadow = nil,
}

-- ----- 간이(Simple) HUD 레이아웃 -----
-- 화면 상하좌우 가장자리 1px 테두리(체력 색) + 좌하단 반투명 명판 + 그 위 무기명.
local S = {
    -- 체력 테두리 바(가장자리 stretch, 좌/우는 세로로 1px, 위/아래는 가로로 1px)
    bars = {
        { pivot = "StretchLeft",   x = 0, y = 0, w = 1, h = -2 },
        { pivot = "StretchRight",  x = 0, y = 0, w = 1, h = -2 },
        { pivot = "StretchTop",    x = 0, y = 0, w = 0, h = 1 },
        { pivot = "StretchBottom", x = 0, y = 0, w = 0, h = 1 },
    },
    panelX = 8, panelY = 7, panelW = 219, panelH = 25,     -- 명판(반투명 검정, 좌하단)
    panelColor = { r = 0, g = 0, b = 0, a = 0.298 },
    nameX = 8, nameY = 9.5, nameW = 16, nameH = 20,        -- 무기명(명판 위, 좌측정렬, 세로중앙 19.5)
    barHandles = {}, panel = nil, name = nil,
}

-- ----- 무기 뷰 (우하단) -----
-- 들고 있는 무기를 3D로 비추는 화면. 배경이 투명이라 뒤의 장식 프레임이 비친다.
-- 모델을 만들고 비추는 건 엔진이 하고, 화면 배치와 무기 자리(위치/크기/회전)는 여기서 정한다.
-- x/y/w/h: 화면 우하단 기준 배치. 아래 자리 좌표는 3D 공간 값이며, 무기 뷰 카메라가 원점을 비춘다.
local WEAPON = {
    x = 0, y = 0, w = 256, h = 256,
    -- 들고 있는 무기: 크게 놓고 Y축으로 계속 돌린다(spin = 초당 도).
    mainX = 0, mainY = -4.3, mainZ = 0, mainScale = 0.8, spin = 66.667,
    -- 메고 있는 무기: 작게 놓고 고정(subYaw = Y축 회전 각도, 도).
    subX = 2.5, subY = -5, subZ = 0, subScale = 0.4, subYaw = 90,
    -- 무기를 비추는 카메라 구도. 무기 자리가 원점 근처라 뒤로 물러나 바라본다.
    -- fov를 좁히면 무기가 크게 잡히고, 배경 알파를 올리면 무기 뷰 뒤의 UI가 가려진다.
    camX = 0, camY = 0, camZ = -10, camPitch = 0, camYaw = 0, camRoll = 0,
    camFov = 65, camBg = { r = 0, g = 0, b = 0, a = 0 },
    handle = nil, main = nil, sub = nil, cam = nil, yaw = 0,
}

-- ----- 크로스헤어 -----
-- 화면 중앙에서 상하좌우로 벌어지는 4개의 막대. 크기/색/간격은 설정(General)에서 읽는다.
-- 간격은 StaticAim이 켜져 있으면 aimGap 고정, 꺼져 있으면 aimGap + 조준오차(움직일수록·쏠수록 벌어짐).
local CH = {
    gap = 0, thick = 1, length = 10, static = false,
    r = 1, g = 0, b = 0, a = 1,
    left = nil, right = nil, up = nil, down = nil,
    shown = nil, lastGap = nil,
}

-- ----- 스코프 -----
-- 스코프 그림은 화면비와 무관하게 제 비율(aspect)을 지키며 화면에 최대로 내접하고,
-- 남는 바깥 영역은 검정 바 4개로 채운다. 조준선은 그 스코프를 처음 쓸 때 만들어 재사용한다.
local SCOPE_ACTION = "zoom"   -- 스코프 토글 입력(기본 LeftShift, 설정에서 리바인딩 가능)
local SC = {
    image = nil,
    bars = {},          -- 바깥 검정 바(좌/우/상/하)
    reticles = {},      -- [스코프번호] = 조준선 핸들 목록
    aspect = 4 / 3,     -- 현재 스코프 그림 비율
    fov = 0,            -- 현재 스코프 시야각(도)
    hideCrosshair = false,
}
local shownScope = nil   -- 지금 화면에 떠 있는 스코프 번호(없으면 nil)
local baseFov = 65       -- 스코프를 안 볼 때의 시야각(설정 Graphic.fov)

-- ----- 이벤트 메시지 (화면 하단) -----
-- 미션 이벤트가 띄우는 안내문. 나타남/유지/사라짐은 엔진이 계산한 진하기를 그대로 쓴다.
local MSG = {
    boxH = 140, fontSize = 18,   -- 하단에서 boxH 높이의 전폭 상자, 그 위쪽에 가운데 정렬
    r = 1, g = 1, b = 1,
    handle = nil, lastId = -1,
}

-- ----- 피격 번쩍 (화면 전체) -----
-- 맞으면 화면을 잠깐 빨갛게. hold 동안 최대 진하기 유지 후 fade 동안 옅어진다(fade=0이면 깜빡 한 번).
local FLASH = {
    r = 1, g = 0, b = 0, a = 0.5,
    hold = 0.05, fade = 0.1,
    handle = nil, timer = 0, shown = false, lastPlayer = nil,
}

-- ----- 벽 블라인드 -----
-- 카메라가 벽에 파묻히면 벽 너머가 비쳐 보이므로, 파묻힌 방향의 화면 절반을 검게 덮는다.
-- 네 방향은 따로 판정하므로 동시에 여러 개가 켜질 수 있다(예: 위+왼쪽 → 화면 3/4이 검정).
local BLIND = { handles = {}, shown = { false, false, false, false } }
-- 상/하/좌/우 순서. half="h"면 화면 높이의 절반, "w"면 폭의 절반을 덮는다.
local BLIND_BOXES = {
    { pivot = "StretchTop",    half = "h" },
    { pivot = "StretchBottom", half = "h" },
    { pivot = "StretchLeft",   half = "w" },
    { pivot = "StretchRight",  half = "w" },
}

-- ----- 화면 암전 -----
-- 미션이 시작되면 검은 화면에서 inTime 초에 걸쳐 밝아지고, 끝나면 outTime 초에 걸쳐 어두워진다.
local FADE = { inTime = 2, outTime = 3.5, handle = nil }

-- ----- 미션 종료 문구 (화면 중앙) -----
-- 판정이 나면 inTime 초 나타나 hold 초 머물고 outTime 초에 걸쳐 사라진다.
-- 암전과 이 문구 중 더 긴 쪽이 끝나면 결과 화면으로 넘어간다.
local END = {
    inTime = 1, hold = 2, outTime = 1,
    fontW = 28, fontH = 32,
    successText = "objective complete", successR = 1, successG = 0.5, successB = 0,
    failText    = "mission failure",    failR = 1, failG = 0, failB = 0,
    handle = nil,
}
local RESULT_SCENE = 5   -- 미션이 끝나면 갈 화면

-- ----- FPS 카운터 (우상단) -----
local FPS = { x = -10, y = -10, w = 18, h = 24, r = 1, g = 0, b = 1, interval = 0.5 }

-- ----- 런타임 상태 (자동 관리) -----
local fpsText = nil
local fpsShown = true
local fpsAccum = 0
local fpsFrames = 0
local playT = 0            -- 미션 시작 후 경과(초) — 밝아지는 연출용
local endT = nil           -- 판정이 난 뒤 경과(초). nil이면 아직 진행 중
local lastStart = nil      -- 마지막으로 본 미션 시작 횟수(바뀌면 재시작된 것)
local resultLoading = false -- 결과 화면 전환 1회 가드
-- 표시 갱신 최소화용 캐시(값이 바뀔 때만 다시 칠함). Normal/Simple 공용(모드 전환 시 무효화).
local lastHP = nil
local lastMag = nil
local lastRes = nil
local lastName = nil

-- 코드포인트 목록을 char.dds 글리프 문자열로 만든다. 0x80~0x7FF를 2바이트 UTF-8로 인코딩해
-- XLua 문자열 경계를 통과시킨다(글자 코드가 그대로 char.dds 셀 = code%16, code/16 으로 매핑된다).
local function glyphString(codes)
    local s = ""
    for i = 1, #codes do
        local c = codes[i]
        s = s .. string.char(0xC0 + math.floor(c / 64), 0x80 + (c % 64))
    end
    return s
end

-- 탄약 앞 글리프(»=탄창, º=예비). glyphString 정의 이후 1회 캐시.
local AMMO_MAG_GLYPH = glyphString({ 0xBB })
local AMMO_RES_GLYPH = glyphString({ 0xBA })

-- 두 인자 아크탄젠트(선의 기울기 → 각도). Lua 버전마다 이름이 달라 있는 쪽을 쓴다.
local atan2 = math.atan2 or math.atan

-- 체력을 상태 색(녹→황→적)으로 변환한다. 반환: r, g, b (0~1).
local function stateColor(hp)
    if hp >= 100 then return 0, 1, 0
    elseif hp >= 50 then return (100 - hp) / 50, 1, 0
    elseif hp > 0 then return 1, hp / 50, 0
    else return 1, 0, 0 end
end

-- 체력을 상태 텍스트로 변환한다.
local function hpText(hp)
    if hp >= 80 then return "FINE"
    elseif hp >= 40 then return "CAUTION"
    elseif hp > 0 then return "DANGER"
    else return "DEAD" end
end

-- 상태 텍스트 길이에 맞춘 체력 텍스트 X 위치(좌하단 기준).
local function hpX(hp)
    if hp >= 80 then return 155
    elseif hp >= 40 then return 135
    elseif hp > 0 then return 140
    else return 155 end
end

-- 무기명 글자 폭 — limit자 넘으면 폭만 비율 축소(높이 유지).
local function weaponFontW(name, limit)
    if #name <= limit then return 16 end
    return 16 * limit / #name
end

-- 예비탄 표시 문자열(1000 이상은 999+).
local function reserveText(res)
    if res > 999 then return "999+" end
    return tostring(res)
end

-- config General.UIScale 값을 HUD 레이어들에 배수로 적용한다.
local function applyUIScale()
    local s = XOPS.Config:GetFloat("General", "UIScale")
    for i = 1, #UI_SCALE_LAYERS do
        XOPS.UI:SetScaleFactor(UI_SCALE_LAYERS[i], s)
    end
end

-- 표시 여부 토글 헬퍼(그룹별).
local function setFramesVisible(v)
    for i = 1, #frames do frames[i]:SetActive(v) end
end

local function setNormalVisible(v)
    N.state:SetActive(v)
    N.hp:SetActive(v)
    N.ammo:SetActive(v)
    N.weaponName:SetActive(v)
end

local function setSimpleVisible(v)
    for i = 1, #S.barHandles do S.barHandles[i]:SetActive(v) end
    S.panel:SetActive(v)
    S.name:SetActive(v)
end

-- 표시 모드를 적용한다(요소 가시성). 재장전/교체는 별도(플레이어 상태로 매 프레임 토글).
local function applyMode()
    local n = mode == "normal"
    setFramesVisible(n)
    setNormalVisible(n)
    WEAPON.handle:SetActive(n)
    setSimpleVisible(mode == "simple")
end

-- 크로스헤어 4개 막대의 표시 여부(변할 때만 토글).
local function setCrosshairVisible(v)
    if v == CH.shown then return end
    CH.shown = v
    CH.left:SetActive(v)
    CH.right:SetActive(v)
    CH.up:SetActive(v)
    CH.down:SetActive(v)
end

-- 크로스헤어 막대를 중앙에서 gap 만큼 떨어뜨린다(막대 안쪽 끝이 중앙에서 gap).
local function spreadCrosshair(gap)
    local off = gap + CH.length * 0.5
    CH.left:SetPosition(-off, 0)
    CH.right:SetPosition(off, 0)
    CH.up:SetPosition(0, off)
    CH.down:SetPosition(0, -off)
end

-- 스코프 요소(그림/바/해당 조준선)의 표시 여부를 정한다.
-- index: 스코프 번호, v: 표시 여부
local function setScopeVisible(index, v)
    SC.image:SetActive(v)
    for i = 1, #SC.bars do SC.bars[i]:SetActive(v) end
    local list = SC.reticles[index]
    if list then
        for i = 1, #list do list[i]:SetActive(v) end
    end
end

-- 스코프 조준선을 만든다. 이미 만든 스코프면 그대로 재사용한다(미리 전부 만들지 않는다).
-- index: 스코프 번호, lines: 선분 목록({x1,y1,x2,y2,r,g,b,a,width})
local function buildReticles(index, lines)
    if SC.reticles[index] then return end

    local list = {}
    for i = 1, #lines do
        local l = lines[i]
        local dx, dy = l.x2 - l.x1, l.y2 - l.y1
        local len = math.sqrt(dx * dx + dy * dy)
        local h = XOPS.UI:CreateImage(SCOPE_LAYER, SCALED, "Center", "",
            (l.x1 + l.x2) / 2, (l.y1 + l.y2) / 2, len, math.max(1, l.width), l.r, l.g, l.b, l.a)
        h:SetRotation(math.deg(atan2(dy, dx)))
        h:SetActive(false)
        list[i] = h
    end
    SC.reticles[index] = list
end

-- 스코프 그림을 화면비와 무관하게 제 비율로 최대 내접시키고, 남는 바깥을 검정 바로 채운다.
-- 창 크기가 바뀔 수 있으므로 스코프를 보는 동안 매 프레임 다시 맞춘다.
local function layoutScope()
    local w = XOPS.UI:GetLayerWidth(SCOPE_LAYER, SCALED)
    local h = XOPS.UI:GetLayerHeight(SCOPE_LAYER, SCALED)
    if w <= 0 or h <= 0 then return end

    local sw, sh
    if w / h >= SC.aspect then
        sw, sh = h * SC.aspect, h    -- 화면이 더 넓다 → 높이에 맞추고 좌우가 남는다
    else
        sw, sh = w, w / SC.aspect    -- 화면이 더 좁다 → 너비에 맞추고 상하가 남는다
    end

    -- 남는 폭/높이를 반씩 나눠 바깥 바로. 한 쌍은 0이 되어 보이지 않는다.
    -- 1픽셀도 안 되는 자투리는 비율값의 반올림 오차이므로(예: 4:3을 1.333으로 적은 경우) 꽉 채운다.
    local gw, gh = (w - sw) / 2, (h - sh) / 2
    if gw < 0.5 then sw, gw = w, 0 end
    if gh < 0.5 then sh, gh = h, 0 end
    SC.image:SetSize(sw, sh)
    SC.bars[1]:SetPosition(-(sw + gw) / 2, 0); SC.bars[1]:SetSize(gw, h)
    SC.bars[2]:SetPosition((sw + gw) / 2, 0);  SC.bars[2]:SetSize(gw, h)
    SC.bars[3]:SetPosition(0, (sh + gh) / 2);  SC.bars[3]:SetSize(w, gh)
    SC.bars[4]:SetPosition(0, -(sh + gh) / 2); SC.bars[4]:SetSize(w, gh)
end

-- 스코프 표시를 현재 상태에 맞춘다(상태는 엔진이 소유 — 여기선 읽기만 한다).
-- 반환: 스코프가 크로스헤어를 대신하므로 숨겨야 하는가.
local function applyScope()
    -- 3인칭에선 스코프를 그리지 않는다(상태는 켜져 있을 수 있다).
    local scoping = XOPS.Player:IsScoping() and XOPS.Player:IsFirstPerson()
    local index = nil
    if scoping then index = XOPS.Player:GetScopeIndex() end

    if index ~= shownScope then
        if shownScope ~= nil then setScopeVisible(shownScope, false) end

        if index ~= nil then
            local s = XOPS.Player:GetActiveScope()
            if s == nil then
                index = nil
            else
                SC.aspect = (s.aspect > 0) and s.aspect or (4 / 3)
                SC.fov = s.fov
                SC.hideCrosshair = s.hideCrosshair
                SC.image:SetTexture(s.texturePath)
                buildReticles(index, s.lines)
                setScopeVisible(index, true)
            end
        end
        shownScope = index
    end

    if shownScope == nil then
        XOPS.Camera:SetFieldOfView(baseFov)
        return false
    end

    layoutScope()
    XOPS.Camera:SetFieldOfView(SC.fov)
    return SC.hideCrosshair
end

-- 캐시를 무효화해 다음 프레임에 체력/탄약/무기명을 강제로 다시 칠한다(표시모드 전환 시).
local function invalidateCache()
    lastHP = nil
    lastMag = nil
    lastRes = nil
    lastName = nil
end

-- 정상 HUD 값(체력/탄약/무기명)을 플레이어 상태로 갱신한다. 변한 값만 다시 칠한다.
local function updateNormalValues()
    local hp = XOPS.Player:GetHP()
    if hp ~= lastHP then
        local r, g, b = stateColor(hp)
        N.state:SetColor(r, g, b, 1)
        N.hp:SetColor(r, g, b, 1)
        N.hp:SetText(hpText(hp))
        N.hp:SetPosition(hpX(hp), N.hpY)
        lastHP = hp
    end

    local mag = XOPS.Player:GetMagazine()
    local res = XOPS.Player:GetReserveAmmo()
    if mag ~= lastMag or res ~= lastRes then
        N.ammo:SetText(AMMO_MAG_GLYPH .. mag .. " " .. AMMO_RES_GLYPH .. reserveText(res))
        lastMag = mag
        lastRes = res
    end

    local name = XOPS.Player:GetWeaponName()
    if name ~= lastName then
        N.weaponName:SetText(name)
        N.weaponName:SetFontSize(weaponFontW(name, 14), N.wnH)
        lastName = name
    end
end

-- 간이 HUD 값(테두리 색/무기명)을 플레이어 상태로 갱신한다.
local function updateSimpleValues()
    local hp = XOPS.Player:GetHP()
    if hp ~= lastHP then
        local r, g, b = stateColor(hp)
        for i = 1, #S.barHandles do S.barHandles[i]:SetColor(r, g, b, 1) end
        lastHP = hp
    end

    local name = XOPS.Player:GetWeaponName()
    if name ~= lastName then
        S.name:SetText(name)
        S.name:SetFontSize(weaponFontW(name, 10), S.nameH)
        lastName = name
    end
end

-- 벽 블라인드를 현재 카메라 상태에 맞춘다. 걸린 방향의 화면 절반을 검게 덮는다.
local function updateBlind()
    local b = XOPS.Camera:GetWallBlind()
    local on = { b.top, b.bottom, b.left, b.right }

    -- 하나라도 켜질 때만 크기를 맞춘다(창 크기가 바뀔 수 있어 그때그때 화면 절반을 다시 잰다).
    if on[1] or on[2] or on[3] or on[4] then
        local w = XOPS.UI:GetLayerWidth(BLIND_LAYER, SCALED) / 2
        local h = XOPS.UI:GetLayerHeight(BLIND_LAYER, SCALED) / 2
        for i = 1, #BLIND_BOXES do
            if BLIND_BOXES[i].half == "h" then
                BLIND.handles[i]:SetSize(0, h)
            else
                BLIND.handles[i]:SetSize(w, 0)
            end
        end
    end

    for i = 1, #BLIND_BOXES do
        if on[i] ~= BLIND.shown[i] then
            BLIND.shown[i] = on[i]
            BLIND.handles[i]:SetActive(on[i])
        end
    end
end

-- 들고 있는 무기를 계속 돌린다(메고 있는 무기는 고정이라 건드리지 않는다).
-- 커스텀 연출을 만들려면 여기서 위치/크기까지 매 프레임 바꿔도 된다.
local function updateWeaponView(dt)
    if WEAPON.main == nil then return end

    WEAPON.yaw = (WEAPON.yaw + WEAPON.spin * dt) % 360
    WEAPON.main:SetRotation(0, WEAPON.yaw, 0)
end

-- 0~1 범위로 자른다.
local function clamp01(v)
    if v < 0 then return 0 elseif v > 1 then return 1 else return v end
end

-- 종료 문구의 진하기(0~1) — 나타남 → 유지 → 사라짐. t: 판정 후 경과초.
-- 커스텀 연출을 만들 때 이 패턴(구간별 보간)을 그대로 쓰면 된다.
local function endTextAlpha(t)
    if t < END.inTime then
        return t / END.inTime
    elseif t < END.inTime + END.hold then
        return 1
    elseif t < END.inTime + END.hold + END.outTime then
        return 1 - (t - END.inTime - END.hold) / END.outTime
    end
    return 0
end

-- 판정이 난 순간 1회 — 성공/실패에 맞는 문구를 띄운다.
-- result: "complete" 또는 "failed"
local function startEnd(result)
    endT = 0

    local ok = result == "complete"
    END.handle:SetText(ok and END.successText or END.failText)
    END.handle:SetColor(
        ok and END.successR or END.failR,
        ok and END.successG or END.failG,
        ok and END.successB or END.failB, 0)
    END.handle:SetActive(true)
end

-- 미션이 처음부터 다시 시작될 때 — 종료 문구를 걷고 다시 검은 화면에서 밝아지게 되돌린다.
local function resetForStart()
    playT = 0
    endT = nil
    resultLoading = false
    inputLock = INPUT_LOCK
    END.handle:SetActive(false)
    FADE.handle:SetAlpha(1)
end

-- 암전과 종료 문구를 진행한다. 둘 다 끝나면 결과 화면으로 넘어간다.
local function updateFade(dt)
    local result = XOPS.Events:GetResult()
    if result == "inprogress" then
        playT = playT + dt
        FADE.handle:SetAlpha(1 - clamp01(playT / FADE.inTime))
        return
    end

    if endT == nil then startEnd(result) end
    endT = endT + dt
    FADE.handle:SetAlpha(clamp01(endT / FADE.outTime))
    END.handle:SetAlpha(endTextAlpha(endT))

    -- 암전과 문구 중 더 긴 쪽이 끝나면 결과 화면으로. 맵은 걷되 미션 데이터/통계는 남긴다.
    local total = FADE.outTime
    local textTotal = END.inTime + END.hold + END.outTime
    if textTotal > total then total = textTotal end
    if not resultLoading and endT >= total then
        resultLoading = true
        XOPS.Scene:UnloadMap()
        XOPS.Scene:Load(RESULT_SCENE)
    end
end

-- 이벤트 메시지를 현재 상태에 맞춘다. 글은 바뀔 때만 다시 쓰고, 진하기는 매 프레임 반영한다.
local function updateMessage()
    local id = XOPS.Events:GetMessageId()
    if id < 0 then
        if MSG.lastId ~= -1 then
            MSG.lastId = -1
            MSG.handle:SetAlpha(0)
        end
        return
    end

    if id ~= MSG.lastId then
        MSG.lastId = id
        MSG.handle:SetText(XOPS.Events:GetMessageText())
    end
    MSG.handle:SetAlpha(XOPS.Events:GetMessageAlpha())
end

-- 피격 번쩍을 갱신한다. 피격 표시는 확인하면 사라지므로 여기서만 소비한다.
local function updateFlash(dt)
    -- 조종 대상이 바뀌면 그 사람이 갖고 있던 피격 표시를 흘려버린다(바뀐 프레임에 잘못 번쩍이지 않게).
    local index = XOPS.Map:PlayerIndex()
    if index ~= FLASH.lastPlayer then
        FLASH.lastPlayer = index
        XOPS.Player:ConsumeHit()
    elseif XOPS.Player:ConsumeHit() and XOPS.Player:IsAlive() then
        FLASH.timer = FLASH.hold + FLASH.fade
    end

    local intensity = 0
    if FLASH.timer > 0 then
        FLASH.timer = FLASH.timer - dt
        if FLASH.timer < 0 then FLASH.timer = 0 end
        if FLASH.fade > 0 and FLASH.timer < FLASH.fade then
            intensity = FLASH.timer / FLASH.fade
        else
            intensity = 1
        end
    end

    local active = intensity > 0
    if active ~= FLASH.shown then
        FLASH.shown = active
        FLASH.handle:SetActive(active)
    end
    if active then FLASH.handle:SetAlpha(FLASH.a * intensity) end
end

-- 크로스헤어 설정(General)을 읽어 막대 크기/색/간격 규칙을 정한다. 진입 시 1회.
local function readCrosshairConfig()
    CH.gap = XOPS.Config:GetInt("General", "aimGap")
    CH.thick = XOPS.Config:GetInt("General", "aimThick")
    CH.length = XOPS.Config:GetInt("General", "aimLength")
    CH.static = XOPS.Config:GetBool("General", "StaticAim")
    CH.r = XOPS.Config:GetFloat("General", "aimColorR")
    CH.g = XOPS.Config:GetFloat("General", "aimColorG")
    CH.b = XOPS.Config:GetFloat("General", "aimColorB")
    CH.a = XOPS.Config:GetFloat("General", "aimColorA")
end

-- ----- 라이프사이클 -----
function M.start()
    XOPS.Input:SetMouseCursor(true, true, true)   -- 창 안에서 커서 숨김 + 화면 중앙 고정(1인칭 조준)
    XOPS.Input:RegisterButton(UIMODE_ACTION, UIMODE_BINDING)
    XOPS.Input:RegisterButton(VIEWMODE_ACTION, VIEWMODE_BINDING)
    XOPS.Input:RegisterButton(RESTART_ACTION, RESTART_BINDING)

    -- 설정에 값이 없으면 0이 오므로, 유효할 때만 받아 기본 시야각을 지킨다(0이면 화면이 깨진다).
    local fov = XOPS.Config:GetInt("Graphic", "fov")
    if fov > 0 then baseFov = fov end

    for i = 1, #FRAME_LINES do
        local f = FRAME_LINES[i]
        frames[i] = XOPS.UI:CreateText(FRAME_LAYER, SCALING, f.pivot, f.align, glyphString(f.codes),
            f.x, f.y, FRAME_FONT.w, FRAME_FONT.h, 0, FRAME_COLOR.r, FRAME_COLOR.g, FRAME_COLOR.b, FRAME_COLOR.a)
    end

    -- 정상 HUD 텍스트(값은 update에서 채움)
    N.state = XOPS.UI:CreateText(HUD_LAYER, SCALING, "BottomLeft", "BottomLeft", "STATE",
        N.stateX, N.stateY, N.stateW, N.stateH, 0, 0, 1, 0, 1)
    N.hp = XOPS.UI:CreateText(HUD_LAYER, SCALING, "BottomLeft", "BottomLeft", "",
        hpX(100), N.hpY, N.hpW, N.hpH, 0, 0, 1, 0, 1)
    N.ammo = XOPS.UI:CreateText(HUD_LAYER, SCALING, "BottomLeft", "BottomLeft", "",
        N.ammoX, N.ammoY, N.ammoW, N.ammoH, 0, 1, 1, 1, 1)
    -- 무기명은 우측 가장자리 기준(BottomRight) + 좌측정렬 → 화면비가 넓어져도 우측에 붙는다.
    N.weaponName = XOPS.UI:CreateText(HUD_LAYER, SCALING, "BottomRight", "BottomLeft", "",
        N.wnX, N.wnY, N.wnW, N.wnH, 0, 1, 1, 1, 1)

    -- 무기 뷰(우하단). 표시는 applyMode 가 결정.
    WEAPON.handle = XOPS.UI:CreateWeaponView(WEAPON_LAYER, SCALING, "BottomRight", WEAPON.x, WEAPON.y, WEAPON.w, WEAPON.h)

    -- 무기 두 자리를 놓는다. 들고 있는 쪽 회전은 update 가 매 프레임 굴린다.
    WEAPON.main = XOPS.UI:GetWeaponViewSlot("main")
    if WEAPON.main then
        WEAPON.main:SetPosition(WEAPON.mainX, WEAPON.mainY, WEAPON.mainZ)
        WEAPON.main:SetScale(WEAPON.mainScale)
    end
    WEAPON.sub = XOPS.UI:GetWeaponViewSlot("sub")
    if WEAPON.sub then
        WEAPON.sub:SetPosition(WEAPON.subX, WEAPON.subY, WEAPON.subZ)
        WEAPON.sub:SetScale(WEAPON.subScale)
        WEAPON.sub:SetRotation(0, WEAPON.subYaw, 0)
    end

    -- 무기를 비추는 카메라 구도
    WEAPON.cam = XOPS.UI:GetWeaponViewCamera()
    if WEAPON.cam then
        WEAPON.cam:SetPosition(WEAPON.camX, WEAPON.camY, WEAPON.camZ)
        WEAPON.cam:SetRotation(WEAPON.camPitch, WEAPON.camYaw, WEAPON.camRoll)
        WEAPON.cam:SetFieldOfView(WEAPON.camFov)
        WEAPON.cam:SetBackgroundColor(WEAPON.camBg.r, WEAPON.camBg.g, WEAPON.camBg.b, WEAPON.camBg.a)
    end

    -- 재장전/교체(화면 중앙, 그림자+본문). 표시는 update에서 플레이어 상태로 토글.
    N.reloadShadow = XOPS.UI:CreateText(CENTER_LAYER, SCALED, "Center", "Center", "RELOADING",
        N.reloadShadowX, N.reloadShadowY, N.centerFontW, N.centerFontH, 0, N.shadow.r, N.shadow.g, N.shadow.b, 1)
    N.reloadMain = XOPS.UI:CreateText(CENTER_LAYER, SCALED, "Center", "Center", "RELOADING",
        N.reloadMainX, N.reloadMainY, N.centerFontW, N.centerFontH, 0, 1, 1, 1, 1)
    N.changeShadow = XOPS.UI:CreateText(CENTER_LAYER, SCALED, "Center", "Center", "CHANGING",
        N.reloadShadowX, N.reloadShadowY, N.centerFontW, N.centerFontH, 0, N.shadow.r, N.shadow.g, N.shadow.b, 1)
    N.changeMain = XOPS.UI:CreateText(CENTER_LAYER, SCALED, "Center", "Center", "CHANGING",
        N.reloadMainX, N.reloadMainY, N.centerFontW, N.centerFontH, 0, 1, 1, 1, 1)
    N.reloadShadow:SetActive(false)
    N.reloadMain:SetActive(false)
    N.changeShadow:SetActive(false)
    N.changeMain:SetActive(false)

    -- 간이 HUD: 체력 테두리 4바 + 명판 + 무기명
    for i = 1, #S.bars do
        local b = S.bars[i]
        S.barHandles[i] = XOPS.UI:CreateImage(SIMPLE_LAYER, SCALING, b.pivot, "", b.x, b.y, b.w, b.h, 0, 1, 0, 1)
    end
    S.panel = XOPS.UI:CreateImage(SIMPLE_LAYER, SCALING, "BottomLeft", "", S.panelX, S.panelY, S.panelW, S.panelH,
        S.panelColor.r, S.panelColor.g, S.panelColor.b, S.panelColor.a)
    S.name = XOPS.UI:CreateText(SIMPLE_LAYER, SCALING, "BottomLeft", "BottomLeft", "",
        S.nameX, S.nameY, S.nameW, S.nameH, 0, 1, 1, 1, 1)

    -- 스코프: 그림 + 바깥 검정 바 4개(조준선은 그 스코프를 처음 볼 때 생성). 표시는 update가 결정.
    SC.image = XOPS.UI:CreateImage(SCOPE_LAYER, SCALED, "Center", "", 0, 0, 0, 0, 1, 1, 1, 1)
    for i = 1, 4 do
        SC.bars[i] = XOPS.UI:CreateImage(SCOPE_LAYER, SCALED, "Center", "", 0, 0, 0, 0, 0, 0, 0, 1)
    end
    setScopeVisible(-1, false)

    -- 크로스헤어: 가로 막대(길이×두께) 2개 + 세로 막대(두께×길이) 2개
    readCrosshairConfig()
    CH.left = XOPS.UI:CreateImage(CROSSHAIR_LAYER, SCALING, "Center", "", 0, 0, CH.length, CH.thick, CH.r, CH.g, CH.b, CH.a)
    CH.right = XOPS.UI:CreateImage(CROSSHAIR_LAYER, SCALING, "Center", "", 0, 0, CH.length, CH.thick, CH.r, CH.g, CH.b, CH.a)
    CH.up = XOPS.UI:CreateImage(CROSSHAIR_LAYER, SCALING, "Center", "", 0, 0, CH.thick, CH.length, CH.r, CH.g, CH.b, CH.a)
    CH.down = XOPS.UI:CreateImage(CROSSHAIR_LAYER, SCALING, "Center", "", 0, 0, CH.thick, CH.length, CH.r, CH.g, CH.b, CH.a)
    spreadCrosshair(CH.gap)
    setCrosshairVisible(false)

    -- 이벤트 메시지: 화면 하단 전폭 상자 안에 가운데 정렬. 진하기 0으로 시작(update가 켠다).
    MSG.handle = XOPS.UI:CreateOSText(MESSAGE_LAYER, SCALED, "StretchBottom", "TopCenter", "",
        0, 0, MSG.fontSize, MSG.r, MSG.g, MSG.b, 0)
    MSG.handle:SetSize(0, MSG.boxH)
    MSG.handle:SetWrappingMode("nowrap")

    -- 피격 번쩍: 화면 전체 빨강. 표시는 update가 결정.
    FLASH.handle = XOPS.UI:CreateImage(FLASH_LAYER, SCALING, "StretchFull", "", 0, 0, 0, 0,
        FLASH.r, FLASH.g, FLASH.b, FLASH.a)
    FLASH.handle:SetActive(false)

    fpsText = XOPS.UI:CreateText(FPS_LAYER, SCALING, "TopRight", "TopRight", "",
        FPS.x, FPS.y, FPS.w, FPS.h, 0, FPS.r, FPS.g, FPS.b, 1)
    fpsShown = XOPS.Config:GetBool("General", "ShowFPS")
    fpsText:SetActive(fpsShown)

    -- 벽 블라인드 4방향(표시는 update가 결정)
    for i = 1, #BLIND_BOXES do
        BLIND.handles[i] = XOPS.UI:CreateImage(BLIND_LAYER, SCALED, BLIND_BOXES[i].pivot, "", 0, 0, 0, 0, 0, 0, 0, 1)
        BLIND.handles[i]:SetActive(false)
    end

    -- 화면 암전(검은 화면에서 시작) + 미션 종료 문구(update가 띄운다)
    FADE.handle = XOPS.UI:CreateImage(FADE_LAYER, SCALED, "StretchFull", "", 0, 0, 0, 0, 0, 0, 0, 1)
    END.handle = XOPS.UI:CreateText(ENDING_LAYER, SCALED, "Center", "Center", "",
        0, 0, END.fontW, END.fontH, 0, 1, 1, 1, 0)
    END.handle:SetActive(false)

    applyMode()
    applyUIScale()
end

-- FPS 표시 여부를 config에서 읽어 변할 때만 토글한다. 반환: 현재 표시 여부.
local function refreshFpsVisible()
    local show = XOPS.Config:GetBool("General", "ShowFPS")
    if show ~= fpsShown then
        fpsShown = show
        fpsText:SetActive(show)
    end
    return show
end

-- FPS 텍스트를 interval 초 평균으로 갱신한다.
local function updateFps(dt)
    if not refreshFpsVisible() then return end
    fpsAccum = fpsAccum + dt
    fpsFrames = fpsFrames + 1
    if fpsAccum >= FPS.interval then
        local avgFrame = fpsAccum / fpsFrames
        fpsText:SetText(string.format("%d FPS (%d ms)", math.floor(1 / avgFrame + 0.5), math.floor(avgFrame * 1000 + 0.5)))
        fpsAccum = 0
        fpsFrames = 0
    end
end

-- 나가기/재시작/시점전환 입력을 처리한다. 반환: 화면을 떠났으면 true(이후 처리 중단).
local function updateInput(dt)
    if inputLock > 0 then inputLock = inputLock - dt end

    -- F1 시점 전환 — 잠금과 무관. 죽었거나 플레이어가 없으면 무시된다(사망 카메라 유지).
    if XOPS.Input:WasPressed(VIEWMODE_ACTION) then
        XOPS.Player:ToggleViewMode()
    end

    if inputLock > 0 then return false end

    if XOPS.Input:WasPressed("escape") then
        XOPS.Scene:UnloadMission()
        XOPS.Scene:Load(MAINMENU_SCENE)
        return true
    end

    if XOPS.Input:WasPressed(RESTART_ACTION) then
        XOPS.Scene:RestartMission()   -- 화면은 그대로 두고 맵만 되돌린다(입력잠금은 미션 시작 감지가 다시 건다).
    end
    return false
end

function M.update(t, dt)
    -- 입력을 먼저 본다 — 재시작이 걸리면 그 즉시 아래에서 감지돼 같은 프레임에 화면이 검어진다
    -- (나중에 보면 되돌린 맵이 한 프레임 밝게 노출된다).
    if updateInput(dt) then return end

    -- 미션이 (재)시작되면 연출과 입력잠금을 처음 상태로 되돌린다.
    local start = XOPS.Events:GetMissionStartCount()
    if start ~= lastStart then
        lastStart = start
        resetForStart()
    end

    -- F2 표시모드 순환
    if XOPS.Input:WasPressed(UIMODE_ACTION) then
        local i = 1
        for k = 1, #MODE_ORDER do
            if MODE_ORDER[k] == mode then i = k end
        end
        mode = MODE_ORDER[(i % #MODE_ORDER) + 1]
        applyMode()
        invalidateCache()
    end

    -- 스코프 토글. 켜고 끄는 판단·자동 해제는 엔진이 하므로 입력만 넘긴다.
    if XOPS.Player:Exists() and XOPS.Input:WasPressed(SCOPE_ACTION) then
        XOPS.Player:ToggleScope()
    end

    -- 스코프를 먼저 반영하고(크로스헤어를 대신할 수 있으므로), 그 결과로 크로스헤어 표시를 정한다.
    local hiddenByScope = applyScope()
    local showCross = XOPS.Player:IsFirstPerson() and XOPS.Player:ShowsCrosshair() and not hiddenByScope
    setCrosshairVisible(showCross)
    if showCross then
        local gap = CH.static and CH.gap or (CH.gap + XOPS.Player:GetErrorRange())
        if gap ~= CH.lastGap then
            CH.lastGap = gap
            spreadCrosshair(gap)
        end
    end

    -- 플레이어가 있을 때만 값 갱신(스폰 전/사망 후엔 마지막 표시를 그대로 유지).
    if XOPS.Player:Exists() then
        if mode == "normal" then updateNormalValues()
        elseif mode == "simple" then updateSimpleValues() end

        local reloading = XOPS.Player:IsReloading()
        N.reloadShadow:SetActive(reloading)
        N.reloadMain:SetActive(reloading)
        local changing = XOPS.Player:IsSwitchingWeapon()
        N.changeShadow:SetActive(changing)
        N.changeMain:SetActive(changing)
    end

    updateWeaponView(dt)
    updateBlind()
    updateMessage()
    updateFlash(dt)
    updateFade(dt)
    updateFps(dt)
end

return M
