using System;
using System.Collections.Generic;
using Fuel.Singleton;

namespace Fuel.Event
{
    /// <summary>
    /// 事件处理器容器
    /// </summary>
    internal class EventHandlerList<T>
    {
        private Action<T> _handlers;

        public void Add(Action<T> handler) => _handlers += handler;
        public void Remove(Action<T> handler) => _handlers -= handler;
        public void Invoke(T arg) => _handlers?.Invoke(arg);
        public void Clear() => _handlers = null;
        public bool HasHandlers => _handlers != null;
    }

    /// <summary>
    /// 通用事件分发器
    /// 通过消息类型注册和触发事件，使用纯 Action 实现
    /// </summary>
    public class EventDispatcher : Singleton<EventDispatcher>
    {
        private static int _typeIdCounter;
        private static readonly Dictionary<Type, int> _typeIdMap = new Dictionary<Type, int>();
        private readonly Dictionary<int, object> _events = new Dictionary<int, object>();

        private static int GetTypeId<T>()
        {
            Type type = typeof(T);
            if (!_typeIdMap.TryGetValue(type, out int id))
            {
                id = _typeIdCounter++;
                _typeIdMap[type] = id;
            }
            return id;
        }

        private EventHandlerList<T> GetHandlerList<T>() where T : IEventMessage
        {
            int id = GetTypeId<T>();
            if (!_events.TryGetValue(id, out object obj))
            {
                obj = new EventHandlerList<T>();
                _events[id] = obj;
            }
            return (EventHandlerList<T>)obj;
        }

        #region Register

        public void Register<T>(Action<T> handler) where T : IEventMessage
        {
            GetHandlerList<T>().Add(handler);
        }

        #endregion

        #region Unregister

        public void Unregister<T>(Action<T> handler) where T : IEventMessage
        {
            int id = GetTypeId<T>();
            if (_events.TryGetValue(id, out object obj))
            {
                var list = (EventHandlerList<T>)obj;
                list.Remove(handler);
                if (!list.HasHandlers)
                {
                    _events.Remove(id);
                }
            }
        }

        #endregion

        #region Dispatch

        public void Dispatch<T>(T message) where T : IEventMessage
        {
            if (_events.TryGetValue(GetTypeId<T>(), out object obj))
            {
                ((EventHandlerList<T>)obj).Invoke(message);
            }
        }

        #endregion

        #region Clear

        public void Clear<T>() where T : IEventMessage
        {
            _events.Remove(GetTypeId<T>());
        }

        public void ClearAll()
        {
            _events.Clear();
        }

        #endregion
    }
}
