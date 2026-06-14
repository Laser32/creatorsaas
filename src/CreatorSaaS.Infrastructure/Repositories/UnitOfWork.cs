using System.Linq.Expressions;
using CreatorSaaS.Core.Entities;
using CreatorSaaS.Core.Interfaces;
using CreatorSaaS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CreatorSaaS.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(e => e.Id == id, ct);

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
        => await _dbSet.ToListAsync(ct);

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.Where(predicate).ToListAsync(ct);

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(predicate, ct);

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
        => await _dbSet.AddAsync(entity, ct);

    public virtual async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        _dbSet.Update(entity);
        entity.UpdatedAt = DateTime.UtcNow;
        await Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
        await Task.CompletedTask;
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        => predicate == null
            ? await _dbSet.CountAsync(ct)
            : await _dbSet.CountAsync(predicate, ct);

    public virtual IQueryable<T> Query() => _dbSet.AsQueryable();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IRepository<Tenant>? _tenants;
    private IRepository<User>? _users;
    private IRepository<Project>? _projects;
    private IRepository<Channel>? _channels;
    private IRepository<VideoJob>? _videoJobs;
    private IRepository<VideoScene>? _videoScenes;
    private IRepository<Subscription>? _subscriptions;

    public UnitOfWork(AppDbContext context) => _context = context;

    public IRepository<Tenant> Tenants => _tenants ??= new Repository<Tenant>(_context);
    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<Project> Projects => _projects ??= new Repository<Project>(_context);
    public IRepository<Channel> Channels => _channels ??= new Repository<Channel>(_context);
    public IRepository<VideoJob> VideoJobs => _videoJobs ??= new Repository<VideoJob>(_context);
    public IRepository<VideoScene> VideoScenes => _videoScenes ??= new Repository<VideoScene>(_context);
    public IRepository<Subscription> Subscriptions => _subscriptions ??= new Repository<Subscription>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
