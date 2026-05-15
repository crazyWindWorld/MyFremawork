using System.Collections.Concurrent;
using GameServer.Core.Net;
using GameServer.Core.Protocol;
using Google.Protobuf;
using LoginPB;
using Microsoft.Extensions.Logging;

namespace GameServer.Core.Context;

/// <summary>
/// 在线玩家状态缓存（内存级，全局单例）。
/// 用于快速查找在线 Session、广播、踢人等操作。
/// 跨节点数据同步请使用 Redis（生产）。
/// </summary>
public sealed class PlayerStateCache
{
    private readonly ConcurrentDictionary<long, GameSession> _playerToSession = new();
    private readonly ILogger<PlayerStateCache> _logger;

    public PlayerStateCache(ILogger<PlayerStateCache> logger) => _logger = logger;

    /// <summary>注册玩家（登录成功后调用）</summary>
    public void Register(GameSession session)
    {
        if (session.PlayerId <= 0) return;
        _playerToSession[session.PlayerId] = session;
        _logger.LogInformation("[PlayerCache] 注册 PlayerId={Id}, Session={Sid}",
            session.PlayerId, session.SessionId);
    }

    /// <summary>注销玩家（断连/登出后调用）</summary>
    public void Unregister(long playerId)
    {
        if (playerId <= 0) return;
        _playerToSession.TryRemove(playerId, out _);
        _logger.LogInformation("[PlayerCache] 注销 PlayerId={Id}", playerId);
    }

    /// <summary>获取玩家 Session</summary>
    public GameSession? GetSession(long playerId)
        => _playerToSession.TryGetValue(playerId, out var s) ? s : null;

    /// <summary>玩家是否在线</summary>
    public bool IsOnline(long playerId) => _playerToSession.ContainsKey(playerId);

    /// <summary>当前在线人数</summary>
    public int OnlineCount => _playerToSession.Count;

    /// <summary>获取所有在线 Session 快照</summary>
    public IReadOnlyCollection<GameSession> AllSessions => _playerToSession.Values.ToArray();

    /// <summary>获取所有在线 PlayerId</summary>
    public IReadOnlyCollection<long> GetAllPlayerIds() => _playerToSession.Keys.ToArray();

    /// <summary>
    /// 踢玩家下线：发送 LogoutPush → 关闭旧连接 → 从缓存移除。
    /// 返回 true 表示成功踢出，false 表示玩家不在线。
    /// </summary>
    public async Task<bool> TryKickAsync(long playerId, CancellationToken ct = default)
    {
        if (!_playerToSession.TryRemove(playerId, out var session))
            return false;

        _logger.LogWarning("[PlayerCache] 踢人下线 PlayerId={Id}, Session={Sid}",
            playerId, session.SessionId);

        // 发送 LogoutPush
        try
        {
            var push   = new LogoutPush { LoginId = playerId };
            var packet = new Packet(ProtoCmds.KickPush, push.ToByteArray());
            await session.SendAsync(packet, ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("[PlayerCache] 发送 LogoutPush 失败（连接可能已断）: {Msg}", ex.Message);
        }

        // 标记登出并关闭连接
        session.MarkLogout();
        session.Close();
        return true;
    }
}
