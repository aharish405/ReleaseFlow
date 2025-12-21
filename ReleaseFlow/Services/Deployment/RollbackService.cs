using ReleaseFlow.Data.Repositories;
using ReleaseFlow.Models;
using ReleaseFlow.Services.IIS;

namespace ReleaseFlow.Services.Deployment;

public class RollbackService : IRollbackService
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IDeploymentRepository _deploymentRepository;
    private readonly IBackupService _backupService;
    private readonly IIISSiteService _siteService;
    private readonly IIISAppPoolService _appPoolService;
    private readonly ILogger<RollbackService> _logger;

    public RollbackService(
        IApplicationRepository applicationRepository,
        IDeploymentRepository deploymentRepository,
        IBackupService backupService,
        IIISSiteService siteService,
        IIISAppPoolService appPoolService,
        ILogger<RollbackService> logger)
    {
        _applicationRepository = applicationRepository;
        _deploymentRepository = deploymentRepository;
        _backupService = backupService;
        _siteService = siteService;
        _appPoolService = appPoolService;
        _logger = logger;
    }

    public async Task<bool> CanRollbackAsync(int deploymentId)
    {
        var deployment = await _deploymentRepository.GetByIdAsync(deploymentId);
        return deployment != null && deployment.CanRollback && !string.IsNullOrEmpty(deployment.BackupPath);
    }

    public async Task<RollbackResult> RollbackDeploymentAsync(int deploymentId, string username)
    {
        var result = new RollbackResult();

        try
        {
            // Get the deployment to rollback
            var deployment = await _deploymentRepository.GetByIdAsync(deploymentId);
            if (deployment == null)
            {
                result.Message = "Deployment not found";
                return result;
            }

            if (!deployment.CanRollback || string.IsNullOrEmpty(deployment.BackupPath))
            {
                result.Message = "Deployment cannot be rolled back (no backup available)";
                return result;
            }

            if (!File.Exists(deployment.BackupPath))
            {
                result.Message = "Backup file not found";
                return result;
            }

            // Load application directly from repository (navigation property might not be populated)
            var application = await _applicationRepository.GetByIdAsync(deployment.ApplicationId);
            if (application == null)
            {
                result.Message = "Application not found";
                return result;
            }

            result.Steps.Add("Rollback initiated");

            // Validate application has physical path
            if (string.IsNullOrEmpty(application.PhysicalPath))
            {
                result.Message = "Application physical path is not configured";
                return result;
            }

            // ALWAYS stop services during rollback to unlock files for restoration
            // We'll restore the original states afterward
            bool siteWasRunning = false;
            bool appPoolWasRunning = false;

            try
            {
                // Check current states
                var siteState = await _siteService.GetSiteStateAsync(application.IISSiteName);
                siteWasRunning = siteState == Microsoft.Web.Administration.ObjectState.Started;

                var poolState = await _appPoolService.GetAppPoolStateAsync(application.AppPoolName);
                appPoolWasRunning = poolState == Microsoft.Web.Administration.ObjectState.Started;

                // Stop IIS site if running
                if (siteWasRunning)
                {
                    var siteStopped = await _siteService.StopSiteAsync(application.IISSiteName);
                    if (!siteStopped)
                    {
                        throw new Exception($"Failed to stop IIS site: {application.IISSiteName}");
                    }
                    result.Steps.Add("IIS site stopped for file restoration");
                }

                // Stop app pool if running
                if (appPoolWasRunning)
                {
                    var appPoolStopped = await _appPoolService.StopAppPoolAsync(application.AppPoolName);
                    if (!appPoolStopped)
                    {
                        throw new Exception($"Failed to stop app pool: {application.AppPoolName}");
                    }
                    result.Steps.Add("App pool stopped for file restoration");
                }

                // Wait for processes to stop
                await Task.Delay(2000);

                // Restore backup
                var restored = await _backupService.RestoreBackupAsync(deployment.BackupPath, application.PhysicalPath);
                if (!restored)
                {
                    throw new Exception("Failed to restore backup");
                }
                result.Steps.Add("Backup restored");

                // Restore original service states
                if (appPoolWasRunning)
                {
                    var appPoolStarted = await _appPoolService.StartAppPoolAsync(application.AppPoolName);
                    if (!appPoolStarted)
                    {
                        throw new Exception($"Failed to start app pool: {application.AppPoolName}");
                    }
                    result.Steps.Add("App pool restarted");
                }

                if (siteWasRunning)
                {
                    var siteStarted = await _siteService.StartSiteAsync(application.IISSiteName);
                    if (!siteStarted)
                    {
                        throw new Exception($"Failed to start IIS site: {application.IISSiteName}");
                    }
                    result.Steps.Add("IIS site restarted");
                }
            }
            catch (Exception)
            {
                // If rollback fails, try to restore services to their original states
                if (appPoolWasRunning)
                {
                    await _appPoolService.StartAppPoolAsync(application.AppPoolName);
                }
                if (siteWasRunning)
                {
                    await _siteService.StartSiteAsync(application.IISSiteName);
                }
                throw;
            }

            // Update deployment status
            deployment.Status = DeploymentStatus.RolledBack;
            await _deploymentRepository.UpdateAsync(deployment);

            // Create rollback deployment record
            var rollbackDeployment = new Models.Deployment
            {
                ApplicationId = application.Id,
                DeployedByUsername = username,
                Version = $"Rollback from {deployment.Version}",
                ZipFileName = Path.GetFileName(deployment.BackupPath),
                ZipFileSize = new FileInfo(deployment.BackupPath).Length,
                Status = DeploymentStatus.Succeeded,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            await _deploymentRepository.CreateAsync(rollbackDeployment);

            result.Success = true;
            result.Message = "Rollback completed successfully";
            _logger.LogInformation("Rollback completed for deployment {DeploymentId}", deploymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rollback failed for deployment {DeploymentId}", deploymentId);
            result.Success = false;
            result.Message = $"Rollback failed: {ex.Message}";
        }

        return result;
    }
}
