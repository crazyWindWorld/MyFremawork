using NetFramework.RedDot.RunTime;
using UnityEngine;
using UnityEngine.UI;

namespace NetFramework.RedDot.RunTime
{
    public class RedDotViewBase : MonoBehaviour
    {
        protected int m_redDotId = -1;
        private RedDotConfigAsset.RedDotConfigData m_redDotData;
        public GameObject NormalGo;
        public GameObject NewGo;
        public GameObject NumberGo;
        public Text TextNumber;
        private string m_path = "";

        protected virtual void Init()
        {
            if (RedDotConfigAsset.Instance.DataDic.TryGetValue(m_redDotId, out var redData))
            {
                if (redData.Path.Contains("{"))
                {
                    return;
                }
                m_redDotData = redData;
                NormalGo?.SetActive(m_redDotData.ShowType == RedDotShowType.Normal);
                NewGo?.SetActive(m_redDotData.ShowType == RedDotShowType.New);
                NumberGo?.SetActive(m_redDotData.ShowType == RedDotShowType.Number);
                m_path = m_redDotData.Path;
                RedDotTree.Instance.Register(m_redDotId, ChangeRedDotCount);
                Debug.LogWarning("<color=#FFFF00>自动注册红点：" + m_path + "</color>");
            }
            else
            {
                NormalGo?.SetActive(false);
                NewGo?.SetActive(false);
                NumberGo?.SetActive(false);
            }
        }

        public virtual void Register(int redDotId, params object[] parameters)
        {
            m_redDotId = redDotId;
            Unregister();
            if (RedDotConfigAsset.Instance.DataDic.TryGetValue(m_redDotId, out var redData))
            {
                m_redDotData = redData;
                NormalGo?.SetActive(m_redDotData.ShowType == RedDotShowType.Normal);
                NewGo?.SetActive(m_redDotData.ShowType == RedDotShowType.New);
                NumberGo?.SetActive(m_redDotData.ShowType == RedDotShowType.Number);
                m_path = string.Format(m_redDotData.Path, parameters);
                RedDotTree.Instance.Register(m_redDotId, ChangeRedDotCount, parameters);
                Debug.LogWarning("<color=#FFFF00>主动注册红点：" + m_path + "</color>");
            }
        }

        public virtual void Watch()
        {
            if (m_redDotId != -1 && !string.IsNullOrEmpty(m_path))
            {
                RedDotTree.Instance.Watch(m_path);
            }
        }

        private void OnDestroy()
        {
            Unregister();
        }

        public void Unregister()
        {
            if (!string.IsNullOrEmpty(m_path))
            {
                RedDotTree.Instance.Unregister(m_path, ChangeRedDotCount);
                ChangeRedDotCount(0);
                Debug.LogWarning("<color=#FFFF00>注销红点：" + m_path + "</color>");
                m_path = "";
            }
        }

        public virtual void ChangeRedDotCount(int count)
        {
            switch (m_redDotData.ShowType)
            {
                case RedDotShowType.Normal:
                    NormalGo?.SetActive(count > 0);
                    break;
                case RedDotShowType.New:
                    NewGo?.SetActive(count > 0);
                    break;
                case RedDotShowType.Number:
                    NumberGo?.SetActive(count > 0);
                    if (TextNumber != null)
                    {
                        int maxCount = count > 99 ? 99 : count;
                        TextNumber.text = maxCount.ToString();
                    }
                    break;
            }
        }
        
    }
}