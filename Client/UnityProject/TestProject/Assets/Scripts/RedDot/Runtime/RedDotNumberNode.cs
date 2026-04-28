using System;
using UnityEngine;

namespace NetFramework.RedDot.RunTime
{
    public class RedDotNumberNode : RedDotNodeBase
    {
        public RedDotNumberNode(string nodeName, RedDotNodeBase parent = null) : base(nodeName, parent)
        {
        }

        /// <summary>
        /// 红点数量
        /// </summary>
        private int m_redDotCount;

        public int RedDotCount
        {
            get => m_redDotCount;
            private set => m_redDotCount = value;
        }

        public override void SetStatus(int number)
        {
            if (m_children?.Count > 0)
            {
                Debug.LogError("非查看类型红点且非叶子节点，不能修改数量");
            }
            else
            {
                RedDotCount = number;
                CalculateCount();
            }
        }

        /// <summary>
        /// 通过累加的方式设置状态
        /// </summary>
        /// <param name="number"></param>
        public void SetStateByAccumulation(int number)
        {
            if (m_children?.Count > 0)
            {
                Debug.LogError("非查看类型红点且非叶子节点，不能修改数量");
            }
            else
            {
                RedDotCount += number;
                if (RedDotCount + number < 0)
                {
                    RedDotCount = 0;
                }
                CalculateCount();
            }
        }

        public override void CalculateCount()
        {
            base.CalculateCount();
            if (m_children == null || m_children.Count == 0) //叶子结点
            {
                m_changeCb?.Invoke(RedDotCount);
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
                if (count != m_redDotCount)
                {
                    m_redDotCount = count;
                    m_changeCb?.Invoke(m_redDotCount);
                    m_parent?.CalculateCount();
                }
            }
        }

        public override void Register(Action<int> cb)
        {
            base.Register(cb);
            cb?.Invoke(m_redDotCount);
        }

        protected override void ResetStatus()
        {
            base.ResetStatus();
            RedDotCount = 0;
            CalculateCount();
        }
    }
}