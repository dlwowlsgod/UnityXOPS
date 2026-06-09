using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// 플레이어 1명의 미션 통계 — 플레이타임/발사/명중/킬/헤드샷. 원본 OpenXOPS GameInfo(gamemain.h:102-112) 대응.
    /// Result 화면이 읽는다. Briefing→Maingame→Result 가 같은 맵(씬 전환만)이라 MapLoader 싱글톤에 보관해 씬을 넘어 유지된다.
    ///
    /// 원본은 전체 Human 배열에 프레임 단위로 기록(objectmanager Human_*[]) 후 플레이어 슬롯만 누적(gamemain MainGameInfo)하지만,
    /// 포팅은 기록 시점에 shooter==Player 게이트를 두어 동일 결과를 단일 누적기로 처리한다.
    /// </summary>
    public class MissionStats
    {
        // 미션 경과 시간(초). 원본은 framecnt(프레임 카운트, gamemain.cpp:2705)지만 포팅은 실시간 누적. 분/초 표시 환산은 UI 책임.
        public float PlayTime;

        // 발사 횟수 — 플레이어가 총을 1회 발사할 때마다 +1 (산탄 펠릿 수 무관, 수류탄 제외). 원본 fire(gamemain.cpp:2240, ShotWeapon 반환 1만).
        public int Fire;

        // 명중 가중 합(float). 산탄은 펠릿당 2/pellet 가중(전탄 명중=2.0), 단발=1.0. 원본 ontarget(objectmanager.cpp:975, ontargetcnt).
        public float OnTarget;

        // 킬 수 — 이번 타격으로 적 HP 가 0 이하로 떨어진 경우 +1 (총알/수류탄 모두). 원본 kill(objectmanager.cpp:978,1115).
        public int Kill;

        // 헤드샷 수 — 머리 부위 명중 시 +1 (킬 무관, 수류탄 제외). 원본 headshot(objectmanager.cpp:976).
        public int Headshot;

        // 표시/정확도용 정수 명중(floor) — 원본 gamemain.cpp:4777 (int)floor(ontarget).
        public int OnTargetInt => Mathf.FloorToInt(OnTarget);

        // 정확도(%) — floor(ontarget)/fire×100. fire==0 이면 0. 산탄 풀히트로 100% 초과 가능(원본 의도). 원본 gamemain.cpp:4779-4781.
        public float AccuracyPercent => Fire > 0 ? (float)OnTargetInt / Fire * 100f : 0f;

        public void Reset()
        {
            PlayTime = 0f;
            Fire     = 0;
            OnTarget = 0f;
            Kill     = 0;
            Headshot = 0;
        }
    }
}
