using UnityEngine;

/// <summary>
/// MonoBehaviour를 상속받는 클래스를 싱글톤 오브젝트화 하기 위한 클래스입니다.
/// </summary>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    /// <summary>
    /// Singleton Instance를 반환합니다. Singleton Instance는 항상 하나만 존재함을 보장합니다.
    /// </summary>
    /// <returns>하나임을 보장받는 Singleton Instance</returns>
    public static T Instance {
        get {
            if (_instance == null) {
                _instance = FindFirstObjectByType<T>();
                if (_instance == null) {
                    var obj = new GameObject(typeof(T).Name, typeof(T));
                    _instance = obj.GetComponent<T>();
                }
            }
            return _instance;
        }
    }

    private void Awake() {
        if (transform.parent != null && transform.root != null) {
            DontDestroyOnLoad(transform.root.gameObject);
        } else {
            DontDestroyOnLoad(gameObject);
        }
    }
}
