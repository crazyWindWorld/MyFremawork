using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fuel.Log;
using Fuel.Pools;
using Fuel.Singleton;
using HotFarmework.AssetManager;
using HotFramework.AssetManager.AssetsPools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Fuel.AssetManager.AssetsPools
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class GameObjectPools : Singleton<GameObjectPools>
    {
        private Dictionary<string, Dictionary<string, GameObjectPool>> _groupGameObjectPools;
        /// <summary>
        /// 实例化对象修改名称的字典
        /// </summary>
        private Dictionary<int, string> _changeNameMap;

        private Dictionary<string, Dictionary<long, LoadCallBack<GameObject>>> _loadCallBackMap;

        /// <summary>
        /// checkNewLoad
        /// </summary>
        private Dictionary<string, Dictionary<long, long>> _loadIndexCheckMap;

        private long _loadIndex;

        public Transform GameObjectPoolParent;


        protected  void RegistrationLife()
        {
         
        }

        protected override void Init()
        {
            _loadCallBackMap = new Dictionary<string, Dictionary<long, LoadCallBack<GameObject>>>();
            _groupGameObjectPools = new Dictionary<string, Dictionary<string, GameObjectPool>>();
            _loadIndexCheckMap = new Dictionary<string, Dictionary<long, long>>();
            _changeNameMap = new Dictionary<int, string>();
            GameObjectPoolParent = new GameObject().transform;
            Object.DontDestroyOnLoad(GameObjectPoolParent);
            GameObjectPoolParent.name = "[GameObjectPools]";
            GameObjectPoolParent.gameObject.SetActive(false);
        }

        public async UniTask<GameObject> GetAsync(string path, string groupName = "")
        {
            if (string.IsNullOrEmpty(groupName)) groupName = AssetsManager.NomalAssetGroupName;
            if (_groupGameObjectPools.TryGetValue(groupName, out var gameObjectPools))
            {
                if (gameObjectPools.TryGetValue(path, out var pool))
                {
                    GameObject go = await pool.GetAsync(path, groupName);
                    return go;
                }
                else
                {
                    pool = ObjectPools.Instance.Get<GameObjectPool>();
                    gameObjectPools.Add(path, pool);
                    GameObject go = await pool.GetAsync(path, groupName);
                    return go;
                }
            }
            else
            {
                gameObjectPools = new Dictionary<string, GameObjectPool>();
                GameObjectPool pool = ObjectPools.Instance.Get<GameObjectPool>();
                _groupGameObjectPools.Add(groupName, gameObjectPools);
                gameObjectPools.Add(path, pool);
                GameObject go = await pool.GetAsync(path, groupName);
                return go;
            }
        }

        private async void GetAsyncAction(string path, long index, string groupName, Action<long, GameObject, string> action)
        {
            if (_groupGameObjectPools.TryGetValue(groupName, out var gameObjectPools))
            {
                if (gameObjectPools.TryGetValue(path, out var pool))
                {
                    var go = await pool.GetAsync(path, groupName);
                    action.Invoke(index, go, groupName);
                }
                else
                {
                    pool = ObjectPools.Instance.Get<GameObjectPool>();
                    gameObjectPools.Add(path, pool);
                    var go = await pool.GetAsync(path, groupName);
                    action.Invoke(index, go, groupName);
                }
            }
            else
            {
                gameObjectPools = new Dictionary<string, GameObjectPool>();
                var pool = ObjectPools.Instance.Get<GameObjectPool>();
                _groupGameObjectPools.Add(groupName, gameObjectPools);
                gameObjectPools.Add(path, pool);
                var go = await pool.GetAsync(path, groupName);
                action.Invoke(index, go, groupName);
            }
        }

        internal void GetAsyncAction(string path, long code, Action<GameObject> action, string groupName = "")
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
            var loadData = ObjectPools.Instance.Get<LoadCallBack<GameObject>>();
            loadData.LoadIndex = _loadIndex;
            loadData.Action = action;
            loadData.Code = code;
            if (_loadCallBackMap.ContainsKey(groupName) == false)
                _loadCallBackMap[groupName] = new Dictionary<long, LoadCallBack<GameObject>>();
            _loadCallBackMap[groupName].Add(loadData.LoadIndex, loadData);
            GetAsyncAction(path, loadData.LoadIndex, groupName, LoadCallBack);
        }

        private void LoadCallBack(long index, GameObject go, string groupName)
        {
            if (_loadCallBackMap.TryGetValue(groupName, out var callBacks))
            {
                if (callBacks.TryGetValue(index, out var callback))
                {
                    _loadIndexCheckMap[groupName].TryGetValue(callback.Code, out var loadIndex);
                    if (loadIndex == index)
                    {
                        _loadIndexCheckMap[groupName].Remove(callback.Code);
                        callback.Action?.Invoke(go);
                        ObjectPools.Instance.Recycle(callback);
                        callBacks.Remove(index);
                        return;
                    }
                    ObjectPools.Instance.Recycle(callback);
                    callBacks.Remove(index);
                }
            }

            if (go != null)
            {
                Recycle(go, groupName);
            }
        }

        public GameObject GetSysnByPrefab(GameObject prefab, string groupName = "")
        {
            ////if (string.IsNullOrEmpty(groupName)) groupName = AssetsManager.Instance.NomalAssetGroupName;
            if (_groupGameObjectPools.TryGetValue(groupName, out var gameObjectPools))
            {
                if (gameObjectPools.TryGetValue(prefab.GetHashCode().ToString(), out var pool))
                {
                    var go = pool.GetSyncByPrefab();
                    return go;
                }
                else
                {
                    pool = ObjectPools.Instance.Get<GameObjectPool>();
                    gameObjectPools.Add(prefab.GetHashCode().ToString(), pool);
                    pool.InitByPrefab(prefab);
                    var go = pool.GetSyncByPrefab();
                    return go;
                }
            }
            else
            {
                gameObjectPools = new Dictionary<string, GameObjectPool>();
                GameObjectPool pool = ObjectPools.Instance.Get<GameObjectPool>();
                _groupGameObjectPools.Add(groupName, gameObjectPools);
                gameObjectPools.Add(prefab.GetHashCode().ToString(), pool);
                pool.InitByPrefab(prefab);
                var go = pool.GetSyncByPrefab();
                return go;
            }
        }

        public void RecycleByPrefab(GameObject go, GameObject prefab, string groupName = "")
        {
            if (go == null || prefab == null)
            {
                DebugLogger.LogWarning("回收对象:{0}是否为空{1},回收时传的模版{2}是否为空{3}", go, go == null, prefab, prefab == null);
                return;
            }
            int instanceId = go.GetInstanceID();
            if (_changeNameMap.Remove(instanceId, out var name))
            {
                go.name = name;
            }
            ////if (string.IsNullOrEmpty(groupName)) groupName = AssetsManager.Instance.NomalAssetGroupName;
            if (_groupGameObjectPools.TryGetValue(groupName, out var gameObjectPools))
            {
                if (gameObjectPools.TryGetValue(prefab.GetHashCode().ToString(), out var pool))
                {
                    pool.Recycle(go);
                    return;
                }
            }
            Object.DestroyImmediate(go);
        }

        public GameObject GetSync(string path, string groupName = "")
        {
            if (string.IsNullOrEmpty(groupName)) groupName = AssetsManager.NomalAssetGroupName;
            if (_groupGameObjectPools.TryGetValue(groupName, out var gameObjectPools))
            {
                if (gameObjectPools.TryGetValue(path, out var pool))
                {
                    GameObject go = pool.GetSync(path, groupName);
                    return go;
                }
                else
                {
                    pool = ObjectPools.Instance.Get<GameObjectPool>();
                    gameObjectPools.Add(path, pool);
                    GameObject go = pool.GetSync(path, groupName);
                    return go;
                }
            }
            else
            {
                gameObjectPools = new Dictionary<string, GameObjectPool>();
                GameObjectPool pool = ObjectPools.Instance.Get<GameObjectPool>();
                _groupGameObjectPools.Add(groupName, gameObjectPools);
                gameObjectPools.Add(path, pool);
                GameObject go = pool.GetSync(path, groupName);
                return go;
            }
        }

        public void Recycle(GameObject go, string groupName = "")
        {
            if (go == null) return;
            int instanceID = go.GetInstanceID();
            if (!_changeNameMap.Remove(instanceID, out var name))
            {
                name = go.name;
            }

            if (string.IsNullOrEmpty(groupName)) groupName = AssetsManager.NomalAssetGroupName;
            if (_groupGameObjectPools.TryGetValue(groupName, out var gameObjectPools))
            {
                if (gameObjectPools.TryGetValue(name, out var pool))
                {
                    pool.Recycle(go);
                    return;
                }
            }
            Object.DestroyImmediate(go);
        }

        public void RecycleByGroup(string groupName = "")
        {
            if (string.IsNullOrEmpty(groupName)) groupName = AssetsManager.NomalAssetGroupName;
            if (!_groupGameObjectPools.TryGetValue(groupName, out var gameObjectPools)) return;
            foreach (var pool in gameObjectPools.Values)
            {
                var createList = pool.GetCreateList();
                for (int i = 0, count = createList.Count; i < count; i++)
                {
                    var instatnceID = createList[i].GetInstanceID();
                    if (_changeNameMap.ContainsKey(instatnceID))
                    {
                        _changeNameMap.Remove(instatnceID);
                    }
                }
                pool.RecycleAll();
            }
        }

        /// <summary>
        /// 修改gameObject名称
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        public void ChangeObjectName(GameObject obj, string name)
        {
            if (obj == null) return;
            var instanceID = obj.GetInstanceID();
            if (!_changeNameMap.ContainsKey(instanceID))
            {
                _changeNameMap.Add(instanceID, obj.name);
            }
            obj.name = name;
        }

        /// <summary>
        /// 删除对象池，通过groupName
        /// </summary>
        /// <param name="groupName"></param>
        public void DestroyByGroup(string groupName = "")
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

            if (!_groupGameObjectPools.TryGetValue(groupName, out var gameObjectPools)) return;
            foreach (var pool in gameObjectPools.Values)
            {
                var createList = pool.GetCreateList();
                for (int i = 0, count = createList.Count; i < count; i++)
                {
                    var instatnceID = createList[i].GetInstanceID();
                    if (_changeNameMap.ContainsKey(instatnceID))
                    {
                        _changeNameMap.Remove(instatnceID);
                    }
                }
                ObjectPools.Instance.Recycle(pool);
            }
            _groupGameObjectPools.Remove(groupName);
        }

        public void StopLoadByGroup(string groupName = "")
        {
            if (string.IsNullOrEmpty(groupName)) groupName = AssetsManager.NomalAssetGroupName;
            if (_groupGameObjectPools.TryGetValue(groupName, out var gameObjectPools))
            {
                foreach (var pool in gameObjectPools.Values)
                {
                    pool.StopLoad();
                }
            }

            if (!_loadCallBackMap.TryGetValue(groupName, value: out var loadCallBacks)) return;
            foreach (var loadCallBack in loadCallBacks)
            {
                ObjectPools.Instance.Recycle(loadCallBack.Value);
            }
            loadCallBacks.Clear();

            if (_loadIndexCheckMap.TryGetValue(groupName, out var loadIndexCheck))
            {
                loadIndexCheck.Clear();
            }
        }


        protected void OnDestroy()
        {
            if (_groupGameObjectPools == null) return;
            foreach (var pool in _groupGameObjectPools.Values.SelectMany(pools => pools.Values))
            {
                ObjectPools.Instance.Recycle(pool);
            }
            foreach (var pool in _loadCallBackMap.Values.SelectMany(pools => pools.Values))
            {
                ObjectPools.Instance.Recycle(pool);
            }
            _groupGameObjectPools?.Clear();
            _changeNameMap?.Clear();
            _loadCallBackMap.Clear();
            _loadIndexCheckMap.Clear();
        }

        protected void OnDispose()
        {
            if (GameObjectPoolParent != null)
                Object.DestroyImmediate(GameObjectPoolParent.gameObject);
            GameObjectPoolParent = null;
            _groupGameObjectPools = null;
            _loadCallBackMap = null;
            _loadIndexCheckMap = null;
            _changeNameMap = null;
        }
    }

}

