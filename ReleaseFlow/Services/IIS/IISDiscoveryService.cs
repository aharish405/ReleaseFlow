using ReleaseFlow.Data.Repositories;
using ReleaseFlow.Models;

namespace ReleaseFlow.Services.IIS;

public class IISDiscoveryService : IIISDiscoveryService
{
    private readonly IIISSiteService _siteService;
    private readonly IApplicationRepository _applicationRepository;
    private readonly ILogger<IISDiscoveryService> _logger;

    public IISDiscoveryService(
        IIISSiteService siteService,
        IApplicationRepository applicationRepository,
        ILogger<IISDiscoveryService> logger)
    {
        _siteService = siteService;
        _applicationRepository = applicationRepository;
        _logger = logger;
    }

    public async Task<DiscoveryResult> DiscoverAndRegisterApplicationsAsync()
    {
        var result = new DiscoveryResult { Success = false };

        try
        {
            _logger.LogInformation("Starting IIS application discovery...");

            // Get all IIS sites
            var sites = await _siteService.GetAllSitesAsync();
            _logger.LogInformation("Found {SiteCount} IIS sites", sites.Count());

            foreach (var site in sites)
            {
                try
                {
                    // Get site details with applications
                    var siteDetails = await _siteService.GetSiteByNameAsync(site.Name);
                    if (siteDetails == null) continue;

                    result.ApplicationsDiscovered += siteDetails.Applications.Count;

                    // Register root application
                    await RegisterApplicationAsync(site.Name, "/", siteDetails, result);

                    // Register nested applications
                    foreach (var app in siteDetails.Applications)
                    {
                        await RegisterApplicationAsync(site.Name, app.Path, siteDetails, result);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error discovering applications for site {SiteName}", site.Name);
                    result.Errors.Add($"Site {site.Name}: {ex.Message}");
                }
            }

            result.Success = true;
            result.Message = $"Discovered {result.ApplicationsDiscovered} applications. Registered {result.ApplicationsRegistered} new, updated {result.ApplicationsUpdated} existing.";
            _logger.LogInformation(result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IIS discovery failed");
            result.Message = $"Discovery failed: {ex.Message}";
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    private async Task RegisterApplicationAsync(string siteName, string appPath, SiteInfo siteDetails, DiscoveryResult result)
    {
        try
        {
            // Find the application info from site details
            var appInfo = appPath == "/"
                ? new ApplicationInfo
                {
                    Path = "/",
                    PhysicalPath = siteDetails.PhysicalPath,
                    AppPoolName = siteDetails.AppPoolName
                }
                : siteDetails.Applications.FirstOrDefault(a => a.Path == appPath);

            if (appInfo == null) return;

            // Check if application already exists
            var allApps = await _applicationRepository.GetAllAsync();
            var existing = allApps.FirstOrDefault(a => a.IISSiteName == siteName && a.ApplicationPath == appPath);

            if (existing != null)
            {
                // Update existing application
                existing.PhysicalPath = appInfo.PhysicalPath;
                existing.AppPoolName = appInfo.AppPoolName;
                existing.IsActive = siteDetails.State == "Started";
                existing.LastDiscoveredAt = DateTime.UtcNow;
                existing.UpdatedAt = DateTime.UtcNow;
                await _applicationRepository.UpdateAsync(existing);

                result.ApplicationsUpdated++;
                _logger.LogInformation("Updated application: {SiteName}{AppPath}", siteName, appPath);
            }
            else
            {
                // Create new application
                var application = new Application
                {
                    Name = appPath == "/" ? siteName : $"{siteName}{appPath}",
                    IISSiteName = siteName,
                    ApplicationPath = appPath,
                    AppPoolName = appInfo.AppPoolName,
                    PhysicalPath = appInfo.PhysicalPath,
                    Environment = DetectEnvironment(siteName, appInfo.PhysicalPath),
                    IsActive = siteDetails.State == "Started",
                    IsDiscovered = true,
                    LastDiscoveredAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    // Default deployment settings
                    StopSiteBeforeDeployment = true,
                    StopAppPoolBeforeDeployment = true,
                    CreateBackup = true,
                    StartSiteAfterDeployment = true,
                    StartAppPoolAfterDeployment = true,
                    RunHealthCheck = false,
                    DeploymentDelaySeconds = 2
                };

                await _applicationRepository.CreateAsync(application);
                result.ApplicationsRegistered++;
                _logger.LogInformation("Registered new application: {SiteName}{AppPath}", siteName, appPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering application {SiteName}{AppPath}", siteName, appPath);
            result.Errors.Add($"{siteName}{appPath}: {ex.Message}");
        }
    }

    private string DetectEnvironment(string siteName, string physicalPath)
    {
        var lowerSiteName = siteName.ToLower();
        var lowerPath = physicalPath.ToLower();

        if (lowerSiteName.Contains("prod") || lowerPath.Contains("production"))
            return "Production";
        if (lowerSiteName.Contains("staging") || lowerSiteName.Contains("stg") || lowerPath.Contains("staging"))
            return "Staging";
        if (lowerSiteName.Contains("dev") || lowerSiteName.Contains("development") || lowerPath.Contains("dev"))
            return "Development";
        if (lowerSiteName.Contains("test") || lowerSiteName.Contains("qa") || lowerPath.Contains("test"))
            return "Testing";

        return "Production"; // Default
    }
}
