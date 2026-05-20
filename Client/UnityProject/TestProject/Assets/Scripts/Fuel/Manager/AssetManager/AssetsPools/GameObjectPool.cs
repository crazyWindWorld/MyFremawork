using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fuel.Pools;
using UnityEngine;
using YooAsset;

namespace Fuel.AssetManager.AssetsPools
{
    internal class GameObjectPool : IObjectPool
    {
        private bool _isInit;
        private string _groupName = string.Empty;
        private string _assetName = string.Empty;
        private Stack<GameObject> _pool;
        private AssetHandle _baseHandle;
        private const int MaxPoolCount = 100;
        private bool _isRuningAwait;
        private GameObject _nomalPrefab;

        private List<GameObject> _createList;
        private List<GameObject> _useList;

        public GameObjectPool()
        {
            _createList = new List<GameObject>();
            _useList = new List<GameObject>();
            _pool = new Stack<GameObject>();
        }


        private async UniTask<bool> InitAsync(string assetName, string groupName)
        {
            _isRuningAwait = true;
            _assetName = assetName;
            _groupName = groupName;
            _baseHandle = await AssetsLoadManager.Instance.LoadAsyncHandle<GameObject>(assetName, groupName);
            if (_isRuningAwait != true)
            {
                _baseHandle?.Release();
                _baseHandle = null;
                return false;
            }
            return true;
        }

        internal bool InitSync(string assetName, string groupName)
        {
            _isRuningAwait = true;
            _assetName = assetName;
            _groupName = groupName;
            _pool = new Stack<GameObject>();
            _baseHandle = AssetsLoadManager.Instance.LoadSyncHandle<GameObject>(assetName, groupName);
            return true;
        }


        internal async UniTask<GameObject> GetAsync(string assetName, string groupName)
        {
            if (!_isInit)
            {
                _isInit = await InitAsync(assetName, groupName);
                if (!_isInit)
                    return null;
            }

           
            if (_pool.Count > 0)
            {
                GameObject go = _pool.Pop();
                if (go == null)
                {
                    _createList.Remove(go);
                    go = await GetAsync(assetName, groupName);
                    return go;
                }
                _useList.Add(go);
                return go;
            }
            else
            {
                InstantiateOperation instantiate = _baseHandle.InstantiateAsync();
                await instantiate.ToUniTask();
                if (!_isRuningAwait)
                    return null;
                GameObject go = instantiate.Result;
                _createList.Add(go);
                go.name = assetName;
                instantiate.Cancel();
                _useList.Add(go);
                return go;
            }
        }
        
        internal void InitByPrefab(GameObject prefab)
        {
            _nomalPrefab = prefab;
            _assetName = prefab.name;
            _isInit = true;
        }
        
        internal GameObject GetSyncByPrefab()
        {
            if (!_isInit)
                return null;
            if (_pool.Count > 0)
            {
                var go = _pool.Pop();
                if (go == null)
                {
                    _createList.Remove(go);
                    go = GetSyncByPrefab();
                    return go;
                }
                _useList.Add(go);
                return go;
            }
            else
            {
                var go = Object.Instantiate(_nomalPrefab);
                go.name = _nomalPrefab.name;
                _createList.Add(go);
                _useList.Add(go);
                return go;
            }
        }

        internal GameObject GetSync(string assetName, string groupName)
        {
            if (!_isInit)
                _isInit = InitSync(assetName, groupName);
            if (_pool.Count > 0)
            {
                GameObject go = _pool.Pop();
                if (go == null)
                {
                    _createList.Remove(go);
                    go = GetSync(assetName, groupName);
                    return go;
                }
                _useList.Add(go);
                return go;
            }
            else
            {
                GameObject go = _baseHandle.InstantiateSync();
                go.name = assetName;
                _createList.Add(go);
                _useList.Add(go);
                return go;
            }
        }

        internal void Recycle(GameObject go)
        {
            if (go == null || _pool.Contains(go)) return;
            if (_pool.Count >= MaxPoolCount)
            {
                _createList.Remove(go);
                _useList.Remove(go);
                Object.DestroyImmediate(go);
                return;
            }
            _useList.Remove(go);
            go.transform.SetParent(GameObjectPools.Instance.GameObjectPoolParent);
            go.name = _assetName;
            _pool.Push(go);
        }

        internal void RecycleAll()
        {
            for (int i = _createList.Count - 1; i >= 0; i--)
            {
                Recycle(_createList[i]);
            }
        }

        internal List<GameObject> GetCreateList()
        {
            return _createList;
        }
        internal void StopLoad()
        {
            _isRuningAwait = false;
        }

        public void Clear()
        {
            StopLoad();
            for (int i = _createList.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(_createList[i]);
            }
            _useList.Clear();
            _createList.Clear();
            _pool.Clear();
            _baseHandle = null;
            AssetsLoadManager.Instance.Release(_assetName, _groupName);
            _assetName = string.Empty;
            _groupName = string.Empty;
            _isInit = false;
        }

        public void Disposable()
        {
            Clear();
        }
    }
}

