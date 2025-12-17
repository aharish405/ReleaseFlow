using ReleaseFlow.Models;
using ReleaseFlow.Services.IIS;

namespace ReleaseFlow.Services.Deployment;

public interface IRollbackService
{
    Task<RollbackResult> RollbackDeploymentAsync(int deploymentId, string username);
    Task<bool> CanRollbackAsync(int deploymentId);
}

public class RollbackResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Steps { get; set; } = new();
}
