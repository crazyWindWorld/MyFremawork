using System;
using Google.Protobuf;
using UnityEngine;

namespace NetFramework.Attributes
{
    /// <summary>
    /// 消息处理器特性 - 标记方法自动注册为消息处理器
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class NetMessageHandlerAttribute : Attribute
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        public ushort MsgId { get; }
        public Type ReqMsgType { get; }
        public Type RespMsgType { get; }

        public NetMessageHandlerAttribute(Type respMsgType, Type reqMsgType = null)
        {
            Type protoCmdsType = typeof(ProtoCmds);
            if (protoCmdsType.GetField(respMsgType.Name) == null)
            {
                Debug.LogError($"ProtoCmds 中不存在 {respMsgType.Name} 类型");
                return;
            }
            MsgId = (ushort)protoCmdsType.GetField(respMsgType.Name).GetValue(null);
            ReqMsgType = reqMsgType;
            RespMsgType = respMsgType;
        }
    }
}
