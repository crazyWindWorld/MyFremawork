using System;

#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace Fuel.RedDot.RunTime
{
    public class RedDotTree
    {
        private static RedDotTree m_instance;

        public static RedDotTree Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new RedDotTree();
                    if (RedDotConfigAsset.Instance != null)
                    {
                        foreach (var redDotConfigData in RedDotConfigAsset.Instance.Data)
                        {
                            if (redDotConfigData.Path.Contains("{") || redDotConfigData.Path.Contains("}"))
                            {
                                continue;
                            }

                            m_instance.InitRedDotNode(redDotConfigData.Path, redDotConfigData.IsView,
                                redDotConfigData.BindRole,
                                redDotConfigData.ViewType, redDotConfigData.UseLocalSave);
                        }
                    }
                }

                return m_instance;
            }
        }

        private const string TREE_ROOT = "RedDotTreeRoot";
        public RedDotNodeBase Root;

        public RedDotTree()
        {
            Root = new RedDotNumberNode(TREE_ROOT);
        }

        private RedDotNodeBase GetRedDotNode(string path) => Root.GetRedDotNode(path);

        /// <summary>
        /// 初始化红点节点
        /// </summary>
        /// <param name="path">红点路径</param>
        /// <param name="isView">是否是查看红点</param>
        /// <param name="bindRole">是否是绑定玩家ID</param>
        /// <param name="viewType">查看红点类型</param>
        public RedDotNodeBase InitRedDotNode(string path, bool isView, bool bindRole, ViewType viewType, bool localSave)
        {
            var redDotNode = Root.InitNode(path, isView, bindRole);
            if (isView)
            {
                string timestamp = GetLocalSaveData(bindRole, TREE_ROOT + "/" + path);
                if (!string.IsNullOrEmpty(timestamp))
                {
                    DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(timestamp)).LocalDateTime;
                    switch (viewType)
                    {
                        case ViewType.Day:
                            if (dateTime.Day != DateTime.UtcNow.Day)
                            {
                                redDotNode.SetStatus(1);
                            }

                            break;
                        case ViewType.Week:
                            if (!IsInSameWeek(dateTime, DateTime.UtcNow))
                            {
                                redDotNode.SetStatus(1);
                            }

                            break;
                        case ViewType.Month:
                            if (dateTime.Month != DateTime.UtcNow.Month)
                            {
                                redDotNode.SetStatus(1);
                            }

                            break;
                    }
                }
                else
                {
                    redDotNode.SetStatus(1);
                }
            }
            else
            {
                if (localSave)
                {
                    string localSaveData = GetLocalSaveData(bindRole, path);
                    if (int.TryParse(localSaveData, out int count))
                    {
                        redDotNode.SetStatus(count);
                    }
                }
            }

            return redDotNode;
        }

        /// <summary>
        /// 修改红点数量
        /// </summary>
        /// <param name="redDotId">红点ID</param>
        /// <param name="count">数量</param>
        /// <param name="args">红点路径参数</param>
        public void ChangeRedDotCount(int redDotId, int count, params object[] args)
        {
            if (RedDotConfigAsset.Instance.DataDic.TryGetValue(redDotId, out var redDotConfigData))
            {
                string path = string.Format(redDotConfigData.Path, args);
                var redDotNode = GetRedDotNode(path);
                if (redDotNode == null)
                {
                    redDotNode = InitRedDotNode(path, redDotConfigData.IsView,
                        redDotConfigData.BindRole, redDotConfigData.ViewType, redDotConfigData.UseLocalSave);
                }

                if (redDotNode is RedDotNumberNode redDotNumberNode)
                {
                    redDotNumberNode.SetStatus(count);
                    if (redDotConfigData.UseLocalSave)
                    {
                        LocalSave(redDotConfigData.BindRole, path, count.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// 通过累加值修改红点数量
        /// </summary>
        /// <param name="redDotId">红点ID</param>
        /// <param name="count">数量</param>
        /// <param name="args">红点路径参数</param>
        public void ChangeRedDotCountByAccumulation(int redDotId, int count, params object[] args)
        {
            if (RedDotConfigAsset.Instance.DataDic.TryGetValue(redDotId, out var redDotConfigData))
            {
                string path = string.Format(redDotConfigData.Path, args);
                var redDotNode = GetRedDotNode(path);
                if (redDotNode == null)
                {
                    redDotNode = InitRedDotNode(path, redDotConfigData.IsView,
                        redDotConfigData.BindRole, redDotConfigData.ViewType, redDotConfigData.UseLocalSave);
                }

                if (redDotNode is RedDotNumberNode redDotNumberNode)
                {
                    redDotNumberNode.SetStateByAccumulation(count);
                    if (redDotConfigData.UseLocalSave)
                    {
                        LocalSave(redDotConfigData.BindRole, path, redDotNumberNode.RedDotCount.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// 设置待查看
        /// </summary>
        /// <param name="redDotId">红点id</param>
        /// <param name="args">路径匹配参数</param>
        public void SetWaitWatch(int redDotId, params object[] args)
        {
            if (RedDotConfigAsset.Instance.DataDic.TryGetValue(redDotId, out var redDotConfigData))
            {
                string path = string.Format(redDotConfigData.Path, args);
                var redDotNode = GetRedDotNode(path);
                if (redDotNode == null)
                {
                    redDotNode = InitRedDotNode(path, redDotConfigData.IsView,
                        redDotConfigData.BindRole, redDotConfigData.ViewType, redDotConfigData.UseLocalSave);
                }

                if (redDotNode is RedDotViewNode redDotViewNode)
                {
                    if (CanChangView(redDotConfigData,
                            GetLocalSaveData(redDotConfigData.BindRole, TREE_ROOT + "/" + path)))
                    {
                        redDotViewNode.SetStatus(1);
                    }
                    else
                    {
                        redDotViewNode.SetStatus(0);
                    }
                }
                else
                {
                    Debug.LogWarning("非查看红点，设置待查看状态默认设置数量未为1");
                }
            }
        }

        private bool CanChangView(RedDotConfigAsset.RedDotConfigData redDotConfigData, string saveKey)
        {
            if (string.IsNullOrEmpty(saveKey))
            {
                return true;
            }

            DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(saveKey)).LocalDateTime;
            switch (redDotConfigData.ViewType)
            {
                case ViewType.Once:
                    return false;
                case ViewType.Day:
                    if (dateTime.Day == DateTime.Now.Day)
                    {
                        return false;
                    }

                    break;
                case ViewType.Week:
                    if (DateTime.UtcNow.Day - dateTime.Day <= 7)
                    {
                        return false;
                    }

                    break;
                case ViewType.Month:
                    if (DateTime.UtcNow.Month == dateTime.Month)
                    {
                        return false;
                    }

                    break;
            }

            return true;
        }


        public void Watch(int redDotId, params object[] args)
        {
            if (RedDotConfigAsset.Instance.DataDic.TryGetValue(redDotId, out var redDotConfigData))
            {
                string path = string.Format(redDotConfigData.Path, args);
                var redDotNode = GetRedDotNode(path);
                if (redDotNode == null)
                {
                    redDotNode = InitRedDotNode(path, redDotConfigData.IsView,
                        redDotConfigData.BindRole, redDotConfigData.ViewType, redDotConfigData.UseLocalSave);
                }

                redDotNode.SetStatus(0);
            }
        }

        public void Watch(string path)
        {
            GetRedDotNode(path)?.SetStatus(0);
        }

        /// <summary>
        /// 注册红点
        /// </summary>
        /// <param name="redDotId">红点id</param>
        /// <param name="changeCb">回调</param>
        /// <param name="args">路径匹配参数</param>
        public void Register(int redDotId, Action<int> changeCb, params object[] args)
        {
            if (RedDotConfigAsset.Instance.DataDic.TryGetValue(redDotId, out var redDotData))
            {
                string path = args.Length > 0 ? string.Format(redDotData.Path, args) : redDotData.Path;
                RedDotNodeBase redDotNode = GetRedDotNode(path);
                if (redDotNode == null)
                {
                    redDotNode = InitRedDotNode(path, redDotData.IsView, redDotData.BindRole, redDotData.ViewType, redDotData.UseLocalSave);
                }

                redDotNode.Register(changeCb);
            }
        }

        /// <summary>
        /// 移除红点数据结构
        /// </summary>
        /// <param name="redDotId"></param>
        /// <param name="args"></param>
        public void RemoveRedDotNode(int redDotId, params object[] args)
        {
            if (RedDotConfigAsset.Instance.DataDic.TryGetValue(redDotId, out var redDotData))
            {
                var redPath = args.Length > 0 ? string.Format(redDotData.Path, args) : redDotData.Path;
                RedDotNodeBase redDotNode = GetRedDotNode(redPath);
                if (redDotNode == null)
                {
                    Debug.LogWarning($"移除红点数据节点失败，路径：{redPath}");
                }
                else
                {
                    redDotNode.Clear();
                }
            }
        }

        /// <summary>
        /// 注销红点
        /// </summary>
        /// <param name="path">红点路径</param>
        /// <param name="changeCb">修改事件</param>
        public void Unregister(string path, Action<int> changeCb)
        {
            GetRedDotNode(path)?.Unregister(changeCb);
        }

        /// <summary>
        /// 储存的特殊key,跟玩家的RoleID绑定
        /// </summary>
        public static string UniqueKey;

        /// <summary>
        /// 本地储存红点数据
        /// </summary>
        public static void LocalSave(bool bindRole, string key, string value)
        {
            string localKey = bindRole ? UniqueKey + key : key;
            PlayerPrefs.SetString(localKey, value);
        }

        /// <summary>
        /// 删除本地储存的红点数据
        /// </summary>
        /// <param name="bindRole"></param>
        /// <param name="key"></param>
        public static void RemoveLocalSave(bool bindRole, string key)
        {
            string localKey = bindRole ? UniqueKey + key : key;
            if (PlayerPrefs.HasKey(localKey))
            {
                PlayerPrefs.DeleteKey(localKey);
            }
        }

        /// <summary>
        /// 获取本地储存的红点数据
        /// </summary>
        public static string GetLocalSaveData(bool bindRole, string key)
        {
            string localKey = bindRole ? UniqueKey + key : key;
            return PlayerPrefs.GetString(localKey);
        }

        #region 工具

        /// <summary> 
        /// 判断两个日期是否在同一周 
        /// </summary> 
        /// <param name="dtmS">开始日期</param> 
        /// <param name="dtmE">结束日期</param>
        /// <returns></returns> 
        private bool IsInSameWeek(DateTime dtmS, DateTime dtmE)
        {
            TimeSpan ts = dtmE - dtmS;
            double dbl = ts.TotalDays;
            int intDow = Convert.ToInt32(dtmE.DayOfWeek);
            if (intDow == 0) intDow = 7;
            if (dbl >= 7 || dbl >= intDow) return false;
            else return true;
        }
        #endregion
    }

    public enum ViewType
    {
        /// <summary>
        /// 一次
        /// </summary>
#if UNITY_EDITOR
        [LabelText("单次查看")]
#endif
        Once,

        /// <summary>
        /// 每日查看
        /// </summary>
#if UNITY_EDITOR
        [LabelText("每日查看")]
#endif
        Day,

        /// <summary>
        /// 每周查看
        /// </summary>
#if UNITY_EDITOR
        [LabelText("每周查看")]
#endif
        Week,

        /// <summary>
        /// 每月查看
        /// </summary>
#if UNITY_EDITOR
        [LabelText("每月查看")]
#endif
        Month,
    }
}