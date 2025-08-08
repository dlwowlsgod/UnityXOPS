using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// A generic Singleton class to enforce the single instance pattern for MonoBehaviour-derived classes.
    /// </summary>
    /// <typeparam name="T">The type of the class inheriting from this Singleton.</typeparam>
    /// <remarks>
    /// This class ensures that there is only one instance of the specified type T at runtime.
    /// If no instance exists, it creates a new GameObject with the component automatically.
    /// </remarks>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        /*
        전형적인 싱글톤 클래스입니다. 당연히 남발 할거고요
        구현 방식도 전형적인 템플릿 그 자체고요
        단지 유니티 6 버전에 맞게 수정했습니다.
         */
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance)
                {
                    return _instance;
                }

                _instance = FindFirstObjectByType<T>();
                if (_instance)
                {
                    return _instance;
                }
                
                var obj = new GameObject(typeof(T).Name, typeof(T));
                _instance = obj.AddComponent<T>();
                return _instance;
            }
        }

        private void Awake()
        {
            if (!_instance)
            {
                _instance = this as T;
                if (transform.parent || transform.root)
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