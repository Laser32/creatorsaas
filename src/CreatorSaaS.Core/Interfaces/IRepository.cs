using System.Linq.Expressions;

namespace CreatorSaaS.Core.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
    IQueryable<T> Query();
}

public interface IUnitOfWork
{
    IRepository<Entities.Tenant> Tenants { get; }
    IRepository<Entities.User> Users { get; }
    IRepository<Entities.Project> Projects { get; }
    IRepository<Entities.Channel> Channels { get; }
    IRepository<Entities.VideoJob> VideoJobs { get; }
    IRepository<Entities.VideoScene> VideoScenes { get; }
    IRepository<Entities.Subscription> Subscriptions { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
