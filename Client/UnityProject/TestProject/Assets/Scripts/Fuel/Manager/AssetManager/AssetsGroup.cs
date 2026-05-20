using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using HotFarmework.AssetManager;
using Fuel.Pools;
using UnityEngine;
using YooAsset;

namespace Fuel.AssetManager
{
    internal class AssetsGroup : IObjectPool
    {
        private Dictionary<string, AssetHandle> _assetHandles;
        private Dictionary<string, SubAssetsHandle> _subAssetHandles;
        private string _groupName;
        private bool _isRuningAwait;
        internal void Init(string groupName)
        {
            _isRuningAwait = true;
            _groupName = groupName;
            _assetHandles = new Dictionary<string, AssetHandle>();
            _subAssetHandles = new Dictionary<string, SubAssetsHandle>();
        }


        internal void Release(string path)
        {
            if (_assetHandles.TryGetValue(path, out var assetHandle))
            {
                assetHandle.Release();
                _assetHandles.Remove(path);
                return;
            }

            if (!_subAssetHandles.TryGetValue(path, out var subAssetsHandle)) return;
            subAssetsHandle.Release();
            _subAssetHandles.Remove(path);
        }

        internal void StopLoad()
        {
            _isRuningAwait = false;
        }


        public void Clear()
        {
            StopLoad();
            foreach (var handle in _assetHandles.Values) { handle.Release(); }
            foreach (var subAssetsHandle in _subAssetHandles.Values) { subAssetsHandle.Release(); }
            _assetHandles.Clear();
            _subAssetHandles.Clear();
        }

        public void Disposable()
        {
            Clear();
        }

        #region 同步加载
        internal T LoadSync<T>(string path) where T : UnityEngine.Object
        {
            if (_assetHandles.TryGetValue(path, out var value))
            {
                return value.GetAssetObject<T>();
            }

            var obj = AssetsManager.Instance.Load<T>(path, out AssetHandle handle);
            if (handle == null) return null;
            if (_assetHandles.TryAdd(path, handle)) return obj;
            handle.Release();
            obj = _assetHandles[path].GetAssetObject<T>();
            return obj;
        }

        internal UnityEngine.Object LoadSync(string path,Type type)
        {
            if (_assetHandles.TryGetValue(path, out var value))
            {
                return value.AssetObject;
            }

            var obj = AssetsManager.Instance.Load(path, type, out AssetHandle handle);
            if (handle == null) return null;
            if (_assetHandles.TryGetValue(path, out var assetHandle))
            {
                handle.Release();
                obj = assetHandle.AssetObject;
            }
            else
            {
                _assetHandles.Add(path, handle);
            }
            return obj;
        }

        internal AssetHandle LoadSyncHandle<T>(string path) where T : UnityEngine.Object
        {
            if (_assetHandles.TryGetValue(path, out var value))
            {
                return value;
            }

            AssetsManager.Instance.Load<T>(path, out var handle);
            if (handle == null) return null;
            if (_assetHandles.TryGetValue(path, out var assetHandle))
            {
                handle.Release();
                handle = assetHandle;
            }
            else
            {
                _assetHandles.Add(path, handle);
            }
            return handle;
        }

        internal AssetHandle LoadSyncHandle(string path, Type type)
        {
            if (_assetHandles.TryGetValue(path, out var value))
            {
                return value;
            }

            AssetsManager.Instance.Load(path, type, out var handle);
            if (handle == null) return null;
            if (_assetHandles.TryGetValue(path, out var assetHandle))
            {
                handle.Release();
                handle = assetHandle;
            }
            else
            {
                _assetHandles.Add(path, handle);
            }
            return handle;
        }


