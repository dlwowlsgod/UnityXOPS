-- ============================================================
--  결과 씬 스크립트 (result.lua)
--  미션 성공/실패와 플레이어 통계를 보여준다. F12=같은 미션 재시작, ESC/좌클릭=메인메뉴.
-- ============================================================

local M = {}

-- ----- 레이어 (숫자 클수록 위에 그림) -----
local BG_LAYER      = 1001   -- 검은 배경 + 옅게 깔리는 타이틀
local CONTENT_LAYER = 1002   -- RESULT 제목 / 미션명 / 판정 / 통계

-- 640x480 기준으로 화면 높이에 맞춰 확대(4:3→16:9에서도 비율 유지). 한 레이어 값은 최초 생성 시 고정.
local SCALING = true

-- ----- 전환할 씬 -----
local MAINMENU_SCENE   = 2     -- ESC/좌클릭: 메인메뉴
local INPUT_ALLOW_TIME = 0.2   -- 진입 후 이 시간(초)이 지나야 입력 인정(직전 씬 입력 누수 방지)

-- ----- 재시작 입력 (F12) -----
local RESTART_ACTION  = "restart"
local RESTART_BINDING = "<Keyboard>/f12"

-- ----- 배경 -----
local BACKDROP = { r = 0, g = 0, b = 0, a = 1 }         -- 화면 전체를 덮는 단색(0~1)
local TITLE    = { path = "data/title.dds", a = 0.012 } -- 배경에 옅게 깔리는 타이틀(0~1 알파)

-- ----- RESULT 제목 (화면 위 중앙 기준 오프셋) -----
-- 알파가 alphaFrom → alphaTo 로 duration 초에 걸쳐 변하고 곧바로 처음부터 반복된다(깜빡임).
local HEADING = {
    text = "RESULT",
    y = -30, w = 50, h = 42,
    r = 1, g = 0, b = 1,
    duration = 0.7, alphaFrom = 1.0, alphaTo = 0.1585,
}

-- ----- 미션 이름 (RESULT 아래) -----
local FULLNAME = { y = -90, w = 18, h = 25, r = 0.502, g = 0.502, b = 1 }

-- ----- 미션 판정 (성공/실패) -----
-- 성공이면 successText/successColor, 실패면 failText/failColor 로 표시.
local VERDICT = {
    y = -140, w = 24, h = 32,
    successText = "mission successful", successR = 0, successG = 1, successB = 0,
    failText    = "mission failure",    failR = 1, failG = 0, failB = 0,
}

-- ----- 통계 줄 (판정 아래로 일정 간격 나열) -----
-- top: 첫 줄 Y 오프셋, pitch: 줄 간격(음수면 아래로), w/h: 글자 크기, r/g/b: 색(0~1).
local INFO = { top = -200, pitch = -50, w = 20, h = 32, r = 1, g = 1, b = 1 }

-- ----- 런타임 상태 (자동 관리 — 보통 건드릴 필요 없음) -----
local headingText = nil   -- 깜빡이는 RESULT
local finished = false    -- 씬 전환 1회 가드

-- a에서 b까지 진행도 p(0~1)에 해당하는 값을 돌려준다.
local function lerp(a, b, p)
    return a + (b - a) * p
end

-- duration 초를 주기로 0→1을 반복하는 진행도를 돌려준다. 반복 연출(깜빡임)의 기준값.
-- t: 씬 시작 후 경과초, duration: 한 주기의 길이(초)
local function cycle(t, duration)
    return (t % duration) / duration
end

-- 통계 테이블을 화면에 표시할 줄 문자열 목록으로 만든다.
-- s: XOPS.Data:GetMissionStats() 결과, 반환: 위→아래 순서의 문자열 배열
local function statLines(s)
    local total = math.floor(s.playTime)
    local min = math.floor(total / 60)
    local sec = total % 60
    return {
        "Time  " .. min .. "min " .. sec .. "sec",
        "Rounds fired  " .. s.fire,
        "Rounds on target  " .. s.onTarget,
        "Accuracy rate  " .. string.format("%.1f", s.accuracy) .. "%",
        "Kill  " .. s.kill .. " / HeadShot  " .. s.headshot,
    }
end

-- 맵·미션 데이터를 해제하고 메인메뉴로 돌아간다.
local function returnToMenu()
    if finished then return end
    finished = true
    XOPS.Scene:UnloadMission()
    XOPS.Scene:Load(MAINMENU_SCENE)
end

-- 같은 미션을 다시 시작한다(맵 재로드 → 메인게임). 맵 해제는 하지 않는다.
local function restart()
    if finished then return end
    finished = true
    XOPS.Scene:RestartMission()
end

-- ----- 라이프사이클 -----
function M.start()
    XOPS.Input:SetMouseCursor(true, false, false)      -- 창 안에서 커서 숨김, 자유 이동
    XOPS.Input:RegisterButton(RESTART_ACTION, RESTART_BINDING)

    XOPS.UI:CreateImage(BG_LAYER, SCALING, "StretchFull", "", 0, 0, 0, 0, BACKDROP.r, BACKDROP.g, BACKDROP.b, BACKDROP.a)
    XOPS.UI:CreateImage(BG_LAYER, SCALING, "StretchFull", TITLE.path, 0, 0, 0, 0, 1, 1, 1, TITLE.a)

    headingText = XOPS.UI:CreateText(CONTENT_LAYER, SCALING, "TopCenter", "TopCenter", HEADING.text,
        0, HEADING.y, HEADING.w, HEADING.h, 0, HEADING.r, HEADING.g, HEADING.b, HEADING.alphaFrom)

    XOPS.UI:CreateText(CONTENT_LAYER, SCALING, "TopCenter", "TopCenter", XOPS.Data:GetMissionFullname(),
        0, FULLNAME.y, FULLNAME.w, FULLNAME.h, 0, FULLNAME.r, FULLNAME.g, FULLNAME.b, 1)

    local complete = XOPS.Events:GetResult() == "complete"
    local verdictText = complete and VERDICT.successText or VERDICT.failText
    local vr = complete and VERDICT.successR or VERDICT.failR
    local vg = complete and VERDICT.successG or VERDICT.failG
    local vb = complete and VERDICT.successB or VERDICT.failB
    XOPS.UI:CreateText(CONTENT_LAYER, SCALING, "TopCenter", "TopCenter", verdictText,
        0, VERDICT.y, VERDICT.w, VERDICT.h, 0, vr, vg, vb, 1)

    local lines = statLines(XOPS.Data:GetMissionStats())
    for i = 1, #lines do
        local y = INFO.top + INFO.pitch * (i - 1)
        XOPS.UI:CreateText(CONTENT_LAYER, SCALING, "TopCenter", "TopCenter", lines[i],
            0, y, INFO.w, INFO.h, 0, INFO.r, INFO.g, INFO.b, 1)
    end
end

function M.update(t, dt)
    local blink = cycle(t, HEADING.duration)
    headingText:SetAlpha(lerp(HEADING.alphaFrom, HEADING.alphaTo, blink))

    if t < INPUT_ALLOW_TIME then return end

    if XOPS.Input:WasPressed(RESTART_ACTION) then
        restart()
    elseif XOPS.Input:WasPressed("escape") or XOPS.Input:WasPressed("fire") then
        returnToMenu()
    end
end

return M
