namespace GameServer.Core.Protocol;

/// <summary>
/// 网络帧：msgId + 原始 body 字节
/// 协议格式：[4B totalLen(BE)][4B msgId(BE)][body...]
///   totalLen = 4 + body.Length
/// </summary>
public sealed class Packet
{
    public int MsgId { get; }
    public ReadOnlyMemory<byte> Body { get; }

    public Packet(int msgId, ReadOnlyMemory<byte> body)
    {
        MsgId = msgId;
        Body = body;
    }

    public static Packet Empty(int msgId) => new(msgId, ReadOnlyMemory<byte>.Empty);
}
