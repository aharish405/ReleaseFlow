using ReleaseFlow.Models;

namespace ReleaseFlow.Data.Repositories;

public interface IDeploymentStepRepository
{
    Task<int> CreateAsync(DeploymentStep step);
    Task UpdateAsync(DeploymentStep step);
    Task<IEnumerable<DeploymentStep>> GetByDeploymentIdAsync(int deploymentId);
}
