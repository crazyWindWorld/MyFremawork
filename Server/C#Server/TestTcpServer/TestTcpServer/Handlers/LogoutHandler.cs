using GameServer.Core.Context;
using GameServer.Core.Net;
using GameServer.Core.Protocol;
using GameServer.Core.Routing;
using Microsoft.Extensions.Logging;

namespace GameServer.Handlers;

/// <summary>
/// 主动登出处理器：客户端发送 LogoutReq → 清理 Session → 返回 LogoutRsp
/// </summary>
[MsgHandler(ProtoCmds.LogoutReq, RequireLogin = true)]
public sealed class LogoutHandler : HandlerBase
{
    private readonly PlayerStateCache _playerCache;
    private readonly ILogger<LogoutHandler> _logger;

    public LogoutHandler(PlayerStateCache playerCache, ILogger<LogoutHandler> logger)
    {
        _playerCache = playerCache;
        _logger      = logger;
    }

    public override async ValueTask HandleAsync(
        GameSession session,
        Packet packet,
        CancellationToken ct)
    {
        var playerId = session.PlayerId;
        _logger.LogInformation("[Logout] PlayerId={Id} 主动登出", playerId);

        // 从在线缓存移除
        _playerCache.Unregister(playerId);

        // 标记 Session 已登出（不断连，允许重新登录）
        session.MarkLogout();

        // 返回确认响应
        await session.SendAsync(Packet.Empty(ProtoCmds.LogoutRsp), ct);
    }
}
