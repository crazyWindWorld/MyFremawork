using System;
using System.Reflection;

namespace NetFramework.Attributes
{
    /// <summary>
    /// 通过消息类型名从 ProtoCmds 中查找对应的命令号
    /// 例: typeof(PONG) → ProtoCmds.PONG → 4001
    /// </summary>
    public static class ProtoCmdsLookup
    {
        private static Type _cmdsType;
        private static Type CmdsType
        {
            get
            {
                if (_cmdsType == null)
                    _cmdsType = Type.GetType("ProtoCmds");
                return _cmdsType;
            }
        }

        /// <summary>
        /// 根据消息类型查找 ProtoCmds 中对应的命令号
        /// </summary>
        /// <param name="msgType">消息类型 (如 typeof(PONG))</param>
        /// <returns>命令号，未找到返回 0</returns>
        public static uint GetMsgId(Type msgType)
        {
            if (msgType == null || CmdsType == null) return 0;

            FieldInfo field = CmdsType.GetField(
                msgType.Name,
                BindingFlags.Public | BindingFlags.Static);

            if (field != null && field.IsLiteral && !field.IsInitOnly)
            {
                object value = field.GetRawConstantValue();
                return Convert.ToUInt32(value);
            }

            UnityEngine.Debug.LogWarning(
                $"[ProtoCmdsLookup] No ProtoCmds field found for type '{msgType.Name}'");
            return 0;
        }
    }

    /// <summary>
    /// 标记网络消息处理方法的特性
    /// 被标记的方法必须是静态方法，用于自动注册到 MessageDispatcher
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class NetMessageHandlerAttribute : Attribute
    {
        /// <summary>
        /// 响应/Push 消息类型
        /// </summary>
        public Type MsgType { get; }

        /// <summary>
        /// 请求消息类型 (可选，用于 Request-Response 模式)
        /// </summary>
        public Type ReqMsgType { get; }

        /// <summary>
        /// 通过 MsgType 的类型名从 ProtoCmds 反射查找到的命令号
        /// 例: MsgType=typeof(PONG) → MsgId=ProtoCmds.PONG=4001
        /// </summary>
        public uint MsgId => ProtoCmdsLookup.GetMsgId(MsgType);

        /// <summary>
        /// 注册一个 Push/响应 消息的处理器
        /// MsgId 通过 msgType 的类名从 ProtoCmds 自动查找
        /// </summary>
        /// <param name="msgType">消息类型 (如 typeof(PONG))</param>
        public NetMessageHandlerAttribute(Type msgType)
        {
            MsgType = msgType;
        }

        /// <summary>
        /// 注册一个 Request-Response 消息的处理器
        /// MsgId 通过 msgType 的类名从 ProtoCmds 自动查找
        /// </summary>
        /// <param name="msgType">响应消息类型 (如 typeof(LoginRsp))</param>
        /// <param name="reqMsgType">请求消息类型 (如 typeof(LoginReq))</param>
        public NetMessageHandlerAttribute(Type msgType, Type reqMsgType)
        {
            MsgType = msgType;
            ReqMsgType = reqMsgType;
        }
    }
}
