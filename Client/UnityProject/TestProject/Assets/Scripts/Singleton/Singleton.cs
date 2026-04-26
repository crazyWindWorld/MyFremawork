using System;

namespace Singleton
{
    public abstract class Singleton<T> where T : Singleton<T>, new()
    {
        private static bool _initialized;
        private static T _instance;
        private readonly static object _lock = new object();
        public static T Instance
        {
            get
            {
                if (!_initialized)
                {
                    lock (_lock)
                    {
                        if (!_initialized)
                        {
                            _instance = new T();
                            _initialized = true;
                            _instance.Init();
                        }
                    }
                }
                return _instance;
            }
            set => _instance = value;
        }

        protected virtual void Init()
        {

        }
    }
}
