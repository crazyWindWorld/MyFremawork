using System.Collections.Concurrent;
using GameServer.Data.Abstractions;

namespace GameServer.Data.InMemory;

/// <summary>
/// 内存仓储实现，用于开发/测试环境。
/// 线程安全（ConcurrentDictionary + Interlocked ID 自增）。
/// 生产环境替换为 MySqlRepository / MongoRepository 即可。
/// </summary>
public sealed class InMemoryRepository<T> : IRepository<T>
    where T : class, IEntity
{
    private readonly ConcurrentDictionary<long, T> _store = new();
    private long _nextId = 0;

    public ValueTask<T?> GetByIdAsync(long id, CancellationToken ct = default)
        => ValueTask.FromResult(_store.TryGetValue(id, out var v) ? v : null);

    public ValueTask<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => ValueTask.FromResult<IReadOnlyList<T>>(_store.Values.ToList());

    public ValueTask<T?> FindByPredicateAsync(
        Func<T, bool> predicate, CancellationToken ct = default)
        => ValueTask.FromResult(_store.Values.FirstOrDefault(predicate));

    public ValueTask<IReadOnlyList<T>> FindManyAsync(
        Func<T, bool> predicate, CancellationToken ct = default)
        => ValueTask.FromResult<IReadOnlyList<T>>(
            _store.Values.Where(predicate).ToList());

    public ValueTask<int> CountAsync(CancellationToken ct = default)
        => ValueTask.FromResult(_store.Count);

    public ValueTask AddAsync(T entity, CancellationToken ct = default)
    {
        if (entity.Id == 0)
            entity.Id = Interlocked.Increment(ref _nextId);
        _store[entity.Id] = entity;
        return ValueTask.CompletedTask;
    }

    public ValueTask UpdateAsync(T entity, CancellationToken ct = default)
    {
        if (!_store.ContainsKey(entity.Id))
            throw new KeyNotFoundException($"Entity id={entity.Id} not found.");
        _store[entity.Id] = entity;
        return ValueTask.CompletedTask;
    }

    public ValueTask UpsertAsync(T entity, CancellationToken ct = default)
    {
        if (entity.Id == 0)
            entity.Id = Interlocked.Increment(ref _nextId);
        _store[entity.Id] = entity;
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> DeleteAsync(long id, CancellationToken ct = default)
        => ValueTask.FromResult(_store.TryRemove(id, out _));

    public ValueTask AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        foreach (var e in entities)
        {
            if (e.Id == 0) e.Id = Interlocked.Increment(ref _nextId);
            _store[e.Id] = e;
        }
        return ValueTask.CompletedTask;
    }
}
