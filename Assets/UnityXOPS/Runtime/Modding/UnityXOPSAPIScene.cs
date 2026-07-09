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
    /// 모드에 씬 전환을 제공하는 API 그룹. Lua에서는 XOPS.Scene 으로 접근한다.
    /// 씬별 정리(풀 해제/맵 언로드 등)는 각 씬의 C# 컨트롤러가 OnDestroy에서 처리하므로,
    /// Lua는 전환 시점만 결정하면 된다.
    /// </summary>
    [LuaCallCSharp]
    public class SceneAPI
    {
        // 브리핑 씬 빌드 인덱스(원본 MainmenuScene.Load와 동일).
        private const int k_briefingScene = 3;

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
        /// </summary>
        private static void DisableCurrentCamera()
        {
            CameraDirector.Instance.SetActive(false);
        }
    }
}
