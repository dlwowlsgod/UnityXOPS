using JJLUtility.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityXOPS
{
    /// <summary>
    /// 모든 매니저 로딩이 완료되면 다음 씬으로 전환하는 초기화 씬 컨트롤러.
    /// </summary>
    public class InitScene : MonoBehaviour
    {
        /// <summary>
        /// 매 프레임마다 모든 매니저의 로드 완료 여부를 확인하고, 완료 시 씬을 전환한다.
        /// </summary>
        private void Update()
        {
            if (!ImageLoader.Loaded || !ModelLoader.Loaded || !MapLoader.Loaded)
            {
                return;
            }

            if (!DataManager.Loaded || !FontManager.Loaded || !InputManager.Loaded)
            {
                return;
            }

            SceneManager.LoadScene(1);
        }
    }
}