using System;

namespace NetFramework.Core
{
    /// <summary>
    /// 网络数据包模型
    /// 包格式: [Length(4字节)] [CmdId(4字节)] [Body(N字节)]
    /// Body 使用 ArraySegment 直接引用接收缓冲区，避免拷贝分配
    /// </summary>
    public class Packet
    {
        /// <summary>
        /// 消息命令号 (对应 ProtoCmds)
        /// </summary>
        public uint CmdId { get; set; }

        /// <summary>
        /// Protobuf 序列化后的消息体（零拷贝，直接引用接收缓冲区）
        /// 注意: Body 在消息处理前有效，之后缓冲区可能被覆盖
        /// </summary>
        public ArraySegment<byte> Body { get; set; }

        public Packet() { }

        public Packet(uint cmdId, ArraySegment<byte> body)
        {
            CmdId = cmdId;
            Body = body;
        }
    }
}
