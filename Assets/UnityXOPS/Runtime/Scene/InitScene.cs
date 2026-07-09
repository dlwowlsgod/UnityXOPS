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
        private void Update()
        {
            if (!ImageLoader.Loaded || !ModelLoader.Loaded || !MapLoader.Loaded)
            {
                return;
            }

            if (!ConfigManager.Loaded || !DataManager.Loaded || !FontManager.Loaded || !InputManager.Loaded)
            {
                return;
            }

            SceneManager.LoadScene(1);
        }
    }
}