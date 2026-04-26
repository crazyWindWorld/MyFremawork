using System;
using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;

namespace NetFramework
{
    public class MessageCacheManager
    {
        // 缓存项
        private class CacheItem
        {
            public ushort MsgId;
            public IMessage Request;
            public float SendTime;
        }

        // 缓存配置
        public float CacheTimeout { get; set; } = 30f; // 缓存超时时间（秒）
        public int MaxCacheSize { get; set; } = 1000;  // 最大缓存数量

        // 缓存字典：MsgId -> 请求列表（同一MsgId可能有多个请求）
        private Dictionary<ushort, List<CacheItem>> _cache = new Dictionary<ushort, List<CacheItem>>();
        private readonly object _lock = new object();

        /// <summary>
        /// 缓存发送的请求
        /// </summary>
        public void CacheRequest(ushort msgId, IMessage request)
        {
            if (request == null) return;

            lock (_lock)
            {
                if (!_cache.TryGetValue(msgId, out var list))
                {
                    list = new List<CacheItem>();
                    _cache[msgId] = list;
                }

                // 检查缓存数量限制
                if (list.Count >= MaxCacheSize)
                {
                    list.RemoveAt(0); // 移除最旧的
                }

                list.Add(new CacheItem
                {
                    MsgId = msgId,
                    Request = request,
                    SendTime = Time.time
                });
            }
        }

        /// <summary>
        /// 尝试获取并移除缓存的请求
        /// </summary>
        public IMessage TryGetRequest(ushort msgId)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(msgId, out var list) && list.Count > 0)
                {
                    // 找到最早发送的请求
                    var item = list[0];
                    list.RemoveAt(0);

                    // 清理空列表
                    if (list.Count == 0)
                    {
                        _cache.Remove(msgId);
                    }

                    return item.Request;
                }
                return null;
            }
        }

        /// <summary>
        /// 清理超时的缓存
        /// </summary>
        public void CleanupTimeout()
        {
            lock (_lock)
            {
                var currentTime = Time.time;
                var keysToRemove = new List<ushort>();

                foreach (var kvp in _cache)
                {
                    var list = kvp.Value;
                    list.RemoveAll(item => currentTime - item.SendTime > CacheTimeout);

                    if (list.Count == 0)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                }
            }
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _cache.Clear();
            }
        }

        /// <summary>
        /// 获取当前缓存数量
        /// </summary>
        public int GetCacheCount()
        {
            lock (_lock)
            {
                int count = 0;
                foreach (var list in _cache.Values)
                {
                    count += list.Count;
                }
                return count;
            }
        }
    }
}
