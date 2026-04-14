using System.Linq.Expressions;

namespace StreamlinedOrderProcessing.Repositories
{
    // IGenericRepository.cs
    public interface IGenericRepository<T> where T : class
    {
        // Basic CRUD
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);

        // Advanced Operations
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        // For "Streamlined" fetching: include related data (e.g., Order with OrderItems)
        Task<T?> GetWithIncludesAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        // For React Pagination
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
    }
}
