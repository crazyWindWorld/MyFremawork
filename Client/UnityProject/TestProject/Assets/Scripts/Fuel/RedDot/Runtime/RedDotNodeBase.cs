using System;
using System.Collections.Generic;
using System.Linq;

namespace Fuel.RedDot.RunTime
{
    public class RedDotNodeBase
    {
        /// <summary>
        /// 子节点
        /// </summary>
        protected Dictionary<string, RedDotNodeBase> m_children;
#if UNITY_EDITOR
        public Dictionary<string, RedDotNodeBase> Children => m_children;
#endif
        /// <summary>
        /// 父节点
        /// </summary>
        protected RedDotNodeBase m_parent;

        /// <summary>
        /// 数量改变事件
        /// </summary>
        protected Action<int> m_changeCb;

        /// <summary>
        /// 节点名称
        /// </summary>
        public string NodeName { get; }

        public RedDotNodeBase(string nodeName, RedDotNodeBase parent = null)
        {
            m_parent = parent;
            NodeName = nodeName;
        }

        /// <summary>
        /// 初始化红点
        /// </summary>
        /// <param name="path">红点路径</param>
        /// <param name="isView">是否是查看红点</param>
        /// <param name="bindRole">是否是绑定玩家id</param>
        /// <returns></returns>
        public RedDotNodeBase InitNode(string path, bool isView, bool bindRole)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var splitPath = path.Split('/');
            return InitNode(splitPath, 0, isView, bindRole);
        }

        private RedDotNodeBase InitNode(string[] splitPath, int index, bool isView, bool bindRole)
        {
            string nextNodeName = splitPath[index];
            bool isLeaf = index == splitPath.Length - 1;

            if (m_children == null || !m_children.ContainsKey(nextNodeName))
            {
                m_children ??= new Dictionary<string, RedDotNodeBase>();
                RedDotNodeBase redDotNode;
                if (isLeaf)
                {
                    redDotNode = isView
                        ? new RedDotViewNode(nextNodeName, bindRole, this)
                        : new RedDotNumberNode(nextNodeName, this);
                }
                else
                {
                    redDotNode = new RedDotNumberNode(nextNodeName, this);
                }

                m_children.Add(nextNodeName, redDotNode);
            }

            if (isLeaf)
            {
                return m_children[nextNodeName];
            }

            return m_children[nextNodeName].InitNode(splitPath, index + 1, isView, bindRole);
        }

        /// <summary>
        /// 获取红点节点
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns>节点</returns>
        public RedDotNodeBase GetRedDotNode(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var splitPath = path.Split('/');
            return GetRedDotNode(splitPath, 0);
        }

        private RedDotNodeBase GetRedDotNode(string[] splitPath, int index)
        {
            string nextNodeName = splitPath[index];
            if (m_children == null || !m_children.TryGetValue(nextNodeName, out var childNode))
            {
                return null;
            }

            if (index == splitPath.Length - 1)
            {
                return childNode;
            }

            return childNode.GetRedDotNode(splitPath, index + 1);
        }

        public virtual void SetStatus(int count)
        {
        }

        public virtual void CalculateCount()
        {
        }

        /// <summary>
        /// 注册消息
        /// </summary>
        /// <param name="cb"></param>
        public virtual void Register(Action<int> cb)
        {
            m_changeCb += cb;
        }

        /// <summary>
        /// 注销消息
        /// </summary>
        /// <param name="cb"></param>
        public void Unregister(Action<int> cb)
        {
            m_changeCb -= cb;
        }

        protected virtual void ResetStatus()
        {

        }

        public virtual void Clear()
        {
            ResetStatus();
            m_changeCb = null;
            if (m_children != null)
            {
                List<string> keysToRemove = m_children.Keys.ToList();
                foreach (var key in keysToRemove)
                {
                    m_children[key].Clear();
                }
                m_children.Clear();
            }
            if (m_parent != null)
            {
                m_parent.RemoveChild(NodeName);
            }
        }

        private void RemoveChild(string nodeName)
        {
            if (m_children == null || !m_children.ContainsKey(nodeName))
            {
                return;
            }
            m_children.Remove(nodeName);
        }

        public void ClearStatus()
        {
            if (m_children != null)
            {
                foreach (var child in m_children)
                {
                    child.Value.ClearStatus();
                }
            }
            else
            {
                ResetStatus();
            }
        }

        #region 查看红点相关逻辑

        /// <summary>
        /// 是否是查看红点
        /// </summary>
        private bool m_isView;


        /// <summary>
        /// 获取红点全路径
        /// </summary>
        /// <param name="path">当前节点名称</param>
        /// <returns>全路径</returns>
        protected string GetFullName(string path)
        {
            if (m_parent != null)
            {
                path = m_parent.NodeName + "/" + path;
                return m_parent.GetFullName(path);
            }

            return path;
        }
        #endregion
    }
}