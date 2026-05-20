using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HotFarmework.AssetManager;
using Fuel.Pools;
using Fuel.Singleton;
using HotFramework.AssetManager.AssetsPools;

namespace Fuel.AssetManager.AssetsPools
{
    internal class LoadCallBackBase : IObjectPool
    {
        public long LoadIndex;
        public long Code;
        public virtual void Clear()
        {
            LoadIndex = -1;
            Code = -1;
        }

        public virtual void Disposable()
        {
            Clear();
        }
    }
    
    internal class LoadCallBack<T> : LoadCallBackBase
    {
        public Action<T> Action;
        public override void Clear()
        {
            base.Clear();
            Action = null;
        }
    }

    internal class InstantiatePools<T> : Singleton<InstantiatePools<T>> where T : UnityEngine.Object
    {
        private long _loadIndex;
        
        private Dictionary<string, Dictionary<string, OtherPool<T>>> _groupPoos;
        
        private Dictionary<string, Dictionary<long,LoadCallBack<T>>> _loadCallBackMap;
        /// <summary>
        /// checkNewLoad
        /// </summary>
        private Dictionary<string, Dictionary<long, long>> _loadIndexCheckMap;

        protected  void RegistrationLife()
        {
           /*  AssetsGroupManager.Instance.DestroyEvent.AddListener(Destroy);
            AssetsGroupManager.Instance.DisposeEvent.AddListener(Dispose); */
        }
        protected override void Init()
        {
            base.Init();
            _groupPoos = new Dictionary<string, Dictionary<string, OtherPool<T>>>();
            _loadCallBackMap = new Dictionary<string, Dictionary<long, LoadCallBack<T>>>();
            _loadIndexCheckMap = new Dictionary<string, Dictionary<long, long>>();
        }

        internal async Task<T> GetAsync(string path, string groupName = "")
        {
            if (string.IsNullOrEmpty(groupName)) groupName = AssetsManager.NomalAssetGroupName;
            if (_groupPoos.TryGetValue(groupName, out var matPools))
            {
                if (matPools.TryGetValue(path, out var pool))
                {
                    var mat = await pool.GetAsync(path, groupName);
                    return mat;
                }
                else
                {
                    pool = ObjectPools.Instance.Get<OtherPool<T>>();
                    matPools.Add(path, pool);
                    var mat = await pool.GetAsync(path, groupName);
                    return mat;
                }
            }
            else
            {
                matPools = new Dictionary<string, OtherPool<T>>();
                var pool = ObjectPools.Instance.Get<OtherPool<T>>();
                _groupPoos.Add(groupName, matPools);
                matPools.Add(path, pool);
                var mat = await pool.GetAsync(path, groupName);
                return mat;
            }
        }
        
        internal void GetAsyncAction(string path, long code,Action<T> action, string groupName = "")
        {
            if (string.IsNullOrEmpty(groupName)) groupName = AssetsManager.NomalAssetGroupName;
            _loadIndex++;
            if (_loadIndexCheckMap.TryGetValue(groupName, out var callBacks))
            {
                callBacks[code] = _loadIndex;
            }
            else
            {
                callBacks = new Dictionary<long, long> { { code, _loadIndex } };
                _loadIndexCheckMap[groupName] = callBacks;
            }
            var loadData = ObjectPools.Instance.Get<LoadCallBack<T>>();
            loadData.LoadIndex = _loadIndex;
            loadData.Action = action;
            loadData.Code = code;
            if (_loadCallBackMap.ContainsKey(groupName) == false)
                _loadCallBackMap[groupName] = new Dictionary<long, LoadCallBack<T>>();
            _loadCallBackMap[groupName].Add(loadData.LoadIndex,loadData);
            GetAsyncAction(path, loadData.LoadIndex, groupName,LoadCallBack);
        }
        
        private async void GetAsyncAction(string path, long index, string groupName, Action<long,T,string> action)
        {
            if (_groupPoos.TryGetValue(groupName, out var materialPools))
            {
                if (materialPools.TryGetValue(path, out var pool))
                {
                    var mat = await pool.GetAsync(path, groupName);
                    action.Invoke(index,mat,groupName);
                }
                else
                {
                    pool = ObjectPools.Instance.Get<OtherPool<T>>();
                    materialPools.Add(path, pool);
                    var mat = await pool.GetAsync(path, groupName);
                    action.Invoke(index,mat,groupName);
                }
            }
            else
            {
                materialPools = new Dictionary<string, OtherPool<T>>();
                var pool = ObjectPools.Instance.Get<OtherPool<T>>();
                materialPools.Add(path, pool);
                _groupPoos.Add(groupName, materialPools);
                var mat = await pool.GetAsync(path, groupName);
                action.Invoke(index,mat,groupName);
            }
        }
        
        private void LoadCallBack(long index, T mat, string groupName)
        {
            if (_loadCallBackMap.TryGetValue(groupName, out var callBacks))
            {
                if (callBacks.TryGetValue(index, out var callback))
                {
                    _loadIndexCheckMap[groupName].TryGetValue(callback.Code, out var loadIndex);
                    if (loadIndex == index)
                    {
                        _loadIndexCheckMap[groupName].Remove(callback.Code);
                        callback.Action?.Invoke(mat);
                        ObjectPools.Instance.Recycle(callback);
                        callBacks.Remove(index);
                        return;
                    }
                    ObjectPools.Instance.Recycle(callback);
                    callBacks.Remove(index);
                }
            }
            if (mat != null)
            {
                Recycle(mat);
            }
        }
        

        internal T GetSync(string path, string groupName = "")
        {
            if (string.IsNullOrEmpty(groupName)) groupName = AssetsManager.NomalAssetGroupName;
            if (_groupPoos.TryGetValue(groupName, out var matPools))
            {
                if (matPools.TryGetValue(path, out var pool))
                {
                    var mat = pool.GetSync(path, groupName);
                    return mat;
                }
                else
                {
                    pool = ObjectPools.Instance.Get<OtherPool<T>>();
                    var mat = pool.GetSync(path, groupName);
                    matPools.Add(path, pool);
                    return mat;
                }
            }
            else
            {
                matPools = new Dictionary<string, OtherPool<T>>();
                var pool = ObjectPools.Instance.Get<OtherPool<T>>();
                _groupPoos.Add(groupName, matPools);
                matPools.Add(path, pool);
                var mat = pool.GetSync(path, groupName);
                return mat;
            }
        }

        internal void Recycle(T mat, string groupName = "")
        {
            if (mat == null) return;
            if (string.IsNullOrEmpty(groupName)) groupName = AssetsManager.NomalAssetGroupName;
            if (_groupPoos.TryGetValue(groupName, out var matPools))
            {
                if (matPools.TryGetValue(mat.name, out var pool))
                {
                    pool.Recycle(mat);
                    return;
                }
            }
            UnityEngine.Object.DestroyImmediate(mat);
        }

        internal void RecycleByGroup(string groupName = "")
        {
            if (string.IsNullOrEmpty(groupName)) groupName = AssetsManager.NomalAssetGroupName;
            if (!_groupPoos.TryGetValue(groupName, out var matPools)) return;
            foreach (var pool in matPools.Values)
            {
                pool.RecycleAll();
            }
        }

        internal void DestroyByGroup(string groupName = "")
        {
            if (string.IsNullOrEmpty(groupName)) groupName = AssetsManager.NomalAssetGroupName;
            if (_loadCallBackMap.TryGetValue(groupName, value: out var loadCallBacks))
            {
                foreach (var loadCallBack in loadCallBacks)
                {
                    ObjectPools.Instance.Recycle(loadCallBack.Value);
                }
                loadCallBacks.Clear();
            }

            if (_loadIndexCheckMap.TryGetValue(groupName, out var loadIndexCheck))
            {
                loadIndexCheck.Clear();
            }
            
            if (!_groupPoos.TryGetValue(groupName, out var matPools)) return;
            foreach (var pool in matPools.Values)
            {
                ObjectPools.Instance.Recycle(pool);
            }
            _groupPoos.Remove(groupName);
        }

        protected  void OnDestroy()
        {
            if (_groupPoos == null) return;
            foreach (var pool in _groupPoos.Values.SelectMany(pools => pools.Values))
            {
                ObjectPools.Instance.Recycle(pool);
            }
            foreach (var pool in _loadCallBackMap.Values.SelectMany(pools=>pools.Values))
            {
                ObjectPools.Instance.Recycle(pool);
            }
            _loadCallBackMap.Clear();
            _loadIndexCheckMap.Clear();
            _groupPoos.Clear();
        }

        protected void OnDispose()
        {
            _groupPoos = null;
            _loadCallBackMap = null;
            _loadIndexCheckMap= null;
        }
    }

    internal class ReferencePools<T> : Singleton<ReferencePools<T>> where T : UnityEngine.Object
    {
        private Dictionary<string, Dictionary<string, T>> _groupPools;
        
        private Dictionary<string, Dictionary<long,LoadCallBack<T>>> _loadCallBackMap;
        /// <summary>
        /// checkNewLoad
        /// </summary>
        private Dictionary<string, Dictionary<long, long>> _loadIndexCheckMap;
        
        private long _loadIndex;
        
        protected  void RegistrationLife()
        {
            
        }

        protected override void Init()
        {
            base.Init();
            _groupPools = new Dictionary<string, Dictionary<string, T>>();
            _loadCallBackMap = new Dictionary<string, Dictionary<long, LoadCallBack<T>>>();
            _loadIndexCheckMap = new Dictionary<string, Dictionary<long, long>>();
        }

        internal async Task<T> GetAsync(string path, string groupName = "")
        {
            if (string.IsNullOrEmpty(groupName)) groupName = AssetsManager.NomalAssetGroupName;
            if (_groupPools.TryGetValue(groupName, out var spritePool))
            {
                if (spritePool.TryGetValue(path, out var obj))
                {
                    return obj;
                }
            }
            else
            {
                spritePool = new Dictionary<string, T>();
                _groupPools.Add(groupName, spritePool);
            }
            var spriteLoad = await LoadAsync(path, groupName);
            return spritePool.TryAdd(path, spriteLoad) ? spriteLoad : spritePool[path];
        }

        internal void GetAsyncAction(string path, long code,Action<T> action, string groupName = "")
        {
            if (string.IsNullOrEmpty(groupName)) groupName = AssetsManager.NomalAssetGroupName;
            _loadIndex++;
            if (_loadIndexCheckMap.TryGetValue(groupName, out var callBacks))
            {
                callBacks[code] = _loadIndex;
            }
            else
            {
                callBacks = new Dictionary<long, long> { { code, _loadIndex } };
                _loadIndexCheckMap[groupName] = callBacks;
            }
            var loadData = ObjectPools.Instance.Get<LoadCallBack<T>>();
            loadData.LoadIndex = _loadIndex;
            loadData.Action = action;
            loadData.Code = code;
            if (_loadCallBackMap.ContainsKey(groupName) == false)
                _loadCallBackMap[groupName] = new Dictionary<long, LoadCallBack<T>>();
            _loadCallBackMap[groupName].Add(loadData.LoadIndex,loadData);
            GetAsyncAction(path, loadData.LoadIndex, groupName,LoadCallBack);
        }
        
        private async void GetAsyncAction(string path, long index, string groupName, Action<long,T,string> action)
        {
            if (_groupPools.TryGetValue(groupName, out var spritePool))
            {
                if (spritePool.TryGetValue(path, out var sprite))
                {
                    action.Invoke(index,sprite,groupName);
                }
            }
            else
            {
                spritePool = new Dictionary<string, T>();
                _groupPools.Add(groupName, spritePool);
            }
            var spriteLoad = await LoadAsync(path, groupName);
            action.Invoke(index, spritePool.TryAdd(path, spriteLoad) ? spriteLoad : spritePool[path], groupName);
        }
        
        private void LoadCallBack(long index, T obj, string groupName)
        {
            if (!_loadCallBackMap.TryGetValue(groupName, out var callBacks)) return;
            if (!callBacks.TryGetValue(index, out var callback)) return;
            _loadIndexCheckMap[groupName].TryGetValue(callback.Code, out var loadIndex);
            if (loadIndex == index)
            {
                _loadIndexCheckMap[groupName].Remove(callback.Code);
                callback.Action?.Invoke(obj);
                ObjectPools.Instance.Recycle(callback);
                callBacks.Remove(index);
                return;
            }
            ObjectPools.Instance.Recycle(callback);
            callBacks.Remove(index);
        }

        internal T GetSync(string path, string groupName = "")
        {
            if (string.IsNullOrEmpty(groupName)) groupName = AssetsManager.NomalAssetGroupName;
            if (_groupPools.TryGetValue(groupName, out var pool))
            {
                if (pool.TryGetValue(path, out var obj))
                {
                    return obj;
                }

                obj = LoadSync(path,groupName);
                pool.Add(path, obj);
                return obj;
            }
            else
            {
                pool = new Dictionary<string, T>();
                _groupPools.Add(groupName, pool);
                var obj = LoadSync(path,groupName);
                pool.Add(path, obj);
                return obj;
            }
        }

        protected virtual T LoadSync(string path, string groupName)
        {
            return AssetsLoadManager.Instance.InternalLoadSync<T>(path, groupName);
        }

        protected virtual async UniTask<T> LoadAsync(string path, string groupName)
        {
            return await AssetsLoadManager.Instance.InternalLoadAsync<T>(path, groupName);
        }


        internal void DestroyByGroup(string groupName)
        {
            if (string.IsNullOrEmpty(groupName)) groupName = AssetsManager.NomalAssetGroupName;
            
            if (_loadCallBackMap.TryGetValue(groupName, value: out var loadCallBacks))
            {
                foreach (var loadCallBack in loadCallBacks)
                {
                    ObjectPools.Instance.Recycle(loadCallBack.Value);
                }
                loadCallBacks.Clear();
            }

            if (_loadIndexCheckMap.TryGetValue(groupName, out var loadIndexCheck))
            {
                loadCallBacks.Clear();
            }
            
            if (_groupPools != null && _groupPools.TryGetValue(groupName, out var pool))
            {
                pool.Clear();
            }
        }

        protected void OnDestroy()
        {
            foreach (var pool in _loadCallBackMap.Values.SelectMany(pools=>pools.Values))
            {
                ObjectPools.Instance.Recycle(pool);
            }
            _loadCallBackMap.Clear();
            _loadIndexCheckMap.Clear();
            _groupPools?.Clear();
        }

        protected  void OnDispose()
        {
            _loadCallBackMap = null;
            _loadIndexCheckMap = null;
            _groupPools = null;
        }
    }

}
