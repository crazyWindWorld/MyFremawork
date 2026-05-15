using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Core.Routing;

/// <summary>
/// DI 扩展方法：一键注册程序集中所有业务 Handler
/// </summary>
public static class HandlerServiceExtensions
{
    /// <summary>
    /// 扫描 <paramref name="assembly"/>，将所有带 [MsgHandler] 的 HandlerBase 子类
    /// 注册为 Scoped（每条消息一个 Scope）。
    /// </summary>
    public static IServiceCollection AddMsgHandlers(
        this IServiceCollection services,
        Assembly? assembly = null)
    {
        assembly ??= Assembly.GetEntryAssembly()!;

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || !type.IsSubclassOf(typeof(HandlerBase))) continue;
            if (type.GetCustomAttribute<MsgHandlerAttribute>() is null) continue;

            services.AddScoped(type);
        }

        return services;
    }
}
