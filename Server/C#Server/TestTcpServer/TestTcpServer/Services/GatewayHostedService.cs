using GameServer.Core.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameServer.Services;

/// <summary>
/// IHostedService 包装：TCP 监听服务（GatewayServer 的生命周期管理）
/// </summary>
public sealed class GatewayHostedService : IHostedService
{
    private readonly GatewayServer _gateway;
    private readonly ILogger<GatewayHostedService> _logger;
    private Task? _serverTask;
    private CancellationTokenSource? _cts;

    public GatewayHostedService(
        GatewayServer gateway,
        ILogger<GatewayHostedService> logger)
    {
        _gateway = gateway;
        _logger  = logger;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _serverTask = _gateway.StartAsync(_cts.Token);
        _logger.LogInformation("[GatewayHostedService] 已启动");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _logger.LogInformation("[GatewayHostedService] 正在停止...");
        _cts?.Cancel();
        if (_serverTask is not null)
        {
            try { await _serverTask.WaitAsync(TimeSpan.FromSeconds(10), ct); }
            catch { /* ignore */ }
        }
        await _gateway.DisposeAsync();
        _logger.LogInformation("[GatewayHostedService] 已停止");
    }
}
