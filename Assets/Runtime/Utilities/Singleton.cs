using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// A generic singleton base class for MonoBehaviour types.
    /// Ensures that only one instance of the MonoBehaviour exists in the scene,
    /// and provides global access to the instance.
    /// </summary>
    /// <typeparam name="T">The type of the singleton MonoBehaviour.</typeparam>
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
                    }
                    else
                    {
                        var obj = new GameObject(typeof(T).Name);
                        _instance = obj.AddComponent<T>();
                    }
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                
                if (transform.parent != null)
                {
                    DontDestroyOnLoad(transform.root.gameObject);
                }
                else
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}
