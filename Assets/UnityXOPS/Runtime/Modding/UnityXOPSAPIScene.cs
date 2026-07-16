using UnityEngine;
using UnityEngine.SceneManagement;
using XLua;

namespace UnityXOPS.Modding
{
    public partial class UnityXOPSAPI
    {
        private SceneAPI m_scene;
        public SceneAPI Scene => m_scene ??= new SceneAPI();
    }

    /// <summary>
    /// 모드에 씬 전환과 미션 수명 제어를 제공하는 API 그룹. Lua에서는 XOPS.Scene 으로 접근한다.
    /// 풀 회수와 카메라 끄기는 Load 가 알아서 하지만, 맵을 언제까지 유지할지(UnloadMap/UnloadMission)는
    /// 호출하는 쪽이 정한다 — 브리핑→메인게임처럼 맵을 넘겨야 하는 전환이 있기 때문이다.
    /// </summary>
    [LuaCallCSharp]
    public class SceneAPI
    {
        // 브리핑 씬 빌드 인덱스(원본 MainmenuScene.Load와 동일).
        private const int k_briefingScene = 3;
        // 메인게임 씬 빌드 인덱스.
        private const int k_maingameScene = 4;

        /// <summary>
        /// 미션을 로드하고 브리핑 씬으로 전환한다. 원본 MainmenuScene.Load와 동일 흐름:
        /// 현재(데모) 맵 언로드 → 미션 데이터/맵 로드 → 풀 회수 + 카메라 off + 브리핑 전환.
        /// </summary>
        /// <param name="index">미션 목록 인덱스(현재 뷰 기준).</param>
        /// <param name="addon">true면 애드온(.mif) 미션, false면 공식 미션.</param>
        /// <param name="page">애드온 페이지(0-기반). 공식 미션이면 무시된다.</param>
        public void LoadMission(int index, bool addon, int page)
        {
            // 배경(데모) 맵을 미션 맵으로 교체. 미션 데이터/통계는 LoadMissionData/LoadPointData가 세팅한다.
            if (MapLoader.Loaded)
            {
                MapLoader.UnloadBlockData();
                MapLoader.UnloadPointData();
                MapLoader.UnloadSkyData();
            }
            HumanController.TickEnabled = false;   // 브리핑은 정지 상태로 진입(Maingame.Start가 다시 켬)

            MapLoader.LoadMissionData(index, addon, page);
            MapLoader.LoadBlockData(MapLoader.Instance.MissionBD1Path);
            MapLoader.LoadPointData(MapLoader.Instance.MissionPD1Path);
            MapLoader.LoadSkyData(MapLoader.Instance.SkyIndex);

            Load(k_briefingScene);   // 풀 회수 + 카메라 off + 브리핑 로드
        }

        /// <summary>
        /// 현재 미션을 처음부터 다시 시작한다. 캐릭터·무기·소품·이벤트·통계가 전부 초기 상태로 돌아간다.
        /// 이미 메인게임 화면이면 화면을 유지한 채 맵만 되돌리고, 다른 화면(결과 등)에서 부르면 메인게임을 새로 연다 —
        /// 어느 쪽으로 부르든 결과는 같으므로 호출하는 쪽은 신경 쓰지 않아도 된다.
        /// 진행 중인 미션이 없으면(메뉴/오프닝 등) 아무 일도 하지 않는다.
        /// </summary>
        public void RestartMission()
        {
            // 맵이 떠 있어도 미션이 아닐 수 있다(메뉴의 배경 데모). 되돌릴 미션 경로가 있어야만 진행한다.
            if (!MapLoader.Loaded || string.IsNullOrEmpty(MapLoader.Instance.MissionBD1Path))
            {
                return;
            }

            if (SceneManager.GetActiveScene().buildIndex == k_maingameScene)
            {
                RestartInPlace();
                return;
            }

            ReloadMissionMap();
            HumanController.TickEnabled = false;   // Maingame.Start가 다시 켜고 BeginMission 호출
            Load(k_maingameScene);
        }

        /// <summary>
        /// 메인게임 화면을 유지한 채 맵만 되돌린다. 씬이 살아 있으므로 카메라를 보호하고
        /// AI/충돌 매니저 캐시를 명시적으로 비워야 한다(씬을 새로 열면 새 인스턴스라 필요 없는 일).
        /// </summary>
        private static void RestartInPlace()
        {
            // 카메라는 플레이어 CameraRoot 의 자식이라 포인트 언로드로 플레이어가 파괴되면 같이 파괴된다.
            // 미리 떼어 보호하고, 재로드 후 PlayerController 가 새 플레이어에 다시 붙인다.
            Camera main = Camera.main;
            if (main != null && main.transform.parent != null) main.transform.SetParent(null, true);

            ClearTransientPools();
            ReloadMissionMap();

            // 동기 재로드라 "Humans 빔" 자가정리 타이밍이 없어 직접 비운다(죽은 참조/AI 잔존 방지).
            AIController ai = Object.FindFirstObjectByType<AIController>();
            if (ai != null) ai.ResetState();
            HumanCollision collision = Object.FindFirstObjectByType<HumanCollision>();
            if (collision != null) collision.ResetState();

            HumanController.TickEnabled = true;
            EventManager.Instance.BeginMission();
        }

        /// <summary>
        /// 같은 미션 맵을 처음 상태로 다시 읽는다. LoadPointData 가 통계 리셋 + 사람/무기/소품 재배치를 수행한다.
        /// </summary>
        private static void ReloadMissionMap()
        {
            MapLoader.UnloadBlockData();
            MapLoader.UnloadPointData();
            MapLoader.UnloadSkyData();
            MapLoader.LoadBlockData(MapLoader.Instance.MissionBD1Path);
            MapLoader.LoadPointData(MapLoader.Instance.MissionPD1Path);
            MapLoader.LoadSkyData(MapLoader.Instance.SkyIndex);
        }

        /// <summary>
        /// 맵(블록/포인트/스카이)만 해제하고 사람 동작을 멈춘다. 미션 데이터와 통계는 남으므로
        /// 미션이 끝나고 Result로 넘어갈 때 쓴다(Result가 미션 이름·통계를 읽는다).
        /// </summary>
        public void UnloadMap()
        {
            HumanController.TickEnabled = false;

            if (!MapLoader.Loaded)
            {
                return;
            }

            MapLoader.UnloadBlockData();
            MapLoader.UnloadPointData();
            MapLoader.UnloadSkyData();
        }

        /// <summary>
        /// LoadMission이 로드한 미션(맵/포인트/스카이 + 미션 데이터)을 해제한다. 브리핑에서 미션을 포기할 때 호출한다.
        /// 메인게임으로 진행할 때는 맵을 그대로 넘겨야 하므로 호출하지 않는다.
        /// </summary>
        public void UnloadMission()
        {
            HumanController.TickEnabled = false;

            if (!MapLoader.Loaded)
            {
                return;
            }

            MapLoader.UnloadBlockData();
            MapLoader.UnloadPointData();
            MapLoader.UnloadMissionData();
            MapLoader.UnloadSkyData();
        }

        /// <summary>
        /// 빌드 인덱스로 씬을 로드한다. 로드 직전 활성 풀(탄/사운드/이펙트)을 회수하고 현재 카메라를 꺼 전환 중 잔상을 막는다.
        /// </summary>
        /// <param name="index">Build Settings의 씬 인덱스</param>
        public void Load(int index)
        {
            ClearTransientPools();
            DisableCurrentCamera();
            SceneManager.LoadScene(index);
        }

        /// <summary>
        /// 이름으로 씬을 로드한다. 로드 직전 활성 풀(탄/사운드/이펙트)을 회수하고 현재 카메라를 꺼 전환 중 잔상을 막는다.
        /// </summary>
        /// <param name="name">씬 이름</param>
        public void LoadByName(string name)
        {
            ClearTransientPools();
            DisableCurrentCamera();
            SceneManager.LoadScene(name);
        }

        /// <summary>
        /// 게임을 종료한다. 에디터에서는 플레이 모드를 중단하고, 빌드에서는 Application.Quit을 호출한다.
        /// </summary>
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// 씬 전환 시 살아있던 일시적 풀 오브젝트(날아가던 탄/재생 중 사운드/이펙트)를 즉시 회수한다.
        /// 모든 전환에서 항상 필요하고 맵(블록/사람/무기/소품)은 건드리지 않으므로 멱등하다 — 풀이 비어 있으면 무동작.
        /// 맵 로드/언로드는 씬마다 수명이 달라(브리핑~메인게임 유지) 여기서 처리하지 않는다.
        /// </summary>
        private static void ClearTransientPools()
        {
            if (BulletManager.Loaded) BulletManager.Instance.ClearPool();
            if (SoundManager.Loaded) SoundManager.Instance.ClearPool();
            if (EffectManager.Loaded) EffectManager.Instance.ClearPool();
        }

        /// <summary>
        /// 씬을 바꾸기 직전에 현재 메인 카메라 렌더링을 끈다.
        /// 페이드가 투명해지거나 UI가 파괴되는 전환 틈에 무너지는 구 씬이 한 프레임 드러나는 것을 막는다.
        /// 다음 씬의 카메라는 별개라 기본 on 상태로 켜진다.
        /// GameObject 가 아니라 컴포넌트를 끄는 것이 의도다 — 이래야 Camera.main 이 null 이 되어,
        /// 카메라를 만지는 다른 LateUpdate 들이 전환 프레임에 자동으로 멈춘다(MaingameScene 참조).
        /// </summary>
        private static void DisableCurrentCamera()
        {
            CameraDirector.Instance.SetActive(false);
        }
    }
}
