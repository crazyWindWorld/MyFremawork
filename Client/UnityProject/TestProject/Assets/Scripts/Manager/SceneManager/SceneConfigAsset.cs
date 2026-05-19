using System.Collections.Generic;
using UnityEngine;
using Fuel.Scene;

namespace Manager.SceneManager
{
    /// <summary>
    /// 场景配置资产（ScriptableObject）
    /// 用于在 Inspector 中配置场景信息
    /// </summary>
    [CreateAssetMenu(fileName = "SceneConfig", menuName = "Fuel/Scene Config")]
    public class SceneConfigAsset : ScriptableObject
    {
        /// <summary>
        /// 场景配置列表
        /// </summary>
        public List<SceneInfo> SceneInfos = new List<SceneInfo>();
    }
}
