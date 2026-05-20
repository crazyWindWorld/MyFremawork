using System;
using System.Collections.Generic;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Runtime.CompilerServices;
using Fuel.Log;
using Fuel.Singleton;

#endif
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fuel.Pools
{
    public interface IClear
    {
        void Clear();
    }

    public interface IDisposable
    {
        void Disposable();
    }

    public interface IObjectPool : IClear, IDisposable
    {
    }


    // ReSharper disable once ClassNeverInstantiated.Global
    public class ObjectPools : Singleton<ObjectPools>
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private sealed class ReferencePoolObjectComparer : IEqualityComparer<IObjectPool>
        {
            public static readonly ReferencePoolObjectComparer Instance = new ReferencePoolObjectComparer();

            public bool Equals(IObjectPool x, IObjectPool y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(IObjectPool obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
#endif

        private int _maxPoolCount = 1000;

        public int MaxPoolCount
        {
            set => _maxPoolCount = value;
        }


#if UNITY_EDITOR
        private Dictionary<RuntimeTypeHandle, List<IObjectPool>> _hashCodePool;
#endif
        private Dictionary<RuntimeTypeHandle, Stack<IObjectPool>> _pool;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private Dictionary<RuntimeTypeHandle, HashSet<IObjectPool>> _pooledObjects;
#endif

        protected void RegistrationLife()
        {
           
        }

        protected void Awake()
        {
            _pool = new Dictionary<RuntimeTypeHandle, Stack<IObjectPool>>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _pooledObjects = new Dictionary<RuntimeTypeHandle, HashSet<IObjectPool>>();
#endif
#if UNITY_EDITOR
            _hashCodePool = new Dictionary<RuntimeTypeHandle, List<IObjectPool>>();
#endif
        }

        public T Get<T>() where T : IObjectPool, new()
        {
            var key = typeof(T).TypeHandle;
            _pool.TryGetValue(key, out var stack);
            if (stack == null)
            {
                stack = new Stack<IObjectPool>();
                _pool.Add(key, stack);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                _pooledObjects.Add(key, new HashSet<IObjectPool>(ReferencePoolObjectComparer.Instance));
#endif
#if UNITY_EDITOR
                _hashCodePool.TryGetValue(key, out var hashList);
                hashList = new List<IObjectPool>();
                _hashCodePool.Add(key, hashList);
#endif
            }

            if (stack.Count > 0)
            {
                var obj = (T)stack.Pop();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (_pooledObjects.TryGetValue(key, out var pooledObjects))
                {
                    pooledObjects.Remove(obj);
                }
#endif

#if UNITY_EDITOR
                UseEditor<T>();
#endif
                return obj;
            }

            var item = new T();
#if UNITY_EDITOR
            _hashCodePool[key]?.Add(item);
            CreateEditor<T>();
#endif
            return item;
        }

        public bool Check<T>(T obj) where T : IObjectPool, new()
        {
#if UNITY_EDITOR
            var key = Type.GetTypeHandle(obj);
            return _hashCodePool.TryGetValue(key, out var hashList) && hashList.Contains(obj);
#else
            return true;
#endif
        }

        public bool Recycle<T>(T obj) where T : IObjectPool, new()
        {
            if (obj == null)
            {
                DebugLogger.LogWarning("Pool池传入的回收对象为空");
                return false;
            }

            var key = Type.GetTypeHandle(obj);
            if (_pool.TryGetValue(key, out var stack))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (!_pooledObjects.TryGetValue(key, out var pooledObjects))
                {
                    pooledObjects = new HashSet<IObjectPool>(ReferencePoolObjectComparer.Instance);
                    _pooledObjects.Add(key, pooledObjects);
                }

                if (CheckRepeatedRecycle(pooledObjects, obj))
                {
                    return false;
                }
#endif

#if UNITY_EDITOR
                if (_hashCodePool.TryGetValue(key, out var hashList))
                {
                    if (!hashList.Contains(obj))
                    {
                        DebugLogger.LogWarning($"回收对象不是通过Pool池创建的{obj.GetType()}");
                        return false;
                    }
                }
#endif

                if (stack.Count < _maxPoolCount)
                {
                    obj.Clear();
                    try
                    {
                        stack.Push(obj);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        pooledObjects.Add(obj);
#endif
                    }
                    catch (Exception e)
                    {
                        DebugLogger.LogWarning($"回收对象出错了,一般情况是在一步操作或发送网络消息中，没有实现值拷贝而使用了引用>>>>{e.Message}");
                        return false;
                    }
#if UNITY_EDITOR
                    RecycleEditor(obj);
#endif
                }
                else
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    pooledObjects.Remove(obj);
#endif
#if UNITY_EDITOR
                    DestroyEditor(obj);
                    hashList?.Remove(obj);
#endif
                    obj.Disposable();
                }

                return true;
            }

            DebugLogger.LogWarning($"发现未使用类对象池创建,但是却使用其回收的的对象:{obj.GetType()},将不会回收");
            return false;
        }

        public void Clear<T>() where T : IObjectPool, new()
        {
            var key = typeof(T).TypeHandle;
            _pool.TryGetValue(key, out var stack);
            if (stack == null) return;
            foreach (var item in stack)
            {
                IDisposable disp = item;
                disp?.Disposable();
            }

            stack.Clear();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_pooledObjects.TryGetValue(key, out var pooledObjects))
            {
                pooledObjects.Clear();
            }
#endif

#if UNITY_EDITOR
            _hashCodePool.TryGetValue(key, out var hashList);
            hashList?.Clear();
#endif
        }

        protected void OnDispose()
        {
            foreach (var pool in _pool)
            {
                if (pool.Value == null) continue;

                foreach (var item in pool.Value)
                {
                    IDisposable disp = item;
                    disp?.Disposable();
                }

                pool.Value.Clear();
            }

            _pool.Clear();
            _pool = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _pooledObjects.Clear();
            _pooledObjects = null;
#endif
#if UNITY_EDITOR
            _hashCodePool.Clear();
            _hashCodePool = null;
            LogCount();
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool CheckRepeatedRecycle<T>(HashSet<IObjectPool> pooledObjects, T obj) where T : IObjectPool
        {
            if (!pooledObjects.Contains(obj)) return false;
            var name = obj.GetType().FullName;
            DebugLogger.LogWarning($"Repeated recycle object: {name}");
            return true;
        }
#endif

#if UNITY_EDITOR
        private Dictionary<string, EditorPoolInfo> _poolInfo;

        private void CheckPoolInfo()
        {
            if (_poolInfo != null) return;
            var obj = new GameObject
            {
                name = "[ObjectPoolsLook]"
            };
            Object.DontDestroyOnLoad(obj);
            var objectPoolsLook = obj.AddComponent<ObjectPoolsLook>();
            _poolInfo = new Dictionary<string, EditorPoolInfo>();
            objectPoolsLook.Init(_poolInfo);
        }

        private void CreateEditor<T>() where T : IObjectPool, new()
        {
            CheckPoolInfo();
            var name = typeof(T).FullName;
            // ReSharper disable once AssignNullToNotNullAttribute
            if (!_poolInfo.TryGetValue(name, out var info))
            {
                info = new EditorPoolInfo();
                info.InitData(name, typeof(T).TypeHandle);
                _poolInfo.Add(name, info);
            }

            info.Use(true);
            info.Create(true);
        }

        private void DestroyEditor<T>(T obj) where T : IObjectPool, new()
        {
            CheckPoolInfo();
            var name = obj.GetType().FullName;
            // ReSharper disable once AssignNullToNotNullAttribute
            if (!_poolInfo.TryGetValue(name, out var info))
            {
                info = new EditorPoolInfo();
                info.InitData(name, typeof(T).TypeHandle);
                _poolInfo.Add(name, info);
            }

            info.Use(false);
            info.Recycle(false);
        }

        private void RecycleEditor<T>(T obj) where T : IObjectPool, new()
        {
            CheckPoolInfo();
            var name = obj.GetType().FullName;
            // ReSharper disable once AssignNullToNotNullAttribute
            if (!_poolInfo.TryGetValue(name, out var info))
            {
                info = new EditorPoolInfo();
                info.InitData(name, typeof(T).TypeHandle);
                _poolInfo.Add(name, info);
            }

            info.Recycle(true);
            info.Use(false);
        }

        private void UseEditor<T>() where T : IObjectPool, new()
        {
            CheckPoolInfo();
            _poolInfo ??= new Dictionary<string, EditorPoolInfo>();
            var name = typeof(T).FullName;
            // ReSharper disable once AssignNullToNotNullAttribute
            if (!_poolInfo.TryGetValue(name, out var info))
            {
                info = new EditorPoolInfo();
                info.InitData(name, typeof(T).TypeHandle);
                _poolInfo.Add(name, info);
            }

            info.Use(true);
            info.Recycle(false);
        }

        private void LogCount()
        {
            foreach (var info in _poolInfo)
            {
                DebugLogger.LogWarning(
                    $"{info.Key}>>>>>>>从类对象池中创建了{info.Value.CreateCount}个,回收个数为{info.Value.RecycelCount}个,使用个数为{info.Value.UseCount}个");
                if (info.Value.CreateCount > info.Value.RecycelCount)
                {
                    DebugLogger.LogWarning($"{info.Key}>>>>>>>内存泄露");
                }
            }
        }

#endif
    }
}
