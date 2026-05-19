using System;
using UnityEngine;

namespace Fuel.Scene
{
    /// <summary>
    /// 场景配置信息
    /// </summary>
    [Serializable]
    public class SceneInfo
    {
        /// <summary>
        /// 场景唯一标识
        /// </summary>
        public string SceneId;

        /// <summary>
        /// 场景显示名称
        /// </summary>
        public string SceneName;

        /// <summary>
        /// 场景资源路径（Unity 场景文件路径）
        /// </summary>
        public string ScenePath;

        /// <summary>
        /// 场景根物体预制体（可选，用于放置场景管理逻辑的根节点）
        /// </summary>
        public GameObject SceneRootPrefab;

        /// <summary>
        /// 是否为主场景
        /// </summary>
        public bool IsMainScene;
    }
}
