using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
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
