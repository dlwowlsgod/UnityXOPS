-- ============================================================
--  브리핑 씬 스크립트 (briefing.lua)
--  미션 시작 전 이미지/미션명/브리핑 본문을 보여주고, 좌클릭이면 미션 시작, ESC면 미션 포기.
-- ============================================================

local M = {}

-- ----- 레이어 (숫자 클수록 위에 그림) -----
local BG_LAYER      = 1001   -- 검은 배경 + 옅게 깔리는 타이틀
local CONTENT_LAYER = 1002   -- 미션 이미지 / BRIEFING / 미션명 / 본문
local CLICK_LAYER   = 1003   -- 클릭 유도 문구

-- 레이어 스케일 모드. true = 640x480 기준으로 화면 높이에 맞춰 확대, false = 640x480 픽셀 1:1 고정.
-- 한 레이어의 값은 최초 생성 시 결정되고 이후 호출에서는 무시된다.
local SCALING       = true    -- 배경 / 컨텐츠
local CLICK_SCALING = false   -- 클릭 유도 문구만 화면 크기와 무관하게 고정 크기

-- UIScale(설정) 배수를 적용할 레이어들. 화면 높이에 맞춰 늘어나는 레이어(SCALING=true)에는 걸어도 무시된다.
local UI_SCALE_LAYERS = { CLICK_LAYER }

-- ----- 전환할 씬 -----
local MAINMENU_SCENE   = 2     -- ESC: 미션 포기
local MAINGAME_SCENE   = 4     -- 좌클릭: 미션 시작
local INPUT_ALLOW_TIME = 0.2   -- 진입 후 이 시간(초)이 지나야 입력 인정(직전 씬 입력 누수 방지)

-- ----- 배경 -----
local BACKDROP = { r = 0, g = 0, b = 0, a = 1 }         -- 화면 전체를 덮는 단색(0~1)
local TITLE    = { path = "data/title.dds", a = 0.012 } -- 배경에 옅게 깔리는 타이틀(0~1 알파)

-- ----- 미션 이미지 -----
-- 화면 왼쪽 중앙 기준 오프셋(+x 오른쪽, +y 위)과 크기(px).
-- 미션 이미지가 1장이면 single 자리에, 2장이면 first/second 자리에 놓인다.
local IMAGE = {
    w = 160, h = 150,
    single = { x = 40, y = -15 },
    first  = { x = 40, y = 35 },
    second = { x = 40, y = -135 },
}

-- ----- BRIEFING 제목 (화면 위 중앙 기준 오프셋) -----
-- 알파가 alphaFrom → alphaTo 로 duration 초에 걸쳐 변하고 곧바로 처음부터 반복된다(깜빡임).
local HEADING = {
    text = "BRIEFING",
    x = 0, y = -30, w = 60, h = 42,
    r = 1, g = 1, b = 0,
    duration = 0.7, alphaFrom = 1.0, alphaTo = 0.1585,
}

-- ----- 미션 이름 (BRIEFING 아래) -----
local FULLNAME = {
    x = 0, y = -90, w = 18, h = 25,
    r = 1, g = 0.5, b = 0,
}

-- ----- 브리핑 본문 (OS 폰트 — 가독성용) -----
local BODY = {
    x = 230, y = -175,   -- 화면 좌상단 기준 오프셋
    w = 370, h = 600,    -- 글상자 크기(이 폭에서 자동 줄바꿈)
    size = 16,           -- 글자 크기(pt)
    lineSpacing = -20,   -- 줄 간격(음수면 좁게)
    r = 1, g = 1, b = 1, a = 1,
}

-- ----- 클릭 유도 문구 (화면 오른쪽 아래 기준 오프셋) -----
-- 같은 문구를 두 겹 놓는다. 아래 겹은 w,h 로 고정하고 위 겹만 pulseW,pulseH → endW,endH 로
-- 커지며 흐려지길 반복해 번지는 느낌을 낸다.
local CLICK = {
    text = "LEFT CLICK TO BEGIN",
    x = -220, y = 35,
    w = 18, h = 26,               -- 아래 겹(고정) 글자 크기
    pulseW = 18, pulseH = 24,     -- 위 겹 시작 글자 크기
    endW = 26, endH = 58,         -- 위 겹 끝 글자 크기
    r = 1, g = 1, b = 1,
    duration = 1.0, alphaFrom = 1.0, alphaTo = 0.142,
}

-- ----- 런타임 상태 (자동 관리 — 보통 건드릴 필요 없음) -----
local headingText = nil      -- 깜빡이는 BRIEFING
local clickPulseText = nil   -- 번지는 클릭 유도 문구(위쪽 겹)
local finished = false       -- 씬 전환 1회 가드

-- a에서 b까지 진행도 p(0~1)에 해당하는 값을 돌려준다.
local function lerp(a, b, p)
    return a + (b - a) * p
end

-- duration 초를 주기로 0→1을 반복하는 진행도를 돌려준다. 반복 연출(깜빡임/번짐)의 기준값.
-- t: 씬 시작 후 경과초, duration: 한 주기의 길이(초)
local function cycle(t, duration)
    return (t % duration) / duration
