using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Fuel.GameEvent;
using Fuel.Scene;
using Fuel.Log;

namespace Manager.SceneManager
{
    public partial class SceneManager
    {
        #region Main Scene Loading

        /// <summary>
        /// 异步加载主场景（会卸载当前主场景和所有附加场景）
        /// </summary>
        /// <param name="sceneId">场景ID</param>
        /// <param name="sceneData">场景数据</param>
        /// <param name="onProgress">加载进度回调 (0-1)</param>
        /// <param name="onComplete">加载完成回调</param>
        public void LoadMainScene(string sceneId, SceneData sceneData = null,
            Action<float> onProgress = null, Action onComplete = null)
        {
            if (_loadingScenes.Contains(sceneId))
            {
                DebugLogger.LogWarning(LogWriter.SceneManager, $"Scene {sceneId} is already loading");
                return;
            }

            if (!_sceneConfigs.TryGetValue(sceneId, out var sceneInfo))
            {
                DebugLogger.LogError(LogWriter.SceneManager, $"Scene config not found: {sceneId}");
                return;
            }

            if (!sceneInfo.IsMainScene)
            {
                DebugLogger.LogWarning(LogWriter.SceneManager, $"Scene {sceneId} is not a main scene, use LoadAdditiveScene instead");
                return;
            }

            LoadMainSceneAsync(sceneInfo, sceneData, onProgress, onComplete).Forget();
        }

        private async UniTask LoadMainSceneAsync(SceneInfo sceneInfo, SceneData sceneData,
            Action<float> onProgress, Action onComplete)
        {
            string sceneId = sceneInfo.SceneId;
            string oldSceneId = _currentMainScene?.SceneId;
            _loadingScenes.Add(sceneId);

            // 通知开始加载
            EventDispatcher.Instance.Dispatch(new Scene_LoadStartEvent
            {
                SceneId = sceneId,
                IsMainScene = true
            });

            // 卸载所有附加场景
            await UnloadAllAdditiveScenesAsync();

            // 卸载当前主场景
            if (_currentMainScene != null)
            {
                await UnloadSceneAsync(_currentMainScene);
            }

            // 异步加载新场景
            await LoadSceneAsync(sceneInfo, true, sceneData, onProgress);

            _currentMainScene = sceneInfo;

            // 通知主场景切换
            EventDispatcher.Instance.Dispatch(new Scene_MainSceneChangedEvent
            {
                OldSceneId = oldSceneId,
                NewSceneId = sceneId
            });

            _loadingScenes.Remove(sceneId);

            onComplete?.Invoke();
            OnSceneLoaded?.Invoke(sceneInfo);

            DebugLogger.Log(LogWriter.SceneManager, $"Main scene loaded: {sceneId}");
        }

        #endregion

        #region Additive Scene Loading

        /// <summary>
        /// 异步加载附加场景（叠加在当前场景上）
        /// </summary>
        /// <param name="sceneId">场景ID</param>
        /// <param name="sceneData">场景数据</param>
        /// <param name="onProgress">加载进度回调 (0-1)</param>
        /// <param name="onComplete">加载完成回调</param>
        public void LoadAdditiveScene(string sceneId, SceneData sceneData = null,
            Action<float> onProgress = null, Action onComplete = null)
        {
            if (_loadingScenes.Contains(sceneId))
            {
                DebugLogger.LogWarning(LogWriter.SceneManager, $"Scene {sceneId} is already loading");
                return;
            }

            if (!_sceneConfigs.TryGetValue(sceneId, out var sceneInfo))
            {
                DebugLogger.LogError(LogWriter.SceneManager, $"Scene config not found: {sceneId}");
                return;
            }

            if (sceneInfo.IsMainScene)
            {
                DebugLogger.LogWarning(LogWriter.SceneManager, $"Scene {sceneId} is a main scene, use LoadMainScene instead");
                return;
            }

            LoadAdditiveSceneAsync(sceneInfo, sceneData, onProgress, onComplete).Forget();
        }

        private async UniTask LoadAdditiveSceneAsync(SceneInfo sceneInfo, SceneData sceneData,
            Action<float> onProgress, Action onComplete)
        {
            string sceneId = sceneInfo.SceneId;
            _loadingScenes.Add(sceneId);

            // 检查是否已加载
            if (_sceneScripts.ContainsKey(sceneId))
            {
                DebugLogger.LogWarning(LogWriter.SceneManager, $"Scene {sceneId} is already loaded");
                _loadingScenes.Remove(sceneId);
                onComplete?.Invoke();
                return;
            }

            // 通知开始加载
            EventDispatcher.Instance.Dispatch(new Scene_LoadStartEvent
            {
                SceneId = sceneId,
                IsMainScene = false
            });

            // 异步加载场景（additive 模式）
            await LoadSceneAsync(sceneInfo, false, sceneData, onProgress);

            _loadedAdditiveScenes.Add(sceneInfo);

            _loadingScenes.Remove(sceneId);

            onComplete?.Invoke();
            OnSceneLoaded?.Invoke(sceneInfo);

            DebugLogger.Log(LogWriter.SceneManager, $"Additive scene loaded: {sceneId}");
        }

        #endregion

        #region Scene Unloading

        /// <summary>
        /// 卸载附加场景
        /// </summary>
        /// <param name="sceneId">场景ID</param>
        /// <param name="onComplete">卸载完成回调</param>
        public void UnloadScene(string sceneId, Action onComplete = null)
        {
            if (!_sceneScripts.ContainsKey(sceneId))
            {
                DebugLogger.LogWarning(LogWriter.SceneManager, $"Scene {sceneId} is not loaded");
                return;
            }

            var sceneInfo = GetLoadedSceneInfo(sceneId);
            if (sceneInfo == null)
            {
                onComplete?.Invoke();
                return;
            }

            UnloadSceneAsync(sceneInfo, onComplete).Forget();
        }

        /// <summary>
        /// 卸载所有附加场景
        /// </summary>
        /// <param name="onComplete">卸载完成回调</param>
        public void UnloadAllAdditiveScenes(Action onComplete = null)
        {
            UnloadAllAdditiveScenesAsync(onComplete).Forget();
        }

        private async UniTask UnloadSceneAsync(SceneInfo sceneInfo, Action onComplete = null)
        {
            string sceneId = sceneInfo.SceneId;

            // 通知开始卸载
            EventDispatcher.Instance.Dispatch(new Scene_UnloadStartEvent
            {
                SceneId = sceneId
            });

            // 调用场景脚本的 UnregisterEvents 和 OnExit
            if (_sceneScripts.TryGetValue(sceneId, out var sceneScript))
            {
                sceneScript.UnregisterEvents();
                sceneScript.OnExit();
                sceneScript.IsLoaded = false;
                _sceneScripts.Remove(sceneId);
            }

            // 异步卸载场景
            var asyncOp = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneInfo.ScenePath);
            if (asyncOp != null)
            {
                await asyncOp.ToUniTask();
            }

            _loadedAdditiveScenes.RemoveAll(s => s.SceneId == sceneId);

            // 通知卸载完成
            EventDispatcher.Instance.Dispatch(new Scene_UnloadCompleteEvent
            {
                SceneId = sceneId
            });

            onComplete?.Invoke();
            OnSceneUnloaded?.Invoke(sceneInfo);

            DebugLogger.Log(LogWriter.SceneManager, $"Scene unloaded: {sceneId}");
        }

        private async UniTask UnloadAllAdditiveScenesAsync(Action onComplete = null)
        {
            var scenesToUnload = new List<SceneInfo>(_loadedAdditiveScenes);

            foreach (var sceneInfo in scenesToUnload)
            {
                await UnloadSceneAsync(sceneInfo);
            }

            onComplete?.Invoke();
        }

        private SceneInfo GetLoadedSceneInfo(string sceneId)
        {
            if (_currentMainScene != null && _currentMainScene.SceneId == sceneId)
                return _currentMainScene;

            return _loadedAdditiveScenes.Find(s => s.SceneId == sceneId);
        }

        #endregion

        #region Core Loading

        private async UniTask LoadSceneAsync(SceneInfo sceneInfo, bool isMainScene,
            SceneData sceneData, Action<float> onProgress)
        {
            var asyncOp = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
                sceneInfo.ScenePath,
                isMainScene
                    ? UnityEngine.SceneManagement.LoadSceneMode.Single
                    : UnityEngine.SceneManagement.LoadSceneMode.Additive);

            if (asyncOp == null)
            {
                DebugLogger.LogError(LogWriter.SceneManager, $"Failed to load scene: {sceneInfo.ScenePath}");
                return;
            }

            // 等待加载完成并报告进度
            while (!asyncOp.isDone)
            {
                float progress = asyncOp.progress;
                onProgress?.Invoke(progress);

                // 通知进度事件
                EventDispatcher.Instance.Dispatch(new Scene_LoadProgressEvent
                {
                    SceneId = sceneInfo.SceneId,
                    Progress = progress
                });

                await UniTask.Yield();
            }

            // 加载完成，进度为1
            onProgress?.Invoke(1f);
            EventDispatcher.Instance.Dispatch(new Scene_LoadProgressEvent
            {
                SceneId = sceneInfo.SceneId,
                Progress = 1f
            });

            // 创建场景脚本
            CreateSceneScript(sceneInfo, sceneData);

            // 通知加载完成
            EventDispatcher.Instance.Dispatch(new Scene_LoadCompleteEvent
            {
                SceneId = sceneInfo.SceneId,
                IsMainScene = isMainScene
            });
        }

        #endregion
    }
}
