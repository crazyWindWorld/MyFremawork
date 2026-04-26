using UnityEngine;

namespace Singleton
{
    /// <summary>
    /// MonoBehaviour 单例基类
    /// </summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationQuitting = false;

        public static T Instance
        {
            get
            {
                if (_applicationQuitting)
                {
                    Debug.LogWarning($"[MonoSingleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<T>();

                        if (_instance == null)
                        {
                            var singletonObject = new GameObject();
                            _instance = singletonObject.AddComponent<T>();
                            singletonObject.name = $"[MonoSingleton] {typeof(T)}";

                            DontDestroyOnLoad(singletonObject);
                        }
                    }

                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
                OnInit();
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[MonoSingleton] Multiple instances of {typeof(T)} found. Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        protected virtual void OnInit() { }

        protected virtual void OnApplicationQuit()
        {
            _applicationQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