end

-- config General.UIScale 값을 위 레이어들에 배수로 적용한다(1=기본, 2/3/4=확대).
local function applyUIScale()
    local s = XOPS.Config:GetFloat("General", "UIScale")
    for i = 1, #UI_SCALE_LAYERS do
        XOPS.UI:SetScaleFactor(UI_SCALE_LAYERS[i], s)
    end
end

-- 미션 이미지 1장을 지정 자리에 생성한다. 경로가 비어 있으면 만들지 않는다.
-- path: 이미지 경로, slot: 배치 자리({x, y})
local function createMissionImage(path, slot)
    if path == "" then return end
    XOPS.UI:CreateImage(CONTENT_LAYER, SCALING, "MiddleLeft", path, slot.x, slot.y, IMAGE.w, IMAGE.h, 1, 1, 1, 1)
end

-- 미션을 포기하고 메인메뉴로 돌아간다. 로드된 맵/미션 데이터를 함께 해제한다.
local function abort()
    if finished then return end
    finished = true
    XOPS.Scene:UnloadMission()
    XOPS.Scene:Load(MAINMENU_SCENE)
end

-- 미션을 시작한다. 맵은 해제하지 않고 그대로 메인게임으로 넘긴다.
local function begin()
    if finished then return end
    finished = true
    XOPS.Scene:Load(MAINGAME_SCENE)
end

-- ----- 라이프사이클 -----
function M.start()
    XOPS.Input:SetMouseCursor(true, false, false)   -- 창 안에서 커서 숨김, 자유 이동

    XOPS.UI:CreateImage(BG_LAYER, SCALING, "StretchFull", "", 0, 0, 0, 0, BACKDROP.r, BACKDROP.g, BACKDROP.b, BACKDROP.a)
    XOPS.UI:CreateImage(BG_LAYER, SCALING, "StretchFull", TITLE.path, 0, 0, 0, 0, 1, 1, 1, TITLE.a)

    local image0 = XOPS.Data:GetMissionImage(0)
    local image1 = XOPS.Data:GetMissionImage(1)
    if image1 ~= "" then
        createMissionImage(image0, IMAGE.first)
        createMissionImage(image1, IMAGE.second)
    else
        createMissionImage(image0, IMAGE.single)
    end

    headingText = XOPS.UI:CreateText(CONTENT_LAYER, SCALING, "TopCenter", "TopCenter", HEADING.text,
        HEADING.x, HEADING.y, HEADING.w, HEADING.h, 0, HEADING.r, HEADING.g, HEADING.b, HEADING.alphaFrom)

    XOPS.UI:CreateText(CONTENT_LAYER, SCALING, "TopCenter", "TopCenter", XOPS.Data:GetMissionFullname(),
        FULLNAME.x, FULLNAME.y, FULLNAME.w, FULLNAME.h, 0, FULLNAME.r, FULLNAME.g, FULLNAME.b, 1)

    local body = XOPS.UI:CreateOSText(CONTENT_LAYER, SCALING, "TopLeft", "TopLeft", XOPS.Data:GetMissionBriefing(),
        BODY.x, BODY.y, BODY.size, BODY.r, BODY.g, BODY.b, BODY.a)
    body:SetSize(BODY.w, BODY.h)
    body:SetWrappingMode("normal")
    body:SetLineSpacing(BODY.lineSpacing)

    -- 두 겹 모두 rect를 점(0,0)으로 줄여 기준점 정중앙에 글자를 놓는다. 그래야 위쪽 겹이 커질 때 사방으로 번진다.
    local clickBase = XOPS.UI:CreateText(CLICK_LAYER, CLICK_SCALING, "BottomRight", "Center", CLICK.text,
        CLICK.x, CLICK.y, CLICK.w, CLICK.h, 0, CLICK.r, CLICK.g, CLICK.b, 1)
    clickBase:SetSize(0, 0)

    clickPulseText = XOPS.UI:CreateText(CLICK_LAYER, CLICK_SCALING, "BottomRight", "Center", CLICK.text,
        CLICK.x, CLICK.y, CLICK.pulseW, CLICK.pulseH, 0, CLICK.r, CLICK.g, CLICK.b, CLICK.alphaFrom)
    clickPulseText:SetSize(0, 0)

    applyUIScale()
end

function M.update(t, dt)
    local blink = cycle(t, HEADING.duration)
    headingText:SetAlpha(lerp(HEADING.alphaFrom, HEADING.alphaTo, blink))

    local spread = cycle(t, CLICK.duration)
    clickPulseText:SetAlpha(lerp(CLICK.alphaFrom, CLICK.alphaTo, spread))
    clickPulseText:SetFontSize(lerp(CLICK.pulseW, CLICK.endW, spread), lerp(CLICK.pulseH, CLICK.endH, spread))

    if t < INPUT_ALLOW_TIME then return end

    if XOPS.Input:WasPressed("escape") then
        abort()
    elseif XOPS.Input:WasPressed("fire") then
        begin()
    end
end

return M
