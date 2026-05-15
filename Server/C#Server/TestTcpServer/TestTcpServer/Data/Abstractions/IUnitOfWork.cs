namespace GameServer.Data.Abstractions;

/// <summary>
/// 工作单元接口。
/// 用于跨多个 Repository 的事务操作。
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    IRepository<T> Repository<T>() where T : class, IEntity;
    ValueTask CommitAsync(CancellationToken ct = default);
    ValueTask RollbackAsync(CancellationToken ct = default);
}
