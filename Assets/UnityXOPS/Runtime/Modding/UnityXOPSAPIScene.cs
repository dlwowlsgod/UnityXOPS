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
        /// <summary>
        /// 빌드 인덱스로 씬을 로드한다. 로드 직전 현재 카메라를 강제로 꺼 전환 중 잔상을 막는다.
        /// </summary>
        /// <param name="index">Build Settings의 씬 인덱스</param>
        public void Load(int index)
        {
            DisableCurrentCamera();
            SceneManager.LoadScene(index);
        }

        /// <summary>
        /// 이름으로 씬을 로드한다. 로드 직전 현재 카메라를 강제로 꺼 전환 중 잔상을 막는다.
        /// </summary>
        /// <param name="name">씬 이름</param>
        public void LoadByName(string name)
        {
            DisableCurrentCamera();
            SceneManager.LoadScene(name);
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
