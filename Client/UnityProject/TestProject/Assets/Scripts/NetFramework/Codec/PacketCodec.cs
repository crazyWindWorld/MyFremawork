using System;
using NetFramework.Core;

namespace NetFramework.Codec
{
    /// <summary>
    /// 数据包编解码器
    /// 包格式: [Length(4字节, big-endian)] [CmdId(4字节, big-endian)] [Body(N字节)]
    /// Length = CmdId(4) + Body.Length
    /// </summary>
    public static class PacketCodec
    {
        /// <summary>
        /// 长度字段占用的字节数
        /// </summary>
        public const int HeaderLengthSize = 4;

        /// <summary>
        /// CmdId 字段占用的字节数
        /// </summary>
        public const int CmdIdSize = 4;

        /// <summary>
        /// 包头总长度 (Length + CmdId)
        /// </summary>
        public const int TotalHeaderSize = HeaderLengthSize + CmdIdSize;

        // 线程局部缓冲区，用于 Encode 过程中临时写入头部，避免每次分配临时数组
        [ThreadStatic] private static byte[] _encodeBuffer;

        /// <summary>
        /// 将消息编码为完整数据包字节
        /// </summary>
        /// <param name="cmdId">消息命令号</param>
        /// <param name="body">Protobuf 序列化后的消息体</param>
        /// <returns>完整数据包字节 (含长度头)</returns>
        public static byte[] Encode(uint cmdId, byte[] body)
        {
            int bodyLen = body?.Length ?? 0;
            // Length = CmdId(4) + Body.Length
            int length = CmdIdSize + bodyLen;
            int totalLen = HeaderLengthSize + length;

            // 获取或创建线程局部缓冲区
            byte[] buf = _encodeBuffer;
            if (buf == null || buf.Length < totalLen)
            {
                // 按 2 的幂次方扩容，减少频繁重新分配
                int size = 1024;
                while (size < totalLen) size <<= 1;
                buf = new byte[size];
                _encodeBuffer = buf;
            }

            // 写入 Length (big-endian int32)
            buf[0] = (byte)(length >> 24);
            buf[1] = (byte)(length >> 16);
            buf[2] = (byte)(length >> 8);
            buf[3] = (byte)length;

            // 写入 CmdId (big-endian uint32)
            buf[4] = (byte)(cmdId >> 24);
            buf[5] = (byte)(cmdId >> 16);
            buf[6] = (byte)(cmdId >> 8);
            buf[7] = (byte)cmdId;

            // 写入 Body
            if (body != null && bodyLen > 0)
            {
                Buffer.BlockCopy(body, 0, buf, TotalHeaderSize, bodyLen);
            }

            // 从线程局部缓冲区复制出精确大小的包（socket send 需要独立生命周期）
            byte[] packet = new byte[totalLen];
            Buffer.BlockCopy(buf, 0, packet, 0, totalLen);
            return packet;
        }

        /// <summary>
        /// 从字节缓冲区解码一个完整的数据包
        /// Body 使用 ArraySegment 直接引用接收缓冲区，零拷贝
        /// </summary>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="offset">起始偏移</param>
        /// <param name="available">可用字节数</param>
        /// <param name="packet">输出的数据包</param>
        /// <returns>消耗的字节数，数据不足时返回 0</returns>
        public static int Decode(byte[] buffer, int offset, int available, out Packet packet)
        {
            packet = null;

            // 至少需要 4 字节的 Length 头
            if (available < HeaderLengthSize)
                return 0;

            // 读取 Length (big-endian int32)
            int length = (buffer[offset] << 24)
                       | (buffer[offset + 1] << 16)
                       | (buffer[offset + 2] << 8)
                       | buffer[offset + 3];

            // 检查数据是否完整
            if (available < HeaderLengthSize + length)
                return 0;

            // 读取 CmdId (big-endian uint32)
            uint cmdId = (uint)((buffer[offset + HeaderLengthSize] << 24)
                              | (buffer[offset + HeaderLengthSize + 1] << 16)
                              | (buffer[offset + HeaderLengthSize + 2] << 8)
                              | buffer[offset + HeaderLengthSize + 3]);

            // Body 直接引用接收缓冲区，零拷贝
            int bodyLen = length - CmdIdSize;
            ArraySegment<byte> body;
            if (bodyLen > 0)
            {
                body = new ArraySegment<byte>(buffer, offset + TotalHeaderSize, bodyLen);
            }
            else
            {
                body = new ArraySegment<byte>(Array.Empty<byte>());
            }

            packet = new Packet(cmdId, body);
            return HeaderLengthSize + length;
        }
    }
}
