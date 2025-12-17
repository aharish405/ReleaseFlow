using ReleaseFlow.Models;

namespace ReleaseFlow.Services.Deployment;

public interface IDeploymentService
{
    Task<DeploymentResult> DeployAsync(int applicationId, string zipFilePath, string version, string username);
    Task<IEnumerable<Models.Deployment>> GetDeploymentHistoryAsync(int? applicationId = null);
    Task<Models.Deployment?> GetDeploymentByIdAsync(int deploymentId);
}

public class DeploymentResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? DeploymentId { get; set; }
    public List<string> Steps { get; set; } = new();
    public string? ErrorDetails { get; set; }
}
