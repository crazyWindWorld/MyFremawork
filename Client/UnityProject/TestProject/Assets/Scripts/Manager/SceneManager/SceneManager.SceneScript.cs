using System;
using System.Collections.Generic;
using UnityEngine;
using Fuel.Scene;
using Fuel.Log;

namespace Manager.SceneManager
{
    public partial class SceneManager
    {
        #region Fields

        /// <summary>
        /// 场景脚本工厂字典（SceneId -> 工厂方法）
        /// </summary>
        private readonly Dictionary<string, Func<SceneBase>> _sceneScriptFactories = new Dictionary<string, Func<SceneBase>>();

        /// <summary>
        /// 场景根物体字典（SceneId -> GameObject）
        /// </summary>
        private readonly Dictionary<string, GameObject> _sceneRoots = new Dictionary<string, GameObject>();

        #endregion

        #region Scene Script Factory Registration

        /// <summary>
        /// 注册场景脚本工厂方法
        /// </summary>
        /// <param name="sceneId">场景ID</param>
        /// <param name="factory">工厂方法，返回 SceneBase 实例</param>
        public void RegisterSceneScript(string sceneId, Func<SceneBase> factory)
        {
            if (string.IsNullOrEmpty(sceneId) || factory == null)
            {
                DebugLogger.LogWarning(LogWriter.SceneManager, "Invalid scene script registration");
                return;
            }

            _sceneScriptFactories[sceneId] = factory;
        }

        /// <summary>
        /// 注册场景脚本工厂方法（泛型版本）
        /// </summary>
        /// <typeparam name="T">场景脚本类型</typeparam>
        /// <param name="sceneId">场景ID</param>
        public void RegisterSceneScript<T>(string sceneId) where T : SceneBase, new()
        {
            _sceneScriptFactories[sceneId] = () => new T();
        }

        #endregion

        #region Scene Root Management

        /// <summary>
        /// 设置场景根物体
        /// </summary>
        /// <param name="sceneId">场景ID</param>
        /// <param name="rootObject">根物体</param>
        public void SetSceneRoot(string sceneId, GameObject rootObject)
        {
            _sceneRoots[sceneId] = rootObject;
        }

        /// <summary>
        /// 获取场景根物体
        /// </summary>
        /// <param name="sceneId">场景ID</param>
        /// <returns>场景根物体</returns>
        public GameObject GetSceneRoot(string sceneId)
        {
            return _sceneRoots.TryGetValue(sceneId, out var root) ? root : null;
        }

        /// <summary>
        /// 查找或创建场景根物体
        /// </summary>
        private GameObject FindOrCreateSceneRoot(SceneInfo sceneInfo)
        {
            // 如果已缓存，直接返回
            if (_sceneRoots.TryGetValue(sceneInfo.SceneId, out var cachedRoot))
            {
                return cachedRoot;
            }

            // 如果有预制体，实例化并返回
            if (sceneInfo.SceneRootPrefab != null)
            {
                var root = UnityEngine.Object.Instantiate(sceneInfo.SceneRootPrefab);
                root.name = $"[SceneRoot] {sceneInfo.SceneName}";
                _sceneRoots[sceneInfo.SceneId] = root;
                return root;
            }

            // 创建空的根物体
            var emptyRoot = new GameObject($"[SceneRoot] {sceneInfo.SceneName}");
            _sceneRoots[sceneInfo.SceneId] = emptyRoot;
            return emptyRoot;
        }

        #endregion

        #region Scene Script Management

        /// <summary>
        /// 创建场景脚本实例
        /// </summary>
        private void CreateSceneScript(SceneInfo sceneInfo, SceneData sceneData)
        {
            // 1. 获取或创建场景根物体
            GameObject viewObject = FindOrCreateSceneRoot(sceneInfo);

            // 2. 创建场景脚本实例
            SceneBase sceneScript = CreateSceneScriptInstance(sceneInfo);
            if (sceneScript == null)
            {
                DebugLogger.Log(LogWriter.SceneManager, $"No script factory for scene: {sceneInfo.SceneId}");
                return;
            }

            // 3. 注入依赖
            sceneScript.ViewObject = viewObject;
            sceneScript.SceneInfo = sceneInfo;
            sceneScript.IsLoaded = true;

            // 4. 缓存场景脚本
            _sceneScripts[sceneInfo.SceneId] = sceneScript;

            // 5. 调用生命周期
            sceneScript.OnEnter(sceneData);
            sceneScript.RegisterEvents();

            DebugLogger.Log(LogWriter.SceneManager, $"Scene script created for scene: {sceneInfo.SceneId}");
        }

        /// <summary>
        /// 创建场景脚本实例（纯 C#）
        /// </summary>
        private SceneBase CreateSceneScriptInstance(SceneInfo sceneInfo)
        {
            if (_sceneScriptFactories.TryGetValue(sceneInfo.SceneId, out var factory))
            {
                return factory();
            }

            return null;
        }

        /// <summary>
        /// 暂停场景
        /// </summary>
        /// <param name="sceneId">场景ID</param>
        public void PauseScene(string sceneId)
        {
            if (_sceneScripts.TryGetValue(sceneId, out var sceneScript))
            {
                sceneScript.OnPause();
            }
        }

        /// <summary>
        /// 恢复场景
        /// </summary>
        /// <param name="sceneId">场景ID</param>
        public void ResumeScene(string sceneId)
        {
            if (_sceneScripts.TryGetValue(sceneId, out var sceneScript))
            {
                sceneScript.OnResume();
            }
        }

        /// <summary>
        /// 暂停所有场景
        /// </summary>
        public void PauseAllScenes()
        {
            if (_currentMainScene != null)
            {
                PauseScene(_currentMainScene.SceneId);
            }

            foreach (var sceneInfo in _loadedAdditiveScenes)
            {
                PauseScene(sceneInfo.SceneId);
            }
        }

        /// <summary>
        /// 恢复所有场景
        /// </summary>
        public void ResumeAllScenes()
        {
            if (_currentMainScene != null)
            {
                ResumeScene(_currentMainScene.SceneId);
            }

            foreach (var sceneInfo in _loadedAdditiveScenes)
            {
                ResumeScene(sceneInfo.SceneId);
            }
        }

        #endregion
    }
}
