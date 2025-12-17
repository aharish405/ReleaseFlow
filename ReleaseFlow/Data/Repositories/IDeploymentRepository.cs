using ReleaseFlow.Models;

namespace ReleaseFlow.Data.Repositories;

public interface IDeploymentRepository
{
    Task<Deployment?> GetByIdAsync(int id);
    Task<IEnumerable<Deployment>> GetByApplicationIdAsync(int applicationId);
    Task<IEnumerable<Deployment>> GetRecentAsync(int count = 10);
    Task<int> CreateAsync(Deployment deployment);
    Task UpdateAsync(Deployment deployment);
    Task<Deployment?> GetLatestSuccessfulAsync(int applicationId);
}
