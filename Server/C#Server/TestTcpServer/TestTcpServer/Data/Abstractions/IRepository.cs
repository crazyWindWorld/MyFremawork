using System.Linq.Expressions;

namespace GameServer.Data.Abstractions;

/// <summary>
/// 泛型仓储抽象接口（Repository Pattern）。
/// 实现：InMemoryRepository（开发/测试）、MySqlRepository（生产）等。
/// </summary>
public interface IRepository<T> where T : class, IEntity
{
    // ── 查询 ─────────────────────────────────────────────────────
    ValueTask<T?> GetByIdAsync(long id, CancellationToken ct = default);

    ValueTask<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);

    ValueTask<T?> FindByPredicateAsync(
        Func<T, bool> predicate,
        CancellationToken ct = default);

    ValueTask<IReadOnlyList<T>> FindManyAsync(
        Func<T, bool> predicate,
        CancellationToken ct = default);

    ValueTask<int> CountAsync(CancellationToken ct = default);

    // ── 写入 ─────────────────────────────────────────────────────
    /// <summary>新增（id == 0 时自动分配 id）</summary>
    ValueTask AddAsync(T entity, CancellationToken ct = default);

    /// <summary>更新（实体必须已存在）</summary>
    ValueTask UpdateAsync(T entity, CancellationToken ct = default);

    /// <summary>新增或更新（按 id 判断）</summary>
    ValueTask UpsertAsync(T entity, CancellationToken ct = default);

    /// <summary>按 id 删除</summary>
    ValueTask<bool> DeleteAsync(long id, CancellationToken ct = default);

    // ── 批量 ─────────────────────────────────────────────────────
    ValueTask AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
}
