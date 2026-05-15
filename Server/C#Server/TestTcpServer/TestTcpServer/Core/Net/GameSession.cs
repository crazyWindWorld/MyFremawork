using System.Net.Sockets;
using System.Threading.Channels;
using GameServer.Core.Protocol;
using Microsoft.Extensions.Logging;

namespace GameServer.Core.Net;

/// <summary>
/// 一个客户端 TCP 连接的全生命周期管理。
/// 职责：
///   1. 独立 recv 循环 → 写入 inbound Channel
///   2. 独立 send 循环 ← 读取 outbound Channel
///   3. 暴露 SendAsync / Close 给业务层
/// </summary>
public sealed class GameSession : IAsyncDisposable
{
    // ─── 唯一 ID ─────────────────────────────────────────────────
    public Guid SessionId { get; } = Guid.NewGuid();

    // ─── 玩家业务标识（认证后填写） ────────────────────────────────
    public long PlayerId { get; set; } = 0;

    // ─── 心跳 & 登录状态 ──────────────────────────────────────────

    /// <summary>是否已登录（PlayerId > 0 且 LoginTime 有值）</summary>
    public bool IsLoggedIn => PlayerId > 0 && LoginTime.HasValue;

    /// <summary>登录成功时间</summary>
    public DateTime? LoginTime { get; private set; }

    // 用 long 存 Ticks，通过 Volatile 保证线程安全
    private long _lastActiveUtc = DateTime.UtcNow.Ticks;

    /// <summary>线程安全地更新最后活跃时间（PingHandler 和其他 Handler 调用）</summary>
    public void MarkActive() =>
        Volatile.Write(ref _lastActiveUtc, DateTime.UtcNow.Ticks);

    /// <summary>线程安全地读取最后活跃时间（HeartbeatService 调用）</summary>
    public DateTime GetLastActive() =>
        new DateTime(Volatile.Read(ref _lastActiveUtc), DateTimeKind.Utc);

    /// <summary>标记登录成功：设置 PlayerId、LoginTime，并刷新活跃时间</summary>
    public void MarkLogin(long playerId)
    {
        PlayerId  = playerId;
        LoginTime = DateTime.UtcNow;
        MarkActive();
    }

    /// <summary>标记登出：清除 PlayerId 和 LoginTime</summary>
    public void MarkLogout()
    {
        PlayerId  = 0;
        LoginTime = null;
    }

    // ─── 额外 KV 上下文（Handler 可自由存放短暂状态）─────────────
    private readonly Dictionary<string, object?> _props = new();
    public T? GetProp<T>(string key) =>
        _props.TryGetValue(key, out var v) ? (T?)v : default;
    public void SetProp(string key, object? value) => _props[key] = value;

    // ─── 内部 ──────────────────────────────────────────────────────
    private readonly TcpClient _tcp;
    private readonly NetworkStream _stream;
    private readonly int _maxFrameSize;
    private readonly ILogger<GameSession> _logger;

    // outbound 有界队列，防止单客户端写爆内存
    private readonly Channel<Packet> _outbound =
        Channel.CreateBounded<Packet>(new BoundedChannelOptions(512)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode     = BoundedChannelFullMode.DropOldest
        });

    // inbound 无界（由上层背压保证）
    private readonly Channel<Packet> _inbound =
        Channel.CreateUnbounded<Packet>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

    private readonly CancellationTokenSource _cts = new();
    private Task? _recvTask;
    private Task? _sendTask;

    public ChannelReader<Packet> InboundReader => _inbound.Reader;

    public GameSession(TcpClient tcp, int maxFrameSize, ILogger<GameSession> logger)
    {
        _tcp          = tcp;
        _stream       = tcp.GetStream();
        _maxFrameSize = maxFrameSize;
        _logger       = logger;
    }

    // ─── 启动收发循环 ──────────────────────────────────────────────
    public void Start()
    {
        _recvTask = RecvLoopAsync(_cts.Token);
        _sendTask = SendLoopAsync(_cts.Token);
    }

    // ─── 业务层调用：推一个包进发送队列 ───────────────────────────
    public ValueTask SendAsync(Packet packet, CancellationToken ct = default)
        => _outbound.Writer.TryWrite(packet)
            ? ValueTask.CompletedTask
            : _outbound.Writer.WriteAsync(packet, ct);

    // ─── 关闭（幂等）────────────────────────────────────────────────
    public void Close()
    {
        if (_cts.IsCancellationRequested) return;
        _cts.Cancel();
        try { _tcp.Close(); } catch { /* ignore */ }
        _outbound.Writer.TryComplete();
        _inbound.Writer.TryComplete();
    }

    // ─── 接收循环 ─────────────────────────────────────────────────
    private async Task RecvLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var packet = await FrameCodec.ReadPacketAsync(_stream, _maxFrameSize, ct);
                if (packet is null)
                {
                    _logger.LogInformation("[Session {Id}] EOF", SessionId);
                    break;
                }
                await _inbound.Writer.WriteAsync(packet, ct);
            }
        }
        catch (OperationCanceledException) { /* normal shutdown */ }
        catch (Exception ex) when (ex is IOException or SocketException)
        {
            _logger.LogWarning("[Session {Id}] recv error: {Msg}", SessionId, ex.Message);
        }
        finally
        {
            _inbound.Writer.TryComplete();
            Close();
        }
    }

    // ─── 发送循环 ─────────────────────────────────────────────────
    private async Task SendLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var packet in _outbound.Reader.ReadAllAsync(ct))
            {
                await FrameCodec.WritePacketAsync(_stream, packet, ct);
            }
        }
        catch (OperationCanceledException) { /* normal shutdown */ }
        catch (Exception ex) when (ex is IOException or SocketException)
        {
            _logger.LogWarning("[Session {Id}] send error: {Msg}", SessionId, ex.Message);
        }
    }

    public async ValueTask DisposeAsync()
    {
        Close();
        if (_recvTask is not null) await _recvTask.ConfigureAwait(false);
        if (_sendTask is not null) await _sendTask.ConfigureAwait(false);
        _stream.Dispose();
        _tcp.Dispose();
        _cts.Dispose();
    }
}
