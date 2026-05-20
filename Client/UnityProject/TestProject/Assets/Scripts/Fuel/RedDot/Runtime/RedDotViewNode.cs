using System;
using UnityEngine;

namespace Fuel.RedDot.RunTime
{
    public class RedDotViewNode : RedDotNodeBase
    {
        public RedDotViewNode(string nodeName, bool bindRole, RedDotNodeBase parent = null) : base(nodeName, parent)
        {
            m_bindRole = bindRole;
        }

        private bool m_bindRole;

        /// <summary>
        /// 是否已经查看
        /// </summary>
        private bool m_viewed = true;

        /// <summary>
        ///是否已经查看
        /// </summary>
        public bool Viewed
        {
            get => m_viewed;
            set
            {
                if (m_children?.Count > 0)
                {
                    Debug.LogWarning("非叶子节点，不能设置查看状态");
                    return;
                }

                if (m_viewed != value)
                {
                    m_viewed = value;
                    CalculateCount();
                }
            }
        }

        public override void SetStatus(int count)
        {
            var viewed = count <= 0;
            if (Viewed != viewed)
            {
                Viewed = viewed;
                if (Viewed)
                {
                    RedDotTree.LocalSave(m_bindRole, GetFullName(NodeName),
                        DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                }
                CalculateCount();
            }
        }

        public override void CalculateCount()
        {
            base.CalculateCount();
            if (m_children == null || m_children.Count == 0) //叶子结点
            {
                m_changeCb?.Invoke(Viewed ? 0 : 1);
                m_parent?.CalculateCount();
            }
            else
            {
                int count = 0;
                if (m_children != null)
                {
                    foreach (var child in m_children)
                    {
                        if (child.Value is RedDotNumberNode numberChildNode)
                        {
                            count += numberChildNode.RedDotCount;
                        }
                        else if (child.Value is RedDotViewNode viewChildNode)
                        {
                            count += viewChildNode.Viewed ? 0 : 1;
                        }
                    }
                }

                var viewed = count <= 0;
                if (viewed != Viewed)
                {
                    Viewed = viewed;
                    m_changeCb?.Invoke(viewed ? 0 : 1);
                    m_parent?.CalculateCount();
                }
            }
        }


        public override void Register(Action<int> cb)
        {
            base.Register(cb);
            cb?.Invoke(Viewed ? 0 : 1);
        }

        public override void Clear()
        {
            base.Clear();
        }

        protected override void ResetStatus()
        {
            base.ResetStatus();
            Viewed = false;
            RedDotTree.RemoveLocalSave(m_bindRole, GetFullName(NodeName));
            CalculateCount();
        }
    }
}