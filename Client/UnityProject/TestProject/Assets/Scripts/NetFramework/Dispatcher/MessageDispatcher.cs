using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;

namespace NetFramework.Dispatcher
{
    /// <summary>
    /// 消息分发器
    /// 负责注册消息处理器，并在收到消息时分发到对应处理器
    /// 通过主线程队列确保 handler 在 Unity 主线程执行
    /// </summary>
    public class MessageDispatcher
    {
        /// <summary>
        /// 无返回值的消息处理器 (用于 Push 消息或只处理响应)
        /// </summary>
        private delegate void MessageHandler(uint cmdId, ArraySegment<byte> body);

        /// <summary>
        /// 有返回值的消息处理器 (用于 Request-Response 模式)
        /// </summary>
        private delegate IMessage RequestHandler(uint cmdId, IMessage request);

        private readonly Dictionary<uint, MessageHandler> _handlers = new Dictionary<uint, MessageHandler>();
        private readonly Dictionary<uint, RequestHandler> _requestHandlers = new Dictionary<uint, RequestHandler>();

        // 主线程派发队列
        private readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();

        #region Register

        /// <summary>
        /// 注册消息处理器 (仅响应类型，用于 Push 消息)
        /// </summary>
        /// <typeparam name="TResp">响应/Push 消息类型</typeparam>
        /// <param name="cmdId">消息命令号</param>
        /// <param name="handler">处理器回调</param>
        public void Register<TResp>(uint cmdId, Action<TResp> handler) where TResp : IMessage<TResp>, new()
        {
            if (_handlers.ContainsKey(cmdId))
            {
                Debug.LogWarning($"[MessageDispatcher] Handler for cmd {cmdId} already registered, overwriting.");
            }

            _handlers[cmdId] = (id, body) =>
            {
                TResp msg = new TResp();
                msg.MergeFrom(body.Array, body.Offset, body.Count);
                handler?.Invoke(msg);
            };
        }

        /// <summary>
        /// 注册消息处理器 (仅响应类型，无泛型约束版本，用于 IMessage)
        /// </summary>
        /// <param name="cmdId">消息命令号</param>
        /// <param name="respParser">响应消息的 MessageParser</param>
        /// <param name="handler">处理器回调</param>
        public void Register(uint cmdId, MessageParser respParser, Action<IMessage> handler)
        {
            if (_handlers.ContainsKey(cmdId))
            {
                Debug.LogWarning($"[MessageDispatcher] Handler for cmd {cmdId} already registered, overwriting.");
            }

            _handlers[cmdId] = (id, body) =>
            {
                IMessage msg = respParser.ParseFrom(body.Array, body.Offset, body.Count);
                handler?.Invoke(msg);
            };
        }

        /// <summary>
        /// 注册 Request-Response 消息处理器
        /// </summary>
        /// <typeparam name="TReq">请求消息类型</typeparam>
        /// <typeparam name="TResp">响应消息类型</typeparam>
        /// <param name="cmdId">消息命令号</param>
        /// <param name="handler">处理器回调，接收请求消息，返回响应消息</param>
        public void Register<TReq, TResp>(uint cmdId, Func<TReq, TResp> handler)
            where TReq : IMessage<TReq>, new()
            where TResp : IMessage<TResp>, new()
        {
            if (_requestHandlers.ContainsKey(cmdId))
            {
                Debug.LogWarning($"[MessageDispatcher] Request handler for cmd {cmdId} already registered, overwriting.");
            }

            _requestHandlers[cmdId] = (id, request) =>
            {
                TResp resp = handler.Invoke((TReq)request);
                return resp;
            };
        }

        #endregion

        #region Dispatch

        /// <summary>
        /// 分发收到的消息到主线程队列
        /// </summary>
        /// <param name="cmdId">消息命令号</param>
        /// <param name="body">Protobuf 消息体 (ArraySegment, 零拷贝引用接收缓冲区)</param>
        public void Dispatch(uint cmdId, ArraySegment<byte> body)
        {
            if (_handlers.TryGetValue(cmdId, out MessageHandler handler))
            {
                _mainThreadQueue.Enqueue(() =>
                {
                    try
                    {
                        handler.Invoke(cmdId, body);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[MessageDispatcher] Handler error for cmd {cmdId}: {e}");
                    }
                });
            }
            else if (_requestHandlers.TryGetValue(cmdId, out RequestHandler reqHandler))
            {
                _mainThreadQueue.Enqueue(() =>
                {
                    try
                    {
                        Debug.LogWarning($"[MessageDispatcher] Request handler for cmd {cmdId} should be used with SendRequest, not Dispatch.");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[MessageDispatcher] Request handler error for cmd {cmdId}: {e}");
                    }
                });
            }
            else
            {
                Debug.LogWarning($"[MessageDispatcher] No handler registered for cmd {cmdId}.");
            }
        }

        /// <summary>
        /// 处理 Request-Response 模式的消息
        /// 收到响应时调用，从等待队列中找到对应的请求处理器
        /// </summary>
        /// <param name="cmdId">响应消息命令号</param>
        /// <param name="body">响应消息体</param>
        /// <param name="reqCmdId">对应的请求命令号</param>
        /// <param name="request">原始请求消息</param>
        public void DispatchRequestResponse(uint cmdId, ArraySegment<byte> body, uint reqCmdId, IMessage request)
        {
            if (!_requestHandlers.TryGetValue(reqCmdId, out RequestHandler reqHandler))
            {
                Debug.LogWarning($"[MessageDispatcher] No request handler for req cmd {reqCmdId}.");
                return;
            }

            _mainThreadQueue.Enqueue(() =>
            {
                try
                {
                    IMessage response = reqHandler.Invoke(reqCmdId, request);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MessageDispatcher] Request-Response error for cmd {reqCmdId}: {e}");
                }
            });
        }

        /// <summary>
        /// 在主线程 Update 中调用，处理所有排队的消息
        /// </summary>
        public void Update()
        {
            // 每帧最多处理 100 条消息，避免卡顿
            int processed = 0;
            while (processed < 100 && _mainThreadQueue.TryDequeue(out Action action))
            {
                action?.Invoke();
                processed++;
            }
        }

        #endregion

        #region Unregister

        /// <summary>
        /// 移除指定命令号的处理器
        /// </summary>
        /// <param name="cmdId">消息命令号</param>
        /// <returns>是否成功移除</returns>
        public bool Unregister(uint cmdId)
        {
            return _handlers.Remove(cmdId) || _requestHandlers.Remove(cmdId);
        }

        /// <summary>
        /// 清除所有注册的处理器
        /// </summary>
        public void Clear()
        {
            _handlers.Clear();
            _requestHandlers.Clear();
            while (_mainThreadQueue.TryDequeue(out _)) { }
        }

        #endregion
    }
}
