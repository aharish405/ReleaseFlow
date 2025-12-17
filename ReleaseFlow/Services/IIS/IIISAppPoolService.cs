using Microsoft.Web.Administration;

namespace ReleaseFlow.Services.IIS;

public interface IIISAppPoolService
{
    Task<IEnumerable<AppPoolInfo>> GetAllAppPoolsAsync();
    Task<AppPoolInfo?> GetAppPoolByNameAsync(string appPoolName);
    Task<bool> CreateAppPoolAsync(string appPoolName, string runtimeVersion, string identityType);
    Task<bool> RecycleAppPoolAsync(string appPoolName);
    Task<bool> StartAppPoolAsync(string appPoolName);
    Task<bool> StopAppPoolAsync(string appPoolName);
    Task<ObjectState> GetAppPoolStateAsync(string appPoolName);
}

public class AppPoolInfo
{
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string RuntimeVersion { get; set; } = string.Empty;
    public string ManagedRuntimeVersion { get; set; } = string.Empty;
    public string PipelineMode { get; set; } = string.Empty;
    public string ManagedPipelineMode { get; set; } = string.Empty;
    public string IdentityType { get; set; } = string.Empty;
}

