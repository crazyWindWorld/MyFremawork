using System;
using System.Collections.Generic;
using UnityEngine;

namespace Manager.UIManager
{
    public class UIResourceManager
    {
        private Dictionary<string, UIWindow> _windowMap = new Dictionary<string, UIWindow>();
        private Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();
        private Dictionary<string, string> _prefabPaths = new Dictionary<string, string>();

        public void RegisterWindow(string windowId, UIWindow window)
        {
            if (!_windowMap.ContainsKey(windowId))
            {
                _windowMap.Add(windowId, window);
            }
            else
            {
                _windowMap[windowId] = window;
            }
        }

        public void UnregisterWindow(string windowId)
        {
            if (_windowMap.ContainsKey(windowId))
            {
                _windowMap.Remove(windowId);
            }
        }

        public UIWindow GetWindow(string windowId)
        {
            return _windowMap.TryGetValue(windowId, out var window) ? window : null;
        }

        public bool HasWindow(string windowId)
        {
            return _windowMap.ContainsKey(windowId);
        }

        public void RegisterPrefabPath(string windowId, string prefabPath)
        {
            if (!_prefabPaths.ContainsKey(windowId))
            {
                _prefabPaths.Add(windowId, prefabPath);
            }
            else
            {
                _prefabPaths[windowId] = prefabPath;
            }
        }

        public string GetPrefabPath(string windowId)
        {
            return _prefabPaths.TryGetValue(windowId, out var path) ? path : null;
        }

        public GameObject LoadPrefab(string windowId)
        {
            if (_prefabCache.TryGetValue(windowId, out var cached))
            {
                return cached;
            }

            string path = GetPrefabPath(windowId);
            if (string.IsNullOrEmpty(path)) return null;

            var prefab = Resources.Load<GameObject>(path);
            if (prefab != null)
            {
                _prefabCache[windowId] = prefab;
            }
            return prefab;
        }

        public GameObject CreateInstance(string windowId, Transform parent = null)
        {
            var prefab = LoadPrefab(windowId);
            if (prefab == null) return null;

            var instance = UnityEngine.Object.Instantiate(prefab, parent);
            return instance;
        }

        public void ReleasePrefab(string windowId)
        {
            if (_prefabCache.ContainsKey(windowId))
            {
                var prefab = _prefabCache[windowId];
                Resources.UnloadAsset(prefab);
                _prefabCache.Remove(windowId);
            }
        }

        public void Clear()
        {
            _windowMap.Clear();
            _prefabCache.Clear();
        }
    }
}
