using System;
using System.Collections.Generic;
using UnityEngine;

namespace Manager.UIManager
{
    /// <summary>
    /// 绑定条目：记录一个需要在代码中引用的组件。
    /// </summary>
    [Serializable]
    public class UIBindEntry
    {
        /// <summary>生成的变量名</summary>
        public string VariableName;
        /// <summary>组件类型全名（如 UnityEngine.UI.Button）</summary>
        public string ComponentTypeName;
        /// <summary>绑定对象在 Prefab 内的层级路径</summary>
        public string TargetPath;
        /// <summary>绑定的目标对象引用</summary>
        public UnityEngine.Object Target;

        public UIBindEntry() { }

        public UIBindEntry(string variableName, string componentTypeName, string targetPath, UnityEngine.Object target)
        {
            VariableName = variableName;
            ComponentTypeName = componentTypeName;
            TargetPath = targetPath;
            Target = target;
        }
    }

    /// <summary>
    /// UI 绑定数据（ScriptableObject）。
    /// 以 .asset 文件形式存储在 Assets/Resources/UIBindData/ 目录下，
    /// 通过 PrefabPath 与对应的 UI Prefab 关联。
    /// 编辑器中配置绑定条目，代码生成器根据此数据生成 NodeProvider 子类。
    /// </summary>
    [CreateAssetMenu(fileName = "NewUIBindData", menuName = "UI/UIBindData")]
    public class UIBindData : ScriptableObject
    {
        /// <summary>关联的 Prefab 资源路径（如 Assets/Resources/UIPrefabs/TestPanel.prefab）</summary>
        public string PrefabPath;

        /// <summary>生成的类名（默认取 Prefab 名称）</summary>
        public string ClassName;

        /// <summary>UI 层级</summary>
        public UILayer Layer = UILayer.Normal;

        /// <summary>所有绑定条目</summary>
        public List<UIBindEntry> Entries = new List<UIBindEntry>();

        /// <summary>
        /// 添加绑定条目
        /// </summary>
        public void AddEntry(string variableName, string componentTypeName, string targetPath, UnityEngine.Object target)
        {
            // 避免重复绑定同一个目标路径的同一个类型
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].TargetPath == targetPath && Entries[i].ComponentTypeName == componentTypeName)
                    return;
            }
            Entries.Add(new UIBindEntry(variableName, componentTypeName, targetPath, target));
        }

        /// <summary>
        /// 移除绑定条目
        /// </summary>
        public void RemoveEntry(int index)
        {
            if (index >= 0 && index < Entries.Count)
                Entries.RemoveAt(index);
        }

        /// <summary>
        /// 检查目标是否已绑定指定类型
        /// </summary>
        public bool HasEntry(string targetPath, string componentTypeName)
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].TargetPath == targetPath && Entries[i].ComponentTypeName == componentTypeName)
                    return true;
            }
            return false;
        }
    }
}
