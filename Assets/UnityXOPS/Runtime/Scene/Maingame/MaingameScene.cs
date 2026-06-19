using JJLUtility;
using JJLUtility.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityXOPS
{
    /// <summary>
    /// 메인게임 씬을 관리하고 ESC 입력 시 메인메뉴로 복귀하는 컨트롤러.
    /// InputManager가 관리하지 않는 글로벌 키(시점 전환 등)도 여기서 처리한다.
    /// </summary>
    public class MaingameScene : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;

        private MaingameFadeSequence m_fadeSequence;

        // 씬 진입/재시작 직후 ESC·F12 잠금 시간 — 전환 시 눌린 키가 새어 즉시 나가기/재시작 되는 것 방지.
        private const float k_inputLockSeconds = 1f;
        private float m_inputLockTimer;

        private bool m_missionEnded;  // 종료 시퀀스(FadeOut+EndText) 1회 트리거 가드. RestartMission 에서 리셋.
        private bool m_resultLoading; // Result 씬 전환 1회 가드. RestartMission 에서 리셋.

        private void Start()
        {
            m_fadeSequence = GetComponent<MaingameFadeSequence>();

            InputManager.MouseCursorMode(true, true, true);
            HumanController.TickEnabled = true;
            EventManager.Instance.BeginMission(); // 미션 이벤트/판정 가동 (Maingame 진입 시)
            m_inputLockTimer = k_inputLockSeconds;
        }

        private void Update()
        {
            // 진입/재시작 직후 잠금 카운트다운 — 잠금 중엔 ESC·F12 무시.
            if (m_inputLockTimer > 0f) m_inputLockTimer -= Time.deltaTime;
            bool inputLocked = m_inputLockTimer > 0f;

            if (!inputLocked && InputManager.Keyboard.escapeKey.wasPressedThisFrame)
            {
                ClearPoolsAndUnloadMap(true);
                HumanController.TickEnabled = false;
                // StopMission 은 EventManager 가 sceneUnloaded 로 자동 처리 (어떤 이탈 경로든 누수 없음).
                Camera.main.gameObject.SetActive(false);
                SceneManager.LoadScene(2);
            }

            UpdateViewModeInput();

            if (!inputLocked && InputManager.Keyboard.f12Key.wasPressedThisFrame)
            {
                RestartMission();
            }

            // 플레이타임 누적 — 미션 진행 중에만 (원본 framecnt, gamemain.cpp:2705). 종료/판정 확정 후 정지.
            if (EventManager.Instance.Result == MissionResult.InProgress)
                MapLoader.AddPlayTime(Time.deltaTime);

            // 미션 종료 시퀀스 — 반드시 1회만. 매 프레임 호출하면 FadeOut 이 매번 알파 0 으로 리셋돼 화면이 안 어두워짐.
            if (!m_missionEnded && EventManager.Instance.Result != MissionResult.InProgress)
            {
                m_missionEnded = true;
                m_fadeSequence.PlayMissionEndSequence();
            }

            // 종료 시퀀스(화면 암전 + 종료 텍스트)가 모두 끝나면 Result 로 전환 — 1회만. 원본은 종료 후 4초 고정(gamemain.cpp:2715)이지만
            // 여기선 페이드 완료를 직접 신호로 받아 전환(시간이 인스펙터 페이드 값에 따라가도록).
            if (m_missionEnded && !m_resultLoading && m_fadeSequence.MissionEndComplete)
            {
                m_resultLoading = true;
                LoadResult();
            }
        }

        /// <summary>
        /// 미션 종료 시퀀스 완료 후 Result(빌드 인덱스 5)로 전환. 맵 시각물(블록/포인트/스카이)은 언로드하되
        /// 미션 데이터·통계(MapLoader.Stats)는 유지 — Result 가 읽는다(Stats 는 LoadPointData 에서만 리셋되므로 언로드해도 생존).
        /// UnloadPointData 가 플레이어와 그 자식 카메라를 함께 파괴해 카메라 누수도 방지된다.
        /// </summary>
        private void LoadResult()
        {
            ClearPoolsAndUnloadMap(false); // UnloadMissionData 는 호출 안 함 — Result 가 미션 이름 표시에 사용 (통계 m_stats 는 유지).
            HumanController.TickEnabled = false;
            // StopMission 은 EventManager 가 sceneUnloaded 로 자동 처리.
            SceneManager.LoadScene(5);
        }

        /// <summary>
        /// F12 — 현재 미션을 맵 재로드로 처음부터 다시 시작. 캐릭터·무기·소품 재배치, 이벤트·AI·미션 상태 전부 초기화.
        /// 씬은 유지(미션 데이터/경로 보존)하고 블록/포인트/스카이만 언로드 후 같은 미션 맵을 재로드한다.
        /// </summary>
        private void RestartMission()
        {
            // 카메라는 플레이어 CameraRoot 자식 → 포인트 언로드로 플레이어가 파괴되면 카메라도 같이 파괴됨.
            // 언로드 전에 분리해 보호. 재로드 후 PlayerController 가 새 플레이어 CameraRoot 로 다시 붙인다.
            Camera main = Camera.main;
            if (main != null && main.transform.parent != null) main.transform.SetParent(null, true);

            // 풀 회수 + 현재 맵 언로드 (미션 데이터는 유지 — 같은 미션 경로/스카이 재사용).
            ClearPoolsAndUnloadMap(false);

            // AI/충돌 매니저 캐시 초기화 — 동기 재로드라 "Humans 빔" 자가정리 타이밍이 없어 명시적으로 비운다(stale brain/참조 방지).
            AIController ai = FindFirstObjectByType<AIController>();
            if (ai != null) ai.ResetState();
            HumanCollision col = FindFirstObjectByType<HumanCollision>();
            if (col != null) col.ResetState();

            // 같은 미션 맵 재로드 → 캐릭터·무기·소품·이벤트/경로 노드 전부 재생성 (LoadPointData 가 m_sortedRawPointData 재구성).
            MapLoader.LoadBlockData(MapLoader.Instance.MissionBD1Path);
            MapLoader.LoadPointData(MapLoader.Instance.MissionPD1Path);
            MapLoader.LoadSkyData(MapLoader.Instance.SkyIndex);

            // 미션 상태 초기화 — 이벤트 라인 커서/결과/메시지 리셋 + Tick 보장.
            HumanController.TickEnabled = true;
            EventManager.Instance.BeginMission();
            m_inputLockTimer = k_inputLockSeconds; // 재시작 직후도 1초 잠금 (F12 연타·잔여 입력 방지)

            // 종료 시퀀스 상태 리셋 — 검은 화면 다시 페이드 인 + 이전 종료 텍스트 제거 (안 하면 재시작해도 화면이 검은 채 + 종료텍스트 잔존).
            m_missionEnded = false;
            m_resultLoading = false;
            if (m_fadeSequence != null) m_fadeSequence.ResetForRestart();
        }

        /// <summary>
        /// 풀(탄/사운드/이펙트)을 회수하고 맵 시각물(블록/포인트/스카이)을 언로드한다. unloadMission=true면 미션 데이터까지 언로드.
        /// </summary>
        private void ClearPoolsAndUnloadMap(bool unloadMission)
        {
            BulletManager.Instance.ClearPool();
            SoundManager.Instance.ClearPool();
            EffectManager.Instance.ClearPool();
            MapLoader.UnloadBlockData();
            MapLoader.UnloadPointData(); // 사람/무기/소품 파괴 + m_humans 비움
            MapLoader.UnloadSkyData();
            if (unloadMission) MapLoader.UnloadMissionData();
        }

        /// <summary>
        /// F2 = 1인칭, F1 = 3인칭(좌), F3 = 3인칭(우). 사망 중에는 입력 무시 (사망 카메라 유지).
        /// </summary>
        private void UpdateViewModeInput()
        {
            if (playerController == null) return;

            Human player = MapLoader.Player;
            if (player == null || !player.Alive) return;

            if (InputManager.Keyboard.f2Key.wasPressedThisFrame) playerController.SetViewMode(ViewMode.FirstPerson);
            else if (InputManager.Keyboard.f1Key.wasPressedThisFrame) playerController.SetViewMode(ViewMode.ThirdPersonLeft);
            else if (InputManager.Keyboard.f3Key.wasPressedThisFrame) playerController.SetViewMode(ViewMode.ThirdPersonRight);
        }

        /// <summary>
        /// Player가 없으면 카메라를 월드 원점에서 cameraAttachPosition 만큼 올린 위치에 정지시킨다.
        /// 씬 초기(스폰 전) 및 Player 사망 구간에 동일 경로로 동작.
        /// </summary>
        private void LateUpdate()
        {
            if (MapLoader.Player != null) return;

            Camera main = Camera.main;
            if (main == null) return;

            float height = DataManager.Instance.HumanParameterData.humanGeneralData.cameraAttachPosition;
            Transform tr = main.transform;

            if (tr.parent != null) tr.SetParent(null, true);
            tr.SetPositionAndRotation(new Vector3(0f, height, 0f), Quaternion.identity);
        }
    }
}
