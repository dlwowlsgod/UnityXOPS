using UnityEngine;

namespace JJLUtility
{
    public class SingletonBehavior<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T m_instance;
        private static bool m_isApplicationQuitting;

        public static T Instance
        {
            get
            {
                // 애플리케이션이 종료 중이면 null을 반환하여 새로운 인스턴스 생성을 방지
                if (m_isApplicationQuitting)
                {
                    return null;
                }

                if (m_instance == null)
                {
                    //씬에 이미 존재하는지 확인
                    var sameComponents = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (sameComponents.Length > 0)
                    {
                        if (sameComponents.Length > 1)
                        {
                            for (int i = 1; i < sameComponents.Length; i++)
                            {
                                Destroy(sameComponents[i].gameObject);
                            }
                        }
                        m_instance = sameComponents[0];
                    }
                    else
                    {
                        //이런 식으로 lazy 생성도 가능하지만 명시적으로 GameObject에 붙이기
                        var singletonObject = new GameObject(typeof(T).Name, typeof(T));
                        m_instance = singletonObject.GetComponent<T>();
                    }
                }

                return m_instance;
            }
        }

        public static bool Loaded => m_instance != null;

        protected virtual void Awake()
        {
            if (m_instance == null)
            {
                m_instance = this as T;
                DontDestroyOnLoad(transform.root.gameObject);
            }
            else if (m_instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            m_isApplicationQuitting = true;
        }
    }
}