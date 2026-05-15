using GameServer.Core.Net;
using GameServer.Core.Protocol;
using GameServer.Core.Routing;
using Microsoft.Extensions.Logging;

namespace GameServer.Handlers;

/// <summary>
/// 心跳 Ping 处理器：收到 PingReq → 更新活跃时间 → 回 PongRsp
/// </summary>
[MsgHandler(ProtoCmds.PingReq, RequireLogin = false)]
public sealed class PingHandler : HandlerBase
{
    // PongRsp 无 body，静态复用
    private static readonly Packet PongPacket = Packet.Empty(ProtoCmds.PongRsp);

    private readonly ILogger<PingHandler> _logger;

    public PingHandler(ILogger<PingHandler> logger) => _logger = logger;

    public override ValueTask HandleAsync(
        GameSession session,
        Packet packet,
        CancellationToken ct)
    {
        // 更新最后活跃时间（HeartbeatService 依赖此时间戳判断超时）
        session.MarkActive();

        _logger.LogDebug("[Ping] Session={Id}, PlayerId={Pid}",
            session.SessionId, session.PlayerId);

        return session.SendAsync(PongPacket, ct);
    }
}
