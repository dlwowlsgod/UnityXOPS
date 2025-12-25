using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityXOPS
{
    public class InitScene : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(LoadScene());
        }
        
        private IEnumerator LoadScene()
        {
            var waitUntil = new WaitUntil(() => InitializeOnLoad.Initialized);
            yield return waitUntil;
            
            
#if UNITY_EDITOR
            Debug.Log("Initialization complete.");
#endif
            SceneManager.LoadScene("MainGame");
        }
    }
}
