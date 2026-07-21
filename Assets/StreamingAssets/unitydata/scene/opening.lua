-- ============================================================
--  오프닝 씬 연출 스크립트 (opening.lua)
-- ============================================================

local M = {}

-- ----- 레이어 (숫자 클수록 위에 그림) -----
local FADE_LAYER          = 1000   -- 화면 페이드(맨 아래)
local TEXT_LAYER          = 1010   -- 크레딧 텍스트
local LETTERBOX_LAYER     = 1020   -- 위/아래 검은 막대(맨 위)

-- ----- 레터박스 (위아래 검은 막대) -----
local LETTERBOX_RATIO     = 0.12                   -- 화면 높이 대비 막대 비율
local LETTERBOX_THICKNESS = LETTERBOX_RATIO * 480  -- 막대 두께(px)

-- ----- 화면 페이드 타이밍 (초) -----
local FADE_IN_DURATION  = 2.0         -- 시작 후 검정→투명에 걸리는 시간
local FADE_OUT_START    = 11.0        -- 이 시각부터 투명→검정 시작
local FADE_OUT_DURATION = 4.0         -- 페이드 아웃에 걸리는 시간
local END_TIME          = 15.0 + 1.1  -- 이 시각에 자동으로 메뉴로 넘어감

-- ----- 카메라 시작 상태 -----
local CAM_FOV   = 65.0                               -- 시야각(도)
local CAM_POS   = { x = 0.5,   y = 5.8,  z = -2.9 }  -- 시작 위치
local CAM_EULER = { x = -12.0, y = 64.0, z = 0.0 }   -- 시작 각도(피치/요/롤)

-- ----- 카메라 드리프트 (천천히 밀리며 도는 연출) -----
-- accelStart/End: 가속 구간(초). constantEnd: 등속 종료 시각(<0이면 무한 등속). tx/ty/tz: 목표 속도. smooth: 감쇠 계수(1에 가까울수록 느림).
local POS_ANIM = { accelStart = 4.0, accelEnd = 5.0, constantEnd = -1.0,
                   tx = 0.0, ty = -0.1667, tz = -0.2667, smooth = 0.8 }
local ROT_ANIM = { accelStart = 2.6, accelEnd = 3.6, constantEnd = 5.0,
                   tx = 20.20, ty = -30.0, tz = 0.0, smooth = 0.8 }

-- ----- 크레딧 텍스트 목록 -----
-- 한 줄 = 한 텍스트. text: 문자열 / pivot: 화면 기준점(TopLeft·Center·BottomCenter 등) / x,y: 기준점에서의 오프셋(+x 오른쪽, +y 위) /
--   w,h: 글자 크기 / r,g,b: 색(0~1) / inS,inE: 나타나는 구간(초, 페이드 인 시작·끝) / outS,outE: 사라지는 구간(페이드 아웃 시작·끝) /
--   page: 쓸 스프라이트 시트 번호(생략하면 0번). 시트는 font.lua에서 등록한다.
local TEXTS = {
    { text = "Project UnityXOPS", pivot = "BottomCenter", x = 0,    y = 120,  w = 22, h = 22, r = 1, g = 1, b = 1,
      inS = 0.5,  inE = 1.5,  outS = 3.0,  outE = 4.0 },
    { text = "Original by",       pivot = "TopLeft",      x = 60,   y = -120, w = 20, h = 20, r = 1, g = 1, b = 1,
      inS = 4.5,  inE = 5.5,  outS = 7.5,  outE = 8.5 },
    { text = "nine-two",          pivot = "TopLeft",      x = 100,  y = -150, w = 20, h = 20, r = 1, g = 1, b = 1,
      inS = 5.0,  inE = 6.0,  outS = 8.0,  outE = 9.0 },
    { text = "TENNKUU",           pivot = "TopLeft",      x = 100,  y = -180, w = 20, h = 20, r = 1, g = 1, b = 1,
      inS = 5.0,  inE = 6.0,  outS = 8.0,  outE = 9.0 },
    { text = "UnityXOPS by",      pivot = "TopRight",     x = -100, y = -270, w = 20, h = 20, r = 1, g = 1, b = 1,
      inS = 7.0,  inE = 8.0,  outS = 10.0, outE = 11.0 },
    { text = "JayTwoGames",       pivot = "TopRight",     x = -70,  y = -300, w = 20, h = 20, r = 1, g = 1, b = 1,
      inS = 7.5,  inE = 8.5,  outS = 10.5, outE = 11.5 },
    { text = "JJL",               pivot = "TopRight",     x = -70,  y = -330, w = 20, h = 20, r = 1, g = 1, b = 1,
      inS = 7.5,  inE = 8.5,  outS = 10.5, outE = 11.5 },
    { text = "dlwowlsgod",        pivot = "TopRight",     x = -70,  y = -360, w = 20, h = 20, r = 1, g = 1, b = 1,
      inS = 7.5,  inE = 8.5,  outS = 10.5, outE = 11.5 },
    { text = "X OPERATIONS",      pivot = "Center",       x = 0,    y = 0,    w = 22, h = 22, r = 1, g = 0, b = 0,
      inS = 12.0, inE = 13.0, outS = 15.0, outE = 16.0 },
}

-- ----- 런타임 상태 (자동 관리 — 보통 건드릴 필요 없음) -----
local pos = { x = 0, y = 0, z = 0 }   -- 카메라 이동 속도 누적
local rot = { x = 0, y = 0, z = 0 }   -- 카메라 회전 속도 누적
local textHandles = {}                -- 생성된 텍스트 핸들
local fadeHandle = nil                -- 페이드 이미지 핸들
local finished = false                -- 종료 1회 가드

-- ----- 헬퍼 -----
local function clamp01(v)
    if v < 0 then return 0 elseif v > 1 then return 1 else return v end
