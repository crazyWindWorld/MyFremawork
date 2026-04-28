using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

namespace NetFramework.RedDot.RunTime
{
    [CreateAssetMenu(menuName = "Tools/RedDotConfigAsset", fileName = "RedDotConfigAsset")]
    public class RedDotConfigAsset : ScriptableObject
    {
        private static RedDotConfigAsset m_instance;

        public static RedDotConfigAsset Instance
        {
            get
            {
                if (m_instance == null)
                {
                    //m_instance = AssetsLoadManager.Instance.LoadSync<RedDotConfigAsset>("RedDotConfigAsset");
                    if (m_instance != null)
                    {
                        m_instance.Init();
                    }
                }

                return m_instance;
            }
        }
#if UNITY_EDITOR
        [ReadOnly]
#endif
        public List<RedDotConfigData> Data;
        public Dictionary<int, RedDotConfigData> DataDic;

        public void Init()
        {
            ResetIsChildData();
            DataDic = new Dictionary<int, RedDotConfigData>();
            foreach (var data in Data)
            {
                DataDic.TryAdd(data.Id, data);
            }
        }

        private void ResetIsChildData()
        {
            for (int i = 0; i < Data.Count; i++)
            {
                bool isContainedInOther = false; // 标记当前路径是否被其他路径包含
                for (int j = 0; j < Data.Count; j++)
                {
                    if (i == j || string.IsNullOrEmpty(Data[i].Path) || string.IsNullOrEmpty(Data[j].Path)) // 不和自己比较
                    {
                        continue;
                    }

                    if (Data[j].Path.StartsWith(Data[i].Path)) // 如果当前路径是其他路径的子路径
                    {
                        isContainedInOther = true;
                        break; // 只要找到一个包含它的路径，就可以提前退出
                    }
                }

                Data[i].IsChild = !isContainedInOther; // 如果不在其他路径中，IsChild = true；否则 false
            }
        }

        [Serializable]
        public class RedDotConfigData
        {
            public int Id;
            public string Path;
#if UNITY_EDITOR
            [LabelText("是否是查看红点")]
#endif
            public bool IsView;
#if UNITY_EDITOR
            [ShowIf("@IsView==true")]
#endif
            public ViewType ViewType;

            public RedDotShowType ShowType = RedDotShowType.Normal;

            [Tooltip("自动计算,修改无效")] public bool IsChild;
#if UNITY_EDITOR
            [ShowIf("@IsView==true")]
#endif
            public bool BindRole = true;

            [Tooltip("枚举名称")] 
            public string Alias;
            
            [Tooltip("使用本地存储")] 
            public bool UseLocalSave;
        }
    }

    public enum RedDotShowType
    {
        Normal,
        New,
        Number,
    }
}