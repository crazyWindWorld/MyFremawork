using System;
using System.Collections.Generic;
using UnityEngine;
using Fuel.Singleton;
using Fuel.Scene;
using Fuel.Log;

namespace Manager.SceneManager
{
    /// <summary>
    /// 场景管理器
    /// </summary>
    public partial class SceneManager : MonoSingleton<SceneManager>
    {
        #region Fields

        /// <summary>
        /// 场景配置字典（SceneId -> SceneInfo）
        /// </summary>
        private readonly Dictionary<string, SceneInfo> _sceneConfigs = new Dictionary<string, SceneInfo>();

        /// <summary>
        /// 已加载的主场景
        /// </summary>
        private SceneInfo _currentMainScene;

        /// <summary>
        /// 已加载的附加场景列表
        /// </summary>
        private readonly List<SceneInfo> _loadedAdditiveScenes = new List<SceneInfo>();

        /// <summary>
        /// 场景脚本实例字典（SceneId -> SceneBase）
        /// </summary>
        private readonly Dictionary<string, SceneBase> _sceneScripts = new Dictionary<string, SceneBase>();

        /// <summary>
        /// 当前正在加载的场景（防止重复加载）
        /// </summary>
        private readonly HashSet<string> _loadingScenes = new HashSet<string>();

        #endregion

        #region Properties

        /// <summary>
        /// 当前主场景
        /// </summary>
        public SceneInfo CurrentMainScene => _currentMainScene;

        /// <summary>
        /// 已加载的附加场景（只读）
        /// </summary>
        public IReadOnlyList<SceneInfo> LoadedAdditiveScenes => _loadedAdditiveScenes;

        /// <summary>
        /// 场景是否正在加载
        /// </summary>
        public bool IsLoading => _loadingScenes.Count > 0;

        #endregion

        #region Events

        /// <summary>
        /// 场景加载完成回调
        /// </summary>
        public event Action<SceneInfo> OnSceneLoaded;

        /// <summary>
        /// 场景卸载完成回调
        /// </summary>
        public event Action<SceneInfo> OnSceneUnloaded;

        #endregion

        #region Initialization

        protected override void OnInit()
        {
            base.OnInit();
            InitializeSceneConfigs();
            DebugLogger.Log(LogWriter.SceneManager, "Initialized successfully");
        }

        /// <summary>
        /// 初始化场景配置（从 Resources 加载 ScriptableObject）
        /// </summary>
        private void InitializeSceneConfigs()
        {
            var configAsset = Resources.Load<SceneConfigAsset>("SceneConfig/SceneConfig");
            if (configAsset != null)
            {
                foreach (var info in configAsset.SceneInfos)
                {
                    RegisterScene(info);
                }
            }
        }

        #endregion

        #region Configuration

        /// <summary>
        /// 注册场景配置
        /// </summary>
        public void RegisterScene(SceneInfo sceneInfo)
        {
            if (sceneInfo == null || string.IsNullOrEmpty(sceneInfo.SceneId))
            {
                DebugLogger.LogWarning(LogWriter.SceneManager, "Invalid scene info");
                return;
            }

            _sceneConfigs[sceneInfo.SceneId] = sceneInfo;
        }

        /// <summary>
        /// 获取场景配置
        /// </summary>
        public SceneInfo GetSceneInfo(string sceneId)
        {
            return _sceneConfigs.TryGetValue(sceneId, out var info) ? info : null;
        }

        #endregion

        #region Query

        /// <summary>
        /// 场景是否已加载
        /// </summary>
        public bool IsSceneLoaded(string sceneId)
        {
            return _sceneScripts.ContainsKey(sceneId);
        }

        /// <summary>
        /// 获取场景脚本实例
        /// </summary>
        public T GetSceneScript<T>(string sceneId) where T : SceneBase
        {
            if (_sceneScripts.TryGetValue(sceneId, out var script))
            {
                return script as T;
            }
            return null;
        }

        /// <summary>
        /// 获取当前主场景脚本
        /// </summary>
        public T GetCurrentMainSceneScript<T>() where T : SceneBase
        {
            if (_currentMainScene != null)
            {
                return GetSceneScript<T>(_currentMainScene.SceneId);
            }
            return null;
        }

        #endregion
    }
}
