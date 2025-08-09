using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace UnityXOPS
{
    /// <summary>
    /// Represents a MonoBehaviour-based utility class designed to manage or wait for initialization processes in a Unity application.
    /// </summary>
    public class WaitForInit : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(Initialize());
        }

        /// <summary>
        /// Handles the initialization process of the application, including loading necessary data,
        /// preparing the initial state, and transitioning to the opening scene.
        /// </summary>
        /// <returns>An IEnumerator for coroutine control, allowing Unity to wait for a frame during initialization processes.</returns>
        private IEnumerator Initialize()
        {
            PrivateProfileReader.LoadProfile();
            
            yield return null;
            
            StateMachine.Instance.SetState(GameState.OpeningStart);
        }
    }
}