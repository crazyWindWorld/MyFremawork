using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using GameServer.Core.Context;
using GameServer.Core.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameServer.Core.Net;

/// <summary>
/// TCP 监听入口；每个连接生成一个 GameSession 并异步处理。
/// 使用 ConcurrentDictionary 管理所有在线 Session。
/// </summary>
public sealed class GatewayServer : IAsyncDisposable
{
    private readonly GatewayOptions _options;
    private readonly MessageRouter _router;
    private readonly PlayerStateCache _playerCache;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<GatewayServer> _logger;

    // 在线 Session 池
    private readonly ConcurrentDictionary<Guid, GameSession> _sessions = new();

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    /// <summary>当前连接数</summary>
    public int SessionCount => _sessions.Count;

    /// <summary>获取所有在线 Session 快照（供 HeartbeatService 遍历）</summary>
    public IReadOnlyCollection<GameSession> GetAllSessions() => _sessions.Values.ToArray();

    /// <summary>根据 SessionId 查找 Session</summary>
    public GameSession? GetSession(Guid sessionId)
        => _sessions.TryGetValue(sessionId, out var s) ? s : null;

    public GatewayServer(
        IOptions<GatewayOptions> options,
        MessageRouter router,
        PlayerStateCache playerCache,
        ILoggerFactory loggerFactory)
    {
        _options     = options.Value;
        _router      = router;
        _playerCache = playerCache;
        _loggerFactory = loggerFactory;
        _logger      = loggerFactory.CreateLogger<GatewayServer>();
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _listener = new TcpListener(IPAddress.Any, _options.Port);
        _listener.Start(_options.Backlog);

        _logger.LogInformation("[Gateway] 监听端口 {Port}，最大帧 {MaxFrame} bytes",
            _options.Port, _options.MaxFrameSize);

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                TcpClient tcp;
                try
                {
                    tcp = await _listener.AcceptTcpClientAsync(_cts.Token);
                }
                catch (OperationCanceledException) { break; }
                catch (SocketException ex)
                {
                    if (_cts.Token.IsCancellationRequested) break;
                    _logger.LogError("[Gateway] Accept 异常: {Msg}", ex.Message);
                    continue;
                }

                tcp.NoDelay           = true;
                tcp.ReceiveBufferSize = _options.SocketBufferSize;
                tcp.SendBufferSize    = _options.SocketBufferSize;

                var sessionLogger = _loggerFactory.CreateLogger<GameSession>();
                var session = new GameSession(tcp, _options.MaxFrameSize, sessionLogger);
                _sessions.TryAdd(session.SessionId, session);

                // Fire-and-forget，每个 session 独立协程
                _ = HandleSessionAsync(session, _cts.Token);
            }
        }
        finally
        {
            _logger.LogInformation("[Gateway] 停止监听");
        }
    }

    private async Task HandleSessionAsync(GameSession session, CancellationToken ct)
    {
        session.Start();
        _logger.LogInformation("[Gateway] 新连接 SessionId={Id}", session.SessionId);

        try
        {
            // 从 inbound Channel 消费消息 → 路由到对应 Handler
            await foreach (var packet in session.InboundReader.ReadAllAsync(ct))
            {
                await _router.DispatchAsync(session, packet, ct);
            }
        }
        catch (OperationCanceledException) { /* shutdown */ }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Gateway] Session {Id} 处理异常", session.SessionId);
        }
        finally
        {
            // 断连时从在线缓存注销
            if (session.IsLoggedIn)
            {
                _playerCache.Unregister(session.PlayerId);
                _logger.LogInformation("[Gateway] PlayerId={Pid} 断连，已从缓存注销",
                    session.PlayerId);
            }

            _sessions.TryRemove(session.SessionId, out _);
            await session.DisposeAsync();
            _logger.LogInformation("[Gateway] Session {Id} 已断开，当前连接数={Count}",
                session.SessionId, _sessions.Count);
        }
    }

    /// <summary>广播给所有在线 Session（可按条件过滤）</summary>
    public async Task BroadcastAsync(
        GameServer.Core.Protocol.Packet packet,
        Predicate<GameSession>? filter = null,
        CancellationToken ct = default)
    {
        foreach (var (_, s) in _sessions)
        {
            if (filter is null || filter(s))
                await s.SendAsync(packet, ct);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _listener?.Stop();

        foreach (var (_, s) in _sessions)
            await s.DisposeAsync();

        _sessions.Clear();
        _cts?.Dispose();
    }
}

/// <summary>网关配置（从 appsettings.json Gateway 节注入）</summary>
public sealed class GatewayOptions
{
    public int Port           { get; set; } = 9000;
    public int Backlog        { get; set; } = 128;
    public int MaxFrameSize   { get; set; } = 10 * 1024 * 1024; // 10 MB
    public int SocketBufferSize { get; set; } = 65536;           // 64 KB
}
