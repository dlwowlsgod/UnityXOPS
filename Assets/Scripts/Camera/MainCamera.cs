using UnityEngine;

namespace UnityXOPS
{
    public class MainCamera : MonoBehaviour
    {
        public static Camera Instance { get; private set; }
        [SerializeField] private Transform targetTransform;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Instance = GetComponent<Camera>();
        }

        public void ChangeCameraTarget(Transform target)
        {
            targetTransform = target;
        }
    }
}