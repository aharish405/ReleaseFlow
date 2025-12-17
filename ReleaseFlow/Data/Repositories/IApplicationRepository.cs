using ReleaseFlow.Models;

namespace ReleaseFlow.Data.Repositories;

public interface IApplicationRepository
{
    Task<Application?> GetByIdAsync(int id);
    Task<IEnumerable<Application>> GetAllAsync();
    Task<IEnumerable<Application>> GetActiveAsync();
    Task<Application?> GetByNameAsync(string name);
    Task<int> CreateAsync(Application application);
    Task UpdateAsync(Application application);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
