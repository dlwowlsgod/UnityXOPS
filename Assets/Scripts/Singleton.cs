using UnityEngine;

namespace UnityXOPS
{
    /// <summary>
    /// <see cref="MonoBehaviour">MonoBehaviour</see>를 상속받아 구현한 Singleton 클래스입니다.
    /// </summary>
    /// <typeparam name="T">싱글톤 클래스화 할 <see cref="MonoBehaviour">MonoBehaviour</see>를 상속받는 클래스</typeparam>
    /// <remarks>
    /// Singleton <see cref="GameObject">GameObject</see>가 Scene Hierarchy에 존재하지 않을 경우 생성합니다. 
    /// Singleton이 지정하지 않은 <see cref="GameObject">GameObject</see>가 존재할 경우 파괴합니다.
    /// </remarks>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
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

        protected virtual void Awake()
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