using System;
using Cysharp.Threading.Tasks;
using Fuel.AssetManager;
using UnityEngine;

namespace Fuel.Scene
{
    /// <summary>
    /// 场景脚本基类（纯 C#，与 UIWindow 模式一致）
    /// 每个场景对应一个继承此类的脚本
    /// </summary>
    public abstract class SceneBase
    {
        /// <summary>
        /// 场景根物体（由 SceneManager 注入）
        /// </summary>
        public GameObject ViewObject { get; internal set; }

        /// <summary>
        /// 场景配置信息（由 SceneManager 注入）
        /// </summary>
        public SceneInfo SceneInfo { get; internal set; }

        /// <summary>
        /// 场景是否已加载完成
        /// </summary>
        public bool IsLoaded { get; internal set; }

        /// <summary>
        /// 场景是否处于暂停状态
        /// </summary>
        public bool IsPaused { get; private set; }

        protected virtual string AssetsGroupName => SceneInfo?.SceneId;

        #region 生命周期方法

        /// <summary>
        /// 场景进入时调用（场景加载完成后）
        /// </summary>
        /// <param name="sceneData">场景数据</param>
        public virtual void OnEnter(SceneData sceneData) { }

        /// <summary>
        /// 场景退出时调用（场景卸载前）
        /// </summary>
        public virtual void OnExit()
        {
            AssetsLoadManager.Instance.ReleaseAllByGroup(AssetsGroupName);
        }

        /// <summary>
        /// 场景暂停时调用
        /// </summary>
        public virtual void OnPause()
        {
            IsPaused = true;
        }

        /// <summary>
        /// 场景恢复时调用
        /// </summary>
        public virtual void OnResume()
        {
            IsPaused = false;
        }

        #endregion

        #region 事件注册（与 UIWindow 一致）

        /// <summary>
        /// 注册事件（场景进入时调用）
        /// </summary>
        public virtual void RegisterEvents() { }

        /// <summary>
        /// 反注册事件（场景退出时调用）
        /// </summary>
        public virtual void UnregisterEvents() { }

        #endregion

        #region 辅助方法
        protected UniTask<T> LoadAssetAsync<T>(string path, string groupName = null) where T : UnityEngine.Object
        {
            return AssetsLoadManager.Instance.LoadAsync<T>(path, groupName ?? AssetsGroupName);
        }


        /// <summary>
        /// 获取场景根物体上的指定组件
        /// </summary>
        protected T GetComponent<T>() where T : Component
        {
            return ViewObject?.GetComponent<T>();
        }

        /// <summary>
        /// 获取场景根物体下的指定组件
        /// </summary>
        protected T GetComponentInChildren<T>() where T : Component
        {
            return ViewObject?.GetComponentInChildren<T>();
        }

        #endregion
    }
}
