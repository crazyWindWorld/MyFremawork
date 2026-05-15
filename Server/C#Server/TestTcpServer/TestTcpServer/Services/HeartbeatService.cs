using GameServer.Core.Context;
using GameServer.Core.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameServer.Services;

/// <summary>
/// 心跳检测服务：定期检查所有已登录 Session 是否超时无响应，超时则强制踢下线。
/// 心跳时间戳由 PingHandler 调用 session.MarkActive() 维护。
/// </summary>
public sealed class HeartbeatService : BackgroundService
{
    private readonly GatewayServer _gateway;
    private readonly PlayerStateCache _cache;
    private readonly HeartbeatOptions _options;
    private readonly ILogger<HeartbeatService> _logger;

    public HeartbeatService(
        GatewayServer gateway,
        PlayerStateCache cache,
        IOptions<HeartbeatOptions> options,
        ILogger<HeartbeatService> logger)
    {
        _gateway = gateway;
        _cache   = cache;
        _options = options.Value;
        _logger  = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation(
            "[Heartbeat] 心跳服务已启动，检测间隔 {Interval}s，超时阈值 {Timeout}s",
            _options.CheckIntervalSeconds, _options.TimeoutSeconds);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(_options.CheckIntervalSeconds), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await CheckAllSessionsAsync(ct);
        }

        _logger.LogInformation("[Heartbeat] 心跳服务已停止");
    }

    private async Task CheckAllSessionsAsync(CancellationToken ct)
    {
        var now            = DateTime.UtcNow;
        var timeoutSpan    = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        var sessions       = _gateway.GetAllSessions();
        int onlineCount    = 0;
        int kickedCount    = 0;

        foreach (var session in sessions)
        {
            // 只检测已登录的 Session
            if (!session.IsLoggedIn) continue;

            onlineCount++;
            var elapsed = now - session.GetLastActive();

            if (elapsed > timeoutSpan)
            {
                _logger.LogWarning(
                    "[Heartbeat] 超时踢人 PlayerId={Pid}, Session={Sid}, 超时 {Sec:F0}s",
                    session.PlayerId, session.SessionId, elapsed.TotalSeconds);

                await _cache.TryKickAsync(session.PlayerId, ct);
                kickedCount++;
            }
        }

        _logger.LogInformation(
            "[Heartbeat] 检测完毕 — 在线={Online}, 踢出={Kicked}, 总连接={Total}",
            onlineCount, kickedCount, sessions.Count);
    }
}

/// <summary>心跳配置（从 appsettings.json Heartbeat 节注入）</summary>
public sealed class HeartbeatOptions
{
    /// <summary>检测间隔（秒）</summary>
    public int CheckIntervalSeconds { get; set; } = 30;

    /// <summary>超时阈值（秒）：超过此时间无心跳则踢下线</summary>
    public int TimeoutSeconds { get; set; } = 90;
}
