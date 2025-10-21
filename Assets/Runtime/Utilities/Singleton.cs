using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// A generic singleton base class for MonoBehaviour.
    /// Ensures that only one instance of the specified type exists in the scene.
    /// </summary>
    /// <typeparam name="T">The type of the singleton class inheriting from MonoBehaviour.</typeparam>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    var instances = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (instances.Length > 0)
                    {
                        _instance = instances[0];
                        if (instances.Length > 1)
                        {
#if UNITY_EDITOR
                            Debug.LogWarning($"{instances.Length} {typeof(T).Name} singleton instances found. {instances.Length - 1} destroyed.");
#endif
                            for (int i = 1; i < instances.Length; i++)
                            {
                                Destroy(instances[i].gameObject);
                            }
                        }
                    }
                    else
                    {
                        _instance = new GameObject(typeof(T).Name).AddComponent<T>();
                    }
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            DontDestroyOnLoad(transform.root.gameObject);
        }
    }
}
