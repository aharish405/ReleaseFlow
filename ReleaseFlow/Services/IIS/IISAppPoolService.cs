using Microsoft.Web.Administration;

namespace ReleaseFlow.Services.IIS;

public class IISAppPoolService : IIISAppPoolService
{
    private readonly ILogger<IISAppPoolService> _logger;

    public IISAppPoolService(ILogger<IISAppPoolService> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<AppPoolInfo>> GetAllAppPoolsAsync()
    {
        return await Task.Run(() =>
        {
            var appPools = new List<AppPoolInfo>();
            
            try
            {
                using var serverManager = new ServerManager();
                
                foreach (var appPool in serverManager.ApplicationPools)
                {
                    appPools.Add(MapToAppPoolInfo(appPool));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application pools");
                throw;
            }

            return appPools;
        });
    }

    public async Task<AppPoolInfo?> GetAppPoolByNameAsync(string appPoolName)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var serverManager = new ServerManager();
                var appPool = serverManager.ApplicationPools.FirstOrDefault(ap => 
                    ap.Name.Equals(appPoolName, StringComparison.OrdinalIgnoreCase));
                
                return appPool != null ? MapToAppPoolInfo(appPool) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving app pool {AppPoolName}", appPoolName);
                throw;
            }
        });
    }

    public async Task<bool> CreateAppPoolAsync(string appPoolName, string runtimeVersion, string identityType)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var serverManager = new ServerManager();
                
                // Check if app pool already exists
                if (serverManager.ApplicationPools.Any(ap => 
                    ap.Name.Equals(appPoolName, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning("App pool {AppPoolName} already exists", appPoolName);
                    return false;
                }

                // Create the app pool
                var appPool = serverManager.ApplicationPools.Add(appPoolName);
                appPool.ManagedRuntimeVersion = runtimeVersion;
                appPool.ManagedPipelineMode = ManagedPipelineMode.Integrated;
                
                // Set identity
                if (identityType.Equals("ApplicationPoolIdentity", StringComparison.OrdinalIgnoreCase))
                {
                    appPool.ProcessModel.IdentityType = ProcessModelIdentityType.ApplicationPoolIdentity;
                }
                else if (identityType.Equals("NetworkService", StringComparison.OrdinalIgnoreCase))
                {
                    appPool.ProcessModel.IdentityType = ProcessModelIdentityType.NetworkService;
                }
                else if (identityType.Equals("LocalSystem", StringComparison.OrdinalIgnoreCase))
                {
                    appPool.ProcessModel.IdentityType = ProcessModelIdentityType.LocalSystem;
                }

                serverManager.CommitChanges();
                _logger.LogInformation("App pool {AppPoolName} created successfully", appPoolName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating app pool {AppPoolName}", appPoolName);
                return false;
            }
        });
    }

    public async Task<bool> RecycleAppPoolAsync(string appPoolName)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var serverManager = new ServerManager();
                var appPool = serverManager.ApplicationPools.FirstOrDefault(ap => 
                    ap.Name.Equals(appPoolName, StringComparison.OrdinalIgnoreCase));
                
                if (appPool == null)
                {
                    _logger.LogWarning("App pool {AppPoolName} not found", appPoolName);
                    return false;
                }

                appPool.Recycle();
                _logger.LogInformation("App pool {AppPoolName} recycled successfully", appPoolName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recycling app pool {AppPoolName}", appPoolName);
                return false;
            }
        });
    }

    public async Task<bool> StartAppPoolAsync(string appPoolName)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var serverManager = new ServerManager();
                var appPool = serverManager.ApplicationPools.FirstOrDefault(ap => 
                    ap.Name.Equals(appPoolName, StringComparison.OrdinalIgnoreCase));
                
                if (appPool == null)
                {
                    _logger.LogWarning("App pool {AppPoolName} not found", appPoolName);
                    return false;
                }

                if (appPool.State == ObjectState.Started || appPool.State == ObjectState.Starting)
                {
                    _logger.LogInformation("App pool {AppPoolName} is already started or starting", appPoolName);
                    return true;
                }

                appPool.Start();
                _logger.LogInformation("App pool {AppPoolName} started successfully", appPoolName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting app pool {AppPoolName}", appPoolName);
                return false;
            }
        });
    }

    public async Task<bool> StopAppPoolAsync(string appPoolName)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var serverManager = new ServerManager();
                var appPool = serverManager.ApplicationPools.FirstOrDefault(ap => 
                    ap.Name.Equals(appPoolName, StringComparison.OrdinalIgnoreCase));
                
                if (appPool == null)
                {
                    _logger.LogWarning("App pool {AppPoolName} not found", appPoolName);
                    return false;
                }

                if (appPool.State == ObjectState.Stopped || appPool.State == ObjectState.Stopping)
                {
                    _logger.LogInformation("App pool {AppPoolName} is already stopped or stopping", appPoolName);
                    return true;
                }

                appPool.Stop();
                _logger.LogInformation("App pool {AppPoolName} stopped successfully", appPoolName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping app pool {AppPoolName}", appPoolName);
                return false;
            }
        });
    }

    public async Task<ObjectState> GetAppPoolStateAsync(string appPoolName)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var serverManager = new ServerManager();
                var appPool = serverManager.ApplicationPools.FirstOrDefault(ap => 
                    ap.Name.Equals(appPoolName, StringComparison.OrdinalIgnoreCase));
                
                return appPool?.State ?? ObjectState.Unknown;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting state for app pool {AppPoolName}", appPoolName);
                return ObjectState.Unknown;
            }
        });
    }

    private AppPoolInfo MapToAppPoolInfo(ApplicationPool appPool)
    {
        return new AppPoolInfo
        {
            Name = appPool.Name,
            State = appPool.State.ToString(),
            RuntimeVersion = appPool.ManagedRuntimeVersion,
            ManagedRuntimeVersion = appPool.ManagedRuntimeVersion,
            PipelineMode = appPool.ManagedPipelineMode.ToString(),
            ManagedPipelineMode = appPool.ManagedPipelineMode.ToString(),
            IdentityType = appPool.ProcessModel.IdentityType.ToString()
        };
    }
}
