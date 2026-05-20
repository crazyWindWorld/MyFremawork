using System.Collections.Generic;
using System.Threading.Tasks;
using Fuel.Pools;
using UnityEngine;

namespace Fuel.AssetManager.AssetsPools
{
    internal class OtherPool<T>  : IObjectPool  where T : Object
    {
        private bool _isInit;
        private string _groupName = string.Empty;
        private string _assetName = string.Empty;
        private Stack<T> _pool;
        private T _base;
        private const int MaxPoolCount = 100;

        private readonly List<T> _createList;
        private readonly List<T> _useList;


        public OtherPool()
        {
            _pool = new Stack<T>();
            _createList = new List<T>();
            _useList = new List<T>();
        }

        internal async Task<bool> InitAsync(string assetName, string groupName)
        {
            _assetName = assetName;
            _groupName = groupName;
            _base = await AssetsLoadManager.Instance.InternalLoadAsync<T>(assetName, groupName);
            return true;
        }

        internal bool InitSync(string assetName, string groupName)
        {
            _assetName = assetName;
            _groupName = groupName;
            _base = AssetsLoadManager.Instance.InternalLoadSync<T>(assetName, groupName);
            return true;
        }

        internal async Task<T> GetAsync(string assetName, string groupName)
        {
            if (!_isInit)
                _isInit = await InitAsync(assetName, groupName);
            if (_pool.Count > 0)
            {
                T mat = _pool.Pop();
                if (mat == null)
                {
                    _createList.Remove(mat);
                    mat = await GetAsync(assetName, groupName);
                    return mat;
                }
                _useList.Add(mat);
                return mat;
            }
            else
            {
                T mat = Object.Instantiate(_base);
                mat.name = assetName;
                _createList.Add(mat);
                _useList.Add(mat);
                return mat;
            }
        }

        internal T GetSync(string assetName, string groupName)
        {
            if (!_isInit)
                _isInit = InitSync(assetName, groupName);
            if (_pool.Count > 0)
            {
                T mat = _pool.Pop();
                if (mat == null)
                {
                    _createList.Remove(mat);
                    mat = GetSync(assetName, groupName);
                    return mat;
                }
                _useList.Add(mat);
                return mat;
            }
            else
            {
                T mat = Object.Instantiate(_base);
                mat.name = assetName;
                _createList.Add(mat);
                _useList.Add(mat);
                return mat;
            }
        }

        internal void Recycle(T mat)
        {
            if (mat == null || _pool.Contains(mat)) return;
            if (_pool.Count >= MaxPoolCount)
            {
                _createList.Remove(mat);
                _useList.Remove(mat);
                Object.DestroyImmediate(mat);
                return;
            }
            _useList.Remove(mat);
            _pool.Push(mat);
        }

        internal void RecycleAll()
        {
            for (var i = _createList.Count - 1; i >= 0; i--)
            {
                Recycle(_createList[i]);
            }
        }


        public void Clear()
        {
            for (var i = _createList.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(_createList[i]);
            }
            _useList.Clear();
            _createList.Clear();
            _pool.Clear();
            _base = null;
            AssetsLoadManager.Instance.Release(_assetName, _groupName);
            _groupName = string.Empty;
            _assetName = string.Empty;
            _isInit = false;
        }

        public void Disposable()
        {
            Clear();
        }
    }
}

