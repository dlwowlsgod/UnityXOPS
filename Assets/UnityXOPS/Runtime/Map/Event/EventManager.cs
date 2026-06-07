using System.Collections.Generic;
using JJLUtility;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityXOPS
{
    /// <summary>
    /// 미션 결과 (폴링용). 엔진은 결과 확정(set)만 하고, 실제 UI 표시/씬 전환은 이 값을 읽는 쪽(추후)이 담당한다.
    /// </summary>
    public enum MissionResult
    {
        InProgress = 0,
        Complete   = 1,
        Failed     = 2,
    }

    /// <summary>
    /// OpenXOPS 이벤트 시스템 포팅 — event.cpp(EventControl) + gamemain.cpp 이벤트 루프 + objectmanager CheckGameOverorComplete.
    /// 3개 라인의 선형 시퀀스(커서 체인)를 매 프레임(33.333fps) 진행한다. 각 노드는 param3=자기ID(P4), param2=다음ID(P3).
    /// 대기(trigger) 노드는 조건 충족 시 커서를 다음으로 옮기고, 즉시(action) 노드는 바로 실행. 라인당 프레임 최대 6스텝.
    ///
    /// 미션 성공/실패/메시지는 "실행(상태 set)"까지만 — 화면 표시/씬 전환 등 UI 연동은 폴링하는 쪽이 처리(1차 보류).
    /// SingletonBehavior 라 DontDestroyOnLoad 로 유지되며, Maingame 진입 시 BeginMission 으로만 가동된다(Mainmenu 데모는 미가동).
    /// </summary>
    public class EventManager : SingletonBehavior<EventManager>
    {
        private const float k_gameFps         = 33.3333f;        // 원본 GAMEFPS
        private const float k_frameTime       = 1f / k_gameFps;
        private const int   k_maxCatchup      = 4;
        private const int   k_maxFrameStep    = 6;               // 원본 TOTAL_EVENTFRAMESTEP (액션 무한연쇄 방지 상한)
        private const float k_showMsgSec      = 5.0f;            // 원본 TOTAL_EVENTENT_SHOWMESSEC
        private const float k_msgFadeSec      = 0.2f;            // 메시지 페이드 인/아웃 시간 (원본 gamemain.cpp:3141-3144, 0.2초)
        private const float k_arrivalDist     = 2.5f;            // 원본 DISTANCE_CHECKPOINT 25.0 × 0.1

        // 원본 이벤트 라인 진입 식별번호 -100/-110/-120 (signed char). PD1 을 unsigned byte 로 읽으므로 156/146/136.
        private static readonly int[] k_lineEntryId = { 156, 146, 136 };

        private readonly int[] m_cursor  = new int[3]; // 라인별 현재 노드 식별번호 (원본 nextp4)
        private readonly int[] m_waitcnt = new int[3]; // 라인별 시간 대기(17) 프레임 카운터

        private MissionResult m_result = MissionResult.InProgress;
        private int   m_messageId = -1; // 현재 표시 메시지 ID (-1 = 없음)
        private int   m_messageCnt;     // 메시지 표시 경과 프레임
        private bool  m_running;
        private float m_accum;

        public MissionResult Result             => m_result;
        public int           CurrentMessageId   => m_messageId;
        public string        CurrentMessageText => m_messageId >= 0 ? MapLoader.GetMessageText(m_messageId) : string.Empty;
        public bool          IsRunning          => m_running;

        /// <summary>
        /// 현재 메시지의 페이드 알파(0~1). 메시지 없으면 0. 표시 시작 0.2초 페이드인 → 유지 → 종료 0.2초 페이드아웃.
        /// 원본 gamemain.cpp:3139-3146 GetEffectAlpha 선형 페이드. m_messageCnt(33.333fps 프레임) 기반이라 시간 락도 원본 일치.
        /// </summary>
        public float CurrentMessageAlpha
        {
            get
            {
                if (m_messageId < 0) return 0f;
                float fadeFrames  = k_msgFadeSec  * k_gameFps; // ≈6.7프레임
                float totalFrames = k_showMsgSec  * k_gameFps; // ≈167프레임
                if (m_messageCnt < fadeFrames)                 return Mathf.Clamp01(m_messageCnt / fadeFrames);                  // 페이드 인
                if (m_messageCnt > totalFrames - fadeFrames)   return Mathf.Clamp01((totalFrames - m_messageCnt) / fadeFrames);  // 페이드 아웃
                return 1f;                                                                                                       // 완전 표시
            }
        }

        protected override void Awake()
        {
            base.Awake();
            // 어느 씬이든 빠져나갈 때(특히 Maingame 이탈) 자동 정지 → m_running 누수 차단.
            // 시작(BeginMission)은 Maingame 씬만 명시적으로 호출하므로 Opening/Mainmenu/Briefing 에선 이벤트가 원천적으로 안 돈다.
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDestroy() => SceneManager.sceneUnloaded -= OnSceneUnloaded;

        private void OnSceneUnloaded(Scene scene) => StopMission();

        /// <summary>미션 시작 — 3개 라인 커서를 진입점으로 리셋하고 이벤트 진행을 켠다. Maingame 진입 시 1회 호출.</summary>
        public void BeginMission()
        {
            for (int i = 0; i < 3; i++) { m_cursor[i] = k_lineEntryId[i]; m_waitcnt[i] = 0; }
            m_result     = MissionResult.InProgress;
            m_messageId  = -1;
            m_messageCnt = 0;
            m_accum      = 0f;
            m_running    = true;
        }

        /// <summary>이벤트 진행 정지 (씬 이탈 시).</summary>
        public void StopMission() => m_running = false;

        private void FixedUpdate()
        {
            if (!m_running || !HumanController.TickEnabled) return;

            // 결정은 33.333fps 프레임 락 (원본 정수 카운트/확률 모델). 누산기로 30ms 마다 1프레임.
            m_accum += Time.fixedDeltaTime;
            int guard = 0;
            while (m_accum >= k_frameTime && guard++ < k_maxCatchup)
            {
                m_accum -= k_frameTime;
                StepFrame();
            }
            if (m_accum > k_frameTime) m_accum = 0f;
        }

        private void StepFrame()
        {
            // 메시지 표시 타이머 — 상태만 갱신(렌더는 UI). 5초 경과 시 자동 종료. 미션 종료와 무관하게 진행.
            if (m_messageId != -1)
            {
                m_messageCnt++;
                if (m_messageCnt >= (int)(k_showMsgSec * k_gameFps)) { m_messageId = -1; m_messageCnt = 0; }
            }

            if (m_result != MissionResult.InProgress) return; // 미션 확정 후 이벤트/판정 정지 (원본 end_framecnt 게이트)

            // (A) 자동 판정 — 이벤트와 독립 (원본 CheckGameOverorComplete). 동시 성립 시 클리어 우선.
            if (CheckAutoComplete()) { m_result = MissionResult.Complete; return; }
            if (CheckAutoFail())     { m_result = MissionResult.Failed;   return; }

            // (B) 이벤트 라인 3개 진행.
            for (int line = 0; line < 3; line++) StepLine(line);
        }

        private void StepLine(int line)
        {
            for (int step = 0; step < k_maxFrameStep; step++)
            {
                if (m_result != MissionResult.InProgress) return;                     // 노드가 미션 종료시킴
                if (!MapLoader.TryGetEventPoint(m_cursor[line], out RawPointData node)) return; // 노드 없음 → 라인 정지
                if (!ProcessNode(line, node)) return;                                 // 대기/정지
            }
        }

        /// <summary>노드 1개 처리. true=다음으로 진행, false=대기/정지.</summary>
        private bool ProcessNode(int line, RawPointData node)
        {
            switch ((EventType)node.param0)
            {
                case EventType.MissionComplete:
                    m_result = MissionResult.Complete;
                    return false;

                case EventType.MissionFailed:
                    m_result = MissionResult.Failed;
                    return false;

                case EventType.WaitDeath:
                {
                    Human t = MapLoader.SearchHuman(node.param1);
                    if (t == null || t.Alive) return false; // 대상 없거나 생존 → 대기 (원본 CheckDead)
                    break;
                }

                case EventType.WaitArrival:
                {
                    Human t = MapLoader.SearchHuman(node.param1);
                    if (!Arrived(t, node.position)) return false;
                    break;
                }

                case EventType.ChangeToWalk:
                    // 대상 웨이포인트(param1)의 이동모드를 Walk(0)로 변경 → 거기서 Wait(2)로 대기 중이던 Human 이 풀려 다음 포인트로 진행.
                    // AIMoveNavi.Mode 가 param1 을 라이브로 읽으므로(공유 인스턴스) 즉시 반영. 원본 SetMovePathMode(歩きに変更).
                    if (MapLoader.TryGetPathPoint(node.param1, out RawPointData path)) path.param1 = 0;
                    break;

                case EventType.WaitBreakObject:
                {
                    SmallObject so = MapLoader.SearchSmallObject(node.param1);
                    if (so != null && !so.IsDestroyed) return false; // 대상 있고 미파괴면 대기 (없으면 파괴 간주, 원본 CheckBreakSmallObject)
                    break;
                }

                case EventType.WaitCase:
                {
                    Human t = MapLoader.SearchHuman(node.param1);
                    if (!Arrived(t, node.position) || !HasCaseWeapon(t)) return false;
                    break;
                }

                case EventType.WaitTime:
                    m_waitcnt[line]++;
                    if (m_waitcnt[line] < (int)k_gameFps * node.param1) return false; // 원본 (int)GAMEFPS*sec 프레임 대기
                    break;

                case EventType.Message:
                    m_messageId  = node.param1; // 범위 밖 ID 도 set (원본 동작) → GetMessageText 가 빈문자 처리
                    m_messageCnt = 0;
                    break;

                case EventType.ChangeTeam:
                {
                    Human t = MapLoader.SearchHuman(node.param1);
                    if (t != null) t.SetTeam(0);
                    break;
                }

                default:
                    return false; // 미지원 타입 → 라인 정지
            }

            AdvanceCursor(line, node.param2);
            return true;
        }

        private void AdvanceCursor(int line, int nextId)
        {
            m_cursor[line]  = nextId;
            m_waitcnt[line] = 0; // 새 노드로 이동 — 시간 대기 카운터 초기화
        }

        private static bool Arrived(Human h, Vector3 nodePos)
        {
            if (h == null) return false;
            return (h.transform.position - nodePos).sqrMagnitude <= k_arrivalDist * k_arrivalDist;
        }

        private static bool HasCaseWeapon(Human h)
        {
            if (h == null) return false;
            // 케이스(서류가방) 무기 인덱스는 데이터(WeaponGeneralData.caseWeaponIndex, 원본 ID_WEAPON_CASE)로 지정.
            List<int> caseIndices = DataManager.Instance.WeaponParameterData.weaponGeneralData.caseWeaponIndex;
            if (caseIndices == null || caseIndices.Count == 0) return false;
            for (int slot = 0; slot < 2; slot++)
            {
                Weapon w = h.GetWeapon(slot);
                if (w != null && caseIndices.Contains(w.WeaponIndex)) return true;
            }
            return false;
        }

        /// <summary>원본 ObjectManager::CheckGameOverorComplete — 적(플레이어와 다른 팀) 생존 HP 합이 0 이면 성공.</summary>
        private static bool CheckAutoComplete()
        {
            Human player = MapLoader.Player;
            if (player == null) return false;
            int playerTeam = player.Team;

            var humans = MapLoader.Humans;
            if (humans == null) return false;

            float enemyHp = 0f;
            for (int i = 0; i < humans.Count; i++)
            {
                Human h = humans[i];
                if (h == null || h.Team == playerTeam) continue;
                if (h.Alive) enemyHp += h.HP;
            }
            return enemyHp <= 0f;
        }

        /// <summary>플레이어 사망 → 실패.</summary>
        private static bool CheckAutoFail()
        {
            Human player = MapLoader.Player;
            return player != null && !player.Alive;
        }
    }
}
