using GameServer.Data.Abstractions;

namespace GameServer.Data.InMemory;

/// <summary>
/// InMemory 工作单元（no-op 实现，Commit/Rollback 为空操作）。
/// 生产环境替换为 EF Core UoW 或事务包装器。
/// </summary>
public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    private readonly IServiceProvider _sp;

    public InMemoryUnitOfWork(IServiceProvider sp) => _sp = sp;

    public IRepository<T> Repository<T>() where T : class, IEntity
        => _sp.GetService(typeof(IRepository<T>)) as IRepository<T>
           ?? throw new InvalidOperationException($"未注册 IRepository<{typeof(T).Name}>");

    public ValueTask CommitAsync(CancellationToken ct = default) => ValueTask.CompletedTask;
    public ValueTask RollbackAsync(CancellationToken ct = default) => ValueTask.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