end

-- 나타났다 사라지는 알파를 돌려준다(0~1). update에서 각 텍스트에 SetAlpha로 적용.
-- 커스텀 효과(펄스 등)도 이렇게 local 함수로 만들어 update에서 호출하면 된다.
-- t: 경과초, inS/inE: 페이드 인 시작/끝, outS/outE: 페이드 아웃 시작/끝
local function fadeAlpha(t, inS, inE, outS, outE)
    if t < inS then return 0
    elseif t < inE then return (t - inS) / (inE - inS)
    elseif t < outS then return 1
    elseif t < outE then return 1 - (t - outS) / (outE - outS)
    else return 0 end
end

-- 화면 페이드 알파(0 투명 ~ 1 검정): 0~FADE_IN_DURATION 동안 1→0(인), 중간 0, FADE_OUT 구간 0→1(아웃).
local function fadeValue(t)
    if t < FADE_IN_DURATION then
        return 1 - t / FADE_IN_DURATION
    elseif t < FADE_OUT_START then
        return 0
    elseif t < FADE_OUT_START + FADE_OUT_DURATION then
        return (t - FADE_OUT_START) / FADE_OUT_DURATION
    else
        return 1
    end
end

-- 속도 벡터 v를 anim 설정에 따라 갱신한다(가속→등속→감쇠). 카메라 드리프트 구동용.
-- v: 갱신할 속도 테이블, a: 애니메이션 설정, t: 경과초, dt: 프레임 델타
local function updateAxis(v, a, t, dt)
    local decay = clamp01(a.smooth ^ (dt * 33.333))
    if t < a.accelStart then
        v.x, v.y, v.z = 0, 0, 0
    elseif t < a.accelEnd then
        -- Vector3.Lerp(target, v, decay): target + (v - target) * decay
        v.x = a.tx + (v.x - a.tx) * decay
        v.y = a.ty + (v.y - a.ty) * decay
        v.z = a.tz + (v.z - a.tz) * decay
    elseif a.constantEnd < 0 or t < a.constantEnd then
        v.x, v.y, v.z = a.tx, a.ty, a.tz
    else
        v.x, v.y, v.z = v.x * decay, v.y * decay, v.z * decay
    end
end

-- 연출을 종료하고 메인메뉴로 전환한다(중복 호출 방지).
-- 동적 레이어(레터박스/페이드/텍스트)는 씬 언로드 시 자동 정리되고, 카메라 off는 Scene:Load이 처리한다.
local function finish()
    if finished then return end
    finished = true
    if fadeHandle then fadeHandle:SetAlpha(1) end   -- 스킵 시에도 화면을 검게(카메라 off가 전환 중 유지)
    XOPS.Scene:Load(2)                               -- 메인메뉴 (Scene:Load이 카메라 off 후 로드)
end

-- ----- 라이프사이클 -----
function M.start()
    -- 마우스 커서: 창 안에서 숨김, 자유 이동(고정 안 함), 시작 시 중앙으로 이동
    XOPS.Input:SetMouseCursor(true, false, true)
    XOPS.Camera:SetFieldOfView(CAM_FOV)

    XOPS.Camera:GoTo(CAM_POS.x, CAM_POS.y, CAM_POS.z)
    XOPS.Camera:SetEuler(CAM_EULER.x, CAM_EULER.y, CAM_EULER.z)

    -- 레터박스: 위/아래 가로 스트레치 검정 바 (최상단 레이어 → 텍스트/페이드까지 프레이밍)
    XOPS.UI:CreateImage(LETTERBOX_LAYER, true, "StretchTop",    "", 0, 0, 0, LETTERBOX_THICKNESS, 0, 0, 0, 1)
    XOPS.UI:CreateImage(LETTERBOX_LAYER, true, "StretchBottom", "", 0, 0, 0, LETTERBOX_THICKNESS, 0, 0, 0, 1)

    -- 화면 페이드: 풀스크린 검정 RawImage(최하단 레이어). 검정으로 시작, 알파는 update에서 fadeValue로 구동.
    fadeHandle = XOPS.UI:CreateImage(FADE_LAYER, true, "StretchFull", "", 0, 0, 0, 0, 0, 0, 0, 1)

    -- 크레딧 텍스트 생성(알파 0으로 시작 → update에서 페이드). 4번째 인자=글자 정렬(여기선 pivot과 같게 줌; 따로 두려면 그 값만 바꾸면 됨).
    for i = 1, #TEXTS do
        local d = TEXTS[i]
        textHandles[i] = XOPS.UI:CreateText(TEXT_LAYER, true, d.pivot, d.pivot, d.text, d.x, d.y, d.w, d.h, 0, d.r, d.g, d.b, 0)
    end
end

function M.update(t, dt)
    updateAxis(pos, POS_ANIM, t, dt)
    updateAxis(rot, ROT_ANIM, t, dt)
    XOPS.Camera:Translate(pos.x * dt, pos.y * dt, pos.z * dt)
    XOPS.Camera:Rotate(rot.x * dt, rot.y * dt, rot.z * dt)

    -- 화면 페이드 알파 갱신(인 → 유지 → 아웃)
    fadeHandle:SetAlpha(fadeValue(t))

    -- 각 텍스트 알파를 타이밍에 맞춰 갱신
    for i = 1, #TEXTS do
        local d = TEXTS[i]
        textHandles[i]:SetAlpha(fadeAlpha(t, d.inS, d.inE, d.outS, d.outE))
    end

    -- 자동 종료 또는 스킵(ESC / 좌클릭)
    if t > END_TIME or XOPS.Input:WasPressed("escape") or XOPS.Input:WasPressed("fire") then
        finish()
    end
end

return M
