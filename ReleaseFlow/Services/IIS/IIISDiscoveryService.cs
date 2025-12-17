namespace ReleaseFlow.Services.IIS;

public interface IIISDiscoveryService
{
    Task<DiscoveryResult> DiscoverAndRegisterApplicationsAsync();
}

public class DiscoveryResult
{
    public bool Success { get; set; }
    public int ApplicationsDiscovered { get; set; }
    public int ApplicationsRegistered { get; set; }
    public int ApplicationsUpdated { get; set; }
    public List<string> Errors { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
