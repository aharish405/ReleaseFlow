using Microsoft.EntityFrameworkCore;
using ReleaseFlow.Data;
using ReleaseFlow.Models;
using ReleaseFlow.Services.IIS;

namespace ReleaseFlow.Services.Deployment;

public class RollbackService : IRollbackService
{
    private readonly ApplicationDbContext _context;
    private readonly IBackupService _backupService;
    private readonly IIISSiteService _siteService;
    private readonly IIISAppPoolService _appPoolService;
    private readonly ILogger<RollbackService> _logger;

    public RollbackService(
        ApplicationDbContext context,
        IBackupService backupService,
        IIISSiteService siteService,
        IIISAppPoolService appPoolService,
        ILogger<RollbackService> logger)
    {
        _context = context;
        _backupService = backupService;
        _siteService = siteService;
        _appPoolService = appPoolService;
        _logger = logger;
    }

    public async Task<bool> CanRollbackAsync(int deploymentId)
    {
        var deployment = await _context.Deployments.FindAsync(deploymentId);
        return deployment != null && deployment.CanRollback && !string.IsNullOrEmpty(deployment.BackupPath);
    }

    public async Task<RollbackResult> RollbackDeploymentAsync(int deploymentId, string username)
    {
        var result = new RollbackResult();

        try
        {
            // Get the deployment to rollback
            var deployment = await _context.Deployments
                .Include(d => d.Application)
                .FirstOrDefaultAsync(d => d.Id == deploymentId);

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

            var application = deployment.Application;
            result.Steps.Add("Rollback initiated");

            // Stop IIS site
            var siteStopped = await _siteService.StopSiteAsync(application.IISSiteName);
            if (!siteStopped)
            {
                throw new Exception($"Failed to stop IIS site: {application.IISSiteName}");
            }
            result.Steps.Add("IIS site stopped");

            // Stop app pool
            var appPoolStopped = await _appPoolService.StopAppPoolAsync(application.AppPoolName);
            if (!appPoolStopped)
            {
                throw new Exception($"Failed to stop app pool: {application.AppPoolName}");
            }
            result.Steps.Add("App pool stopped");

            // Wait for processes to stop
            await Task.Delay(2000);

            // Restore backup
            var restored = await _backupService.RestoreBackupAsync(deployment.BackupPath, application.PhysicalPath);
            if (!restored)
            {
                throw new Exception("Failed to restore backup");
            }
            result.Steps.Add("Backup restored");

            // Start app pool
            var appPoolStarted = await _appPoolService.StartAppPoolAsync(application.AppPoolName);
            if (!appPoolStarted)
            {
                throw new Exception($"Failed to start app pool: {application.AppPoolName}");
            }
            result.Steps.Add("App pool started");

            // Start IIS site
            var siteStarted = await _siteService.StartSiteAsync(application.IISSiteName);
            if (!siteStarted)
            {
                throw new Exception($"Failed to start IIS site: {application.IISSiteName}");
            }
            result.Steps.Add("IIS site started");

            // Update deployment status
            deployment.Status = DeploymentStatus.RolledBack;
            await _context.SaveChangesAsync();

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

            await _context.Deployments.AddAsync(rollbackDeployment);
            await _context.SaveChangesAsync();

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
