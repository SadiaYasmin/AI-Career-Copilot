namespace CareerCopilot.Application.Common.Interfaces;

/// <summary>
/// Persistence abstraction that keeps EF Core implementation details out of the
/// Application layer. Handlers scope every query to the authenticated user.
/// </summary>
public interface IApplicationDbContext
{
    IQueryable<T> Set<T>() where T : class;
    void Add<T>(T entity) where T : class;
    void AddRange<T>(IEnumerable<T> entities) where T : class;
    void Update<T>(T entity) where T : class;
    void Remove<T>(T entity) where T : class;
    void RemoveRange<T>(IEnumerable<T> entities) where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}