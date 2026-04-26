using System;
using System.Collections.Generic;
using Google.Protobuf;

namespace NetFramework
{
    /// <summary>
    /// 消息分发器 - 管理消息回调注册和分发
    /// </summary>
    public class MessageDispatcher
    {
        // 消息处理器字典：MsgId -> Handler
        private Dictionary<ushort, List<Action<IMessage>>> _handlers = new Dictionary<ushort, List<Action<IMessage>>>();
        private Dictionary<ushort, List<Action<IMessage, IMessage>>> _requestResponseHandlers = new Dictionary<ushort, List<Action<IMessage, IMessage>>>();
        private readonly object _lock = new object();


        /// <summary>
        /// 注册消息处理器
        /// </summary>
        public void Register<T>(ushort msgId, Action<T> handler) where T : IMessage,new()
        {
            lock (_lock)
            {
                if (!_handlers.TryGetValue(msgId, out var list))
                {
                    list = new List<Action<IMessage>>();
                    _handlers[msgId] = list;
                }
                list.Add((IMessage msg) => { handler?.Invoke((T)msg); });
            }
        }


        /// <summary>
        /// 注册消息处理器
        /// </summary>
        public void Register<T1,T2>(ushort msgId, Action<T1,T2> handler) where T1 : IMessage,new() where T2 : IMessage,new()
        {
            lock (_lock)
            {
                if (!_requestResponseHandlers.TryGetValue(msgId, out var list))
                {
                    list = new List<Action<IMessage, IMessage>>();
                    _requestResponseHandlers[msgId] = list;
                }
                list.Add((IMessage rsp, IMessage req) => { handler?.Invoke((T1)rsp, (T2)req); });
            }
        }

        /// <summary>
        /// 取消注册消息处理器
        /// </summary>
        public void Unregister<T>(ushort msgId, Action<T> handler) where T : IMessage,new()
        {
            lock (_lock)
            {
                if (_handlers.TryGetValue(msgId, out var list))
                {
                    list.Remove((IMessage msg) => { handler?.Invoke((T)msg); });
                    if (list.Count == 0)
                    {
                        _handlers.Remove(msgId);
                    }
                }
            }
        }

        /// <summary>
        /// 分发消息 - 所有消息都会走这里
        /// </summary>
        public void Dispatch(ushort msgId, IMessage msg, IMessage request)
        {
            if (request != null)
            {
                lock (_lock)
                {
                    if (_requestResponseHandlers.TryGetValue(msgId, out var list) && list.Count > 0)
                    {
                        foreach (var handler in list)
                        {
                            try
                            {
                                handler(request, msg);
                            }
                            catch (Exception ex)
                            {
                                UnityEngine.Debug.LogError($"[MessageDispatcher] Handler error for msgId {msgId}: {ex}");
                            }
                        }
                    }
                }
            }
            else
            {
                lock (_lock)
                {
                    if (_handlers.TryGetValue(msgId, out var list) && list.Count > 0)
                    {
                        foreach (var handler in list)
                        {
                            try
                            {
                                handler?.Invoke(msg);
                            }
                            catch (Exception ex)
                            {
                                UnityEngine.Debug.LogError($"[MessageDispatcher] Handler error for msgId {msgId}: {ex}");
                            }
                        }
                    }
                }

            }
        }

        

        /// <summary>
        /// 清空所有注册
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _handlers.Clear();
                _requestResponseHandlers.Clear();
            }
        }


    }
}
