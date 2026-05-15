using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets;

namespace GameServer.Core.Protocol;

/// <summary>
/// 协议编解码工具
/// 帧格式：[4B totalLen Big-Endian][4B msgId Big-Endian][body bytes...]
/// totalLen = 4（msgId字节数）+ body.Length
/// </summary>
public static class FrameCodec
{
    private const int LenFieldSize = 4;

    /// <summary>读取一个完整帧；返回 null 代表对端关闭连接</summary>
    public static async ValueTask<Packet?> ReadPacketAsync(
        NetworkStream stream,
        int maxFrameSize,
        CancellationToken ct)
    {
        // 1. 读 4 字节 total-length
        byte[] lenBuf = ArrayPool<byte>.Shared.Rent(LenFieldSize);
        try
        {
            int n = await ReadExactlyAsync(stream, lenBuf, LenFieldSize, ct);
            if (n == 0) return null; // 正常 EOF

            int totalLen = BinaryPrimitives.ReadInt32BigEndian(lenBuf);
            if (totalLen < 4 || totalLen > maxFrameSize)
                throw new InvalidDataException($"Invalid frame totalLen={totalLen}");

            // 2. 读 totalLen 字节（含 msgId 4B + body）
            byte[] payload = ArrayPool<byte>.Shared.Rent(totalLen);
            try
            {
                int pn = await ReadExactlyAsync(stream, payload, totalLen, ct);
                if (pn < totalLen) throw new IOException("EOF while reading payload");

                int msgId   = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(0, 4));
                int bodyLen = totalLen - 4;

                // 复制 body（避免 ArrayPool 生命周期问题）
                Memory<byte> body = new byte[bodyLen];
                if (bodyLen > 0)
                    payload.AsSpan(4, bodyLen).CopyTo(body.Span);

                return new Packet(msgId, body);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(payload);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(lenBuf);
        }
    }

    /// <summary>向流写入一个帧</summary>
    public static async ValueTask WritePacketAsync(
        NetworkStream stream,
        Packet packet,
        CancellationToken ct)
    {
        int bodyLen   = packet.Body.Length;
        int totalLen  = 4 + bodyLen;       // msgId(4) + body
        int frameSize = 4 + totalLen;      // len(4) + msgId(4) + body

        byte[] buf = ArrayPool<byte>.Shared.Rent(frameSize);
        try
        {
            BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(0, 4), totalLen);
            BinaryPrimitives.WriteInt32BigEndian(buf.AsSpan(4, 4), packet.MsgId);
            if (bodyLen > 0)
                packet.Body.Span.CopyTo(buf.AsSpan(8, bodyLen));

            await stream.WriteAsync(buf.AsMemory(0, frameSize), ct);
            await stream.FlushAsync(ct);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buf);
        }
    }

    private static async ValueTask<int> ReadExactlyAsync(
        NetworkStream stream, byte[] buf, int count, CancellationToken ct)
    {
        int total = 0;
        while (total < count)
        {
            int n = await stream.ReadAsync(buf.AsMemory(total, count - total), ct);
            if (n == 0) return total;
            total += n;
        }
        return total;
    }
}