        internal Sprite LoadSprite(string path, string mainPath)
        {
            if (_subAssetHandles.TryGetValue(mainPath, out var value))
            {
                return value.GetSubAssetObject<Sprite>(path);
            }

            var sprite = AssetsManager.Instance.LoadSub<Sprite>(mainPath, path, out SubAssetsHandle subAssetsHandle);
            if (subAssetsHandle == null) return null;
            if (_subAssetHandles.TryAdd(mainPath, subAssetsHandle)) return sprite;
            subAssetsHandle.Release();
            sprite = _subAssetHandles[mainPath].GetSubAssetObject<Sprite>(path);

            return sprite;
        }



        #endregion

        #region 异步加载
        internal async UniTask<T> LoadAsync<T>(string path) where T : UnityEngine.Object
        {
            if (_assetHandles.TryGetValue(path, out var value))
            {
                return value.GetAssetObject<T>();
            }

            _isRuningAwait = true;
            var (obj, handle) = await AssetsManager.Instance.LoadAsyncWithHandle<T>(path);
            if (!_isRuningAwait)
            {
                handle?.Release();
                return null;
            }
                
            if (handle == null)
            {
                return null;
            }
            if (!_assetHandles.TryAdd(path, handle))
            {
                handle.Release();
                return _assetHandles[path].GetAssetObject<T>();
            }
            else
            {
                return obj;
            }
        }

        internal async UniTask<UnityEngine.Object> LoadAsync(string path,Type type)
        {
            if (_assetHandles.TryGetValue(path, out var value))
            {
                return value.AssetObject;
            }
            _isRuningAwait = true;
            var (obj, handle) = await AssetsManager.Instance.LoadAsyncWithHandle(path,type);
            if (!_isRuningAwait)
            {
                handle?.Release();
                return null;
            }
            if (handle == null)
            {
                return null;
            }
            if (_assetHandles.TryGetValue(path, out var assetHandle))
            {
                handle.Release();
                return assetHandle.AssetObject;
            }
            _assetHandles.Add(path, handle);
            return obj;
        }

        internal async UniTask<AssetHandle> LoadAsyncHandle<T>(string path) where T : UnityEngine.Object
        {
            if (_assetHandles.TryGetValue(path, out var value))
            {
                return value;
            }
            _isRuningAwait = true;
            var (_, handle) = await AssetsManager.Instance.LoadAsyncWithHandle<T>(path);
            if (!_isRuningAwait)
            {
                handle?.Release();
                return null;
            }
            if (handle == null)
            {
                return null;
            }
            if (_assetHandles.TryGetValue(path, out var asyncHandle))
            {
                handle.Release();
                return asyncHandle;
            }
            _assetHandles.Add(path, handle);
            return handle;
        }

        internal async UniTask<AssetHandle> LoadAsyncHandle(string path,Type type)
        {
            if (_assetHandles.TryGetValue(path, out var value))
            {
                return value;
            }

            _isRuningAwait = true;
            var (_, handle) = await AssetsManager.Instance.LoadAsyncWithHandle(path,type);
            if (!_isRuningAwait)
            {
                handle?.Release();
                return null;
            }
            if (handle == null)
            {
                return null;
            }
            if (_assetHandles.TryGetValue(path, out var asyncHandle))
            {
                handle.Release();
                return asyncHandle;
            }

            _assetHandles.Add(path, handle);
            return handle;
        }

        internal async UniTask<Sprite> LoadSpriteAsync(string mainPath, string path)
        {
            if (_subAssetHandles.TryGetValue(mainPath, out var value))
            {
                return value.GetSubAssetObject<Sprite>(path);
            }

            _isRuningAwait = true;
            var (sprite, subHandle) = await AssetsManager.Instance.LoadSubAsyncWithHandle<Sprite>(mainPath, path);
            if (!_isRuningAwait)
            {
                subHandle?.Release();
                return null;
            }
            if (subHandle == null)
            {
                return null;
            }

            if (_subAssetHandles.TryAdd(mainPath, subHandle))
                return sprite;
            subHandle.Release();
            return _subAssetHandles[mainPath].GetSubAssetObject<Sprite>(path);
        }
        #endregion
    }
}
