using Microsoft.Web.Administration;

namespace ReleaseFlow.Services.IIS;

public interface IIISSiteService
{
    Task<IEnumerable<SiteInfo>> GetAllSitesAsync();
    Task<SiteInfo?> GetSiteByNameAsync(string siteName);
    Task<bool> StartSiteAsync(string siteName);
    Task<bool> StopSiteAsync(string siteName);
    Task<bool> RestartSiteAsync(string siteName);
    Task<bool> CreateSiteAsync(string siteName, string physicalPath, string appPoolName, string binding);
    Task<bool> DeleteSiteAsync(string siteName);
    Task<ObjectState> GetSiteStateAsync(string siteName);
}

public class SiteInfo
{
    public string Name { get; set; } = string.Empty;
    public long Id { get; set; }
    public string State { get; set; } = string.Empty;
    public string PhysicalPath { get; set; } = string.Empty;
    public string AppPoolName { get; set; } = string.Empty;
    public List<BindingInfo> Bindings { get; set; } = new();
    public List<ApplicationInfo> Applications { get; set; } = new();
}

public class BindingInfo
{
    public string Protocol { get; set; } = string.Empty;
    public string BindingInformation { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
}

public class ApplicationInfo
{
    public string Path { get; set; } = string.Empty;
    public string PhysicalPath { get; set; } = string.Empty;
    public string AppPoolName { get; set; } = string.Empty;
    public bool EnabledProtocols { get; set; }
}
