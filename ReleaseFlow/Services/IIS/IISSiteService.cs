using Microsoft.Web.Administration;

namespace ReleaseFlow.Services.IIS;

public class IISSiteService : IIISSiteService
{
    private readonly ILogger<IISSiteService> _logger;

    public IISSiteService(ILogger<IISSiteService> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<SiteInfo>> GetAllSitesAsync()
    {
        return await Task.Run(() =>
        {
            var sites = new List<SiteInfo>();
            
            try
            {
                using var serverManager = new ServerManager();
                
                foreach (var site in serverManager.Sites)
                {
                    sites.Add(MapToSiteInfo(site));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving IIS sites");
                throw;
            }

            return sites;
        });
    }

    public async Task<SiteInfo?> GetSiteByNameAsync(string siteName)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var serverManager = new ServerManager();
                var site = serverManager.Sites.FirstOrDefault(s => s.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase));
                
                return site != null ? MapToSiteInfo(site) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving site {SiteName}", siteName);
                throw;
            }
        });
    }

    public async Task<bool> StartSiteAsync(string siteName)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var serverManager = new ServerManager();
                var site = serverManager.Sites.FirstOrDefault(s => s.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase));
                
                if (site == null)
                {
                    _logger.LogWarning("Site {SiteName} not found", siteName);
                    return false;
                }

                if (site.State == ObjectState.Started || site.State == ObjectState.Starting)
                {
                    _logger.LogInformation("Site {SiteName} is already started or starting", siteName);
                    return true;
                }

                site.Start();
                _logger.LogInformation("Site {SiteName} started successfully", siteName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting site {SiteName}", siteName);
                return false;
            }
        });
    }

    public async Task<bool> StopSiteAsync(string siteName)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var serverManager = new ServerManager();
                var site = serverManager.Sites.FirstOrDefault(s => s.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase));
                
                if (site == null)
                {
                    _logger.LogWarning("Site {SiteName} not found", siteName);
                    return false;
                }

                if (site.State == ObjectState.Stopped || site.State == ObjectState.Stopping)
                {
                    _logger.LogInformation("Site {SiteName} is already stopped or stopping", siteName);
                    return true;
                }

                site.Stop();
                _logger.LogInformation("Site {SiteName} stopped successfully", siteName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping site {SiteName}", siteName);
                return false;
            }
        });
    }

    public async Task<bool> RestartSiteAsync(string siteName)
    {
        var stopped = await StopSiteAsync(siteName);
        if (!stopped) return false;

        // Wait a moment for the site to fully stop
        await Task.Delay(1000);

        return await StartSiteAsync(siteName);
    }

    public async Task<bool> CreateSiteAsync(string siteName, string physicalPath, string appPoolName, string binding)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var serverManager = new ServerManager();
                
                // Check if site already exists
                if (serverManager.Sites.Any(s => s.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning("Site {SiteName} already exists", siteName);
                    return false;
                }

                // Create the site
                var site = serverManager.Sites.Add(siteName, physicalPath, 80);
                site.ApplicationDefaults.ApplicationPoolName = appPoolName;

                // Clear default binding and add custom one
                site.Bindings.Clear();
                site.Bindings.Add(binding, "http");

                serverManager.CommitChanges();
                _logger.LogInformation("Site {SiteName} created successfully", siteName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating site {SiteName}", siteName);
                return false;
            }
        });
    }

    public async Task<bool> DeleteSiteAsync(string siteName)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var serverManager = new ServerManager();
                var site = serverManager.Sites.FirstOrDefault(s => s.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase));
                
                if (site == null)
                {
                    _logger.LogWarning("Site {SiteName} not found", siteName);
                    return false;
                }

                serverManager.Sites.Remove(site);
                serverManager.CommitChanges();
                _logger.LogInformation("Site {SiteName} deleted successfully", siteName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting site {SiteName}", siteName);
                return false;
            }
        });
    }

    public async Task<ObjectState> GetSiteStateAsync(string siteName)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var serverManager = new ServerManager();
                var site = serverManager.Sites.FirstOrDefault(s => s.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase));
                
                return site?.State ?? ObjectState.Unknown;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting state for site {SiteName}", siteName);
                return ObjectState.Unknown;
            }
        });
    }

    private SiteInfo MapToSiteInfo(Site site)
    {
        var siteInfo = new SiteInfo
        {
            Name = site.Name,
            Id = site.Id,
            State = site.State.ToString(),
            PhysicalPath = site.Applications["/"]?.VirtualDirectories["/"]?.PhysicalPath ?? string.Empty,
            AppPoolName = site.Applications["/"]?.ApplicationPoolName ?? string.Empty
        };

        // Map bindings
        foreach (var binding in site.Bindings)
        {
            siteInfo.Bindings.Add(new BindingInfo
            {
                Protocol = binding.Protocol,
                BindingInformation = binding.BindingInformation,
                Host = binding.Host,
                HostName = binding.Host
            });
        }

        // Map applications (including nested applications)
        foreach (var app in site.Applications)
        {
            // Skip the root application as it's already represented in the site info
            if (app.Path == "/")
                continue;

            var virtualDir = app.VirtualDirectories["/"];
            siteInfo.Applications.Add(new ApplicationInfo
            {
                Path = app.Path,
                PhysicalPath = virtualDir?.PhysicalPath ?? string.Empty,
                AppPoolName = app.ApplicationPoolName ?? string.Empty,
                EnabledProtocols = !string.IsNullOrEmpty(app.EnabledProtocols)
            });
        }

        return siteInfo;
    }
}
