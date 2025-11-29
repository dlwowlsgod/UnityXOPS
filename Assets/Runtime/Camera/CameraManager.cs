using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityXOPS
{
    public class CameraManager : Singleton<CameraManager>
    {
        [SerializeField]
        private Camera currentCamera;

        protected override void Awake()
        {
            base.Awake();
            currentCamera = FindFirstObjectByType<Camera>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            currentCamera = FindFirstObjectByType<Camera>();
        }
    }
}
