using GameServer.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Data.InMemory;

/// <summary>
/// DI 扩展：注册 InMemory 数据层
/// </summary>
public static class InMemoryDataExtensions
{
    /// <summary>
    /// 注册 InMemoryRepository 和 InMemoryUnitOfWork。
    /// <para>用法：services.AddInMemoryData(typeof(PlayerEntity), typeof(ItemEntity), ...);</para>
    /// </summary>
    public static IServiceCollection AddInMemoryData(
        this IServiceCollection services,
        params Type[] entityTypes)
    {
        // UnitOfWork
        services.AddScoped<IUnitOfWork, InMemoryUnitOfWork>();

        // 每个实体类型注册为 Singleton（InMemory 仓储全局共享）
        foreach (var entityType in entityTypes)
        {
            var repoInterface = typeof(IRepository<>).MakeGenericType(entityType);
            var repoImpl      = typeof(InMemoryRepository<>).MakeGenericType(entityType);
            services.AddSingleton(repoInterface, repoImpl);
        }

        return services;
    }
}
