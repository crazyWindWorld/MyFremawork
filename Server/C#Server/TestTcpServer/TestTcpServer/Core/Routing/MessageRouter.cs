using System.Reflection;
using GameServer.Core.Net;
using GameServer.Core.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameServer.Core.Routing;

/// <summary>
/// 消息路由器。
/// 启动时扫描所有带 [MsgHandler] 的 HandlerBase 子类，
/// 运行时根据 msgId 实例化（通过 DI）并分发消息。
/// 支持 RequireLogin 校验：未登录的 Session 只能访问白名单消息。
/// </summary>
public sealed class MessageRouter
{
    // msgId → (Handler 类型, 是否需要登录)
    private readonly Dictionary<int, (Type HandlerType, bool RequireLogin)> _registry = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessageRouter> _logger;

    public MessageRouter(IServiceProvider serviceProvider, ILogger<MessageRouter> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 扫描指定程序集中带 [MsgHandler] 的 HandlerBase 子类并注册。
    /// 应在应用启动时调用一次。
    /// </summary>
    public void RegisterFromAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || !type.IsSubclassOf(typeof(HandlerBase))) continue;
            var attr = type.GetCustomAttribute<MsgHandlerAttribute>();
            if (attr is null) continue;

            if (_registry.TryGetValue(attr.MsgId, out var existing))
            {
                _logger.LogWarning(
                    "[Router] msgId={Id} 已被 {Existing} 注册，{New} 将被忽略",
                    attr.MsgId, existing.HandlerType.Name, type.Name);
                continue;
            }

            _registry[attr.MsgId] = (type, attr.RequireLogin);
            _logger.LogDebug("[Router] 注册 msgId={Id} → {Type} (RequireLogin={Req})",
                attr.MsgId, type.Name, attr.RequireLogin);
        }

        _logger.LogInformation("[Router] 注册完成，共 {Count} 个 Handler", _registry.Count);
    }

    /// <summary>分发一条消息到对应 Handler（每次请求创建新 Scope）</summary>
    public async ValueTask DispatchAsync(
        GameSession session,
        Packet packet,
        CancellationToken ct)
    {
        if (!_registry.TryGetValue(packet.MsgId, out var entry))
        {
            _logger.LogWarning("[Router] 未注册的 msgId={Id}，已忽略", packet.MsgId);
            return;
        }

        var (handlerType, requireLogin) = entry;

        // ── 登录校验 ─────────────────────────────────────────────
        if (requireLogin && !session.IsLoggedIn)
        {
            _logger.LogWarning(
                "[Router] 未登录 Session={Sid} 尝试访问 msgId={Id}，已拦截",
                session.SessionId, packet.MsgId);
            return;
        }

        // 每条消息独立 DI Scope，Handler 中可安全使用 Scoped 服务（如 DbContext）
        await using var scope = _serviceProvider.CreateAsyncScope();
        var handler = (HandlerBase)scope.ServiceProvider.GetRequiredService(handlerType);

        try
        {
            await handler.HandleAsync(session, packet, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Router] Handler {Type} 处理 msgId={Id} 时抛出异常",
                handlerType.Name, packet.MsgId);
        }
    }

    /// <summary>返回当前注册表快照（调试用）</summary>
    public IReadOnlyDictionary<int, Type> GetRegistry()
        => _registry.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.HandlerType);
}
