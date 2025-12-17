using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using ReleaseFlow.Data;
using ReleaseFlow.Models;
using ReleaseFlow.Services.IIS;

namespace ReleaseFlow.Services.Deployment;

public class DeploymentService : IDeploymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IIISSiteService _siteService;
    private readonly IIISAppPoolService _appPoolService;
    private readonly IHealthCheckService _healthCheckService;
    private readonly IBackupService _backupService;
    private readonly ILogger<DeploymentService> _logger;

    public DeploymentService(
        ApplicationDbContext context,
        IIISSiteService siteService,
        IIISAppPoolService appPoolService,
        IHealthCheckService healthCheckService,
        IBackupService backupService,
        ILogger<DeploymentService> logger)
    {
        _context = context;
        _siteService = siteService;
        _appPoolService = appPoolService;
        _healthCheckService = healthCheckService;
        _backupService = backupService;
        _logger = logger;
    }

    public async Task<DeploymentResult> DeployAsync(int applicationId, string zipFilePath, string version, string username)
    {
        var result = new DeploymentResult
        {
            Success = false,
            Steps = new List<string>()
        };

        Models.Deployment? deployment = null;
        string? tempExtractPath = null;
        bool siteWasStopped = false;
        bool appPoolWasStopped = false;

        try
        {
            // Step 1: Validate application exists
            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.IsActive);

            if (application == null)
            {
                result.Message = "Application not found or inactive";
                return result;
            }

            result.Steps.Add("Application validated");

            // Step 2: Create deployment record
            deployment = new Models.Deployment
            {
                ApplicationId = applicationId,
                DeployedByUsername = username,
                Version = version,
                ZipFileName = Path.GetFileName(zipFilePath),
                ZipFileSize = new FileInfo(zipFilePath).Length,
                Status = DeploymentStatus.InProgress,
                StartedAt = DateTime.UtcNow
            };

            await _context.Deployments.AddAsync(deployment);
            await _context.SaveChangesAsync();
            result.DeploymentId = deployment.Id;

            await AddDeploymentStepAsync(deployment.Id, 1, "Deployment initialized", StepStatus.Succeeded);
            result.Steps.Add("Deployment record created");

            // Step 3: Validate ZIP file
            if (!File.Exists(zipFilePath))
            {
                throw new FileNotFoundException("ZIP file not found", zipFilePath);
            }

            await AddDeploymentStepAsync(deployment.Id, 2, "ZIP file validated", StepStatus.Succeeded);
            result.Steps.Add("ZIP file validated");

            // Step 4: Extract to temp directory
            tempExtractPath = Path.Combine(Path.GetTempPath(), $"ReleaseFlow_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempExtractPath);
            
            ZipFile.ExtractToDirectory(zipFilePath, tempExtractPath);
            await AddDeploymentStepAsync(deployment.Id, 3, "ZIP extracted to temp directory", StepStatus.Succeeded);
            result.Steps.Add("ZIP extracted");

            // Log the full application target
            var fullAppPath = $"{application.IISSiteName}{application.ApplicationPath}";
            _logger.LogInformation("Deploying to: {FullPath}", fullAppPath);
            await AddDeploymentStepAsync(deployment.Id, 3, $"Target: {fullAppPath}", StepStatus.Succeeded);

            // Step 5: Stop IIS site (if configured)
            if (application.StopSiteBeforeDeployment)
            {
                var siteStopped = await _siteService.StopSiteAsync(application.IISSiteName);
                if (!siteStopped)
                {
                    throw new Exception($"Failed to stop IIS site: {application.IISSiteName}");
                }
                siteWasStopped = true;
                await AddDeploymentStepAsync(deployment.Id, 4, $"IIS site '{application.IISSiteName}' stopped", StepStatus.Succeeded);
                result.Steps.Add("IIS site stopped");
            }
            else
            {
                await AddDeploymentStepAsync(deployment.Id, 4, "Skipped stopping IIS site (not configured)", StepStatus.Succeeded);
                result.Steps.Add("IIS site stop skipped");
            }

            // Step 6: Stop app pool (if configured)
            if (application.StopAppPoolBeforeDeployment)
            {
                var appPoolStopped = await _appPoolService.StopAppPoolAsync(application.AppPoolName);
                if (!appPoolStopped)
                {
                    throw new Exception($"Failed to stop app pool: {application.AppPoolName}");
                }
                appPoolWasStopped = true;
                await AddDeploymentStepAsync(deployment.Id, 5, $"App pool '{application.AppPoolName}' stopped", StepStatus.Succeeded);
                result.Steps.Add("App pool stopped");
            }
            else
            {
                await AddDeploymentStepAsync(deployment.Id, 5, "Skipped stopping app pool (not configured)", StepStatus.Succeeded);
                result.Steps.Add("App pool stop skipped");
            }

            // Wait for processes to fully stop (configurable delay)
            if (application.DeploymentDelaySeconds > 0)
            {
                await Task.Delay(application.DeploymentDelaySeconds * 1000);
            }

            // Step 7: Create backup (if configured)
            if (application.CreateBackup)
            {
                var backupPath = await _backupService.CreateBackupAsync(application.PhysicalPath, application.Name, version);
                
                if (!string.IsNullOrEmpty(backupPath))
                {
                    deployment.BackupPath = backupPath;
                    deployment.CanRollback = true;
                    await _context.SaveChangesAsync();
                    await AddDeploymentStepAsync(deployment.Id, 6, $"Backup created at '{backupPath}'", StepStatus.Succeeded);
                    result.Steps.Add("Backup created");
                }
                else
                {
                    // First deployment - no backup needed
                    deployment.CanRollback = false;
                    await _context.SaveChangesAsync();
                    await AddDeploymentStepAsync(deployment.Id, 6, "First deployment - backup skipped", StepStatus.Succeeded);
                    result.Steps.Add("Backup skipped (first deployment)");
                }
            }
            else
            {
                deployment.CanRollback = false;
                await _context.SaveChangesAsync();
                await AddDeploymentStepAsync(deployment.Id, 6, "Backup skipped (not configured)", StepStatus.Succeeded);
                result.Steps.Add("Backup skipped");
            }

            // Step 8: Replace files (atomic swap)
            await ReplaceFilesAsync(tempExtractPath, application.PhysicalPath);
            await AddDeploymentStepAsync(deployment.Id, 7, "Files replaced successfully", StepStatus.Succeeded);
            result.Steps.Add("Files replaced");

            // Step 9: Start app pool (if configured)
            if (application.StartAppPoolAfterDeployment)
            {
                var appPoolStarted = await _appPoolService.StartAppPoolAsync(application.AppPoolName);
                if (!appPoolStarted)
                {
                    throw new Exception($"Failed to start app pool: {application.AppPoolName}");
                }
                await AddDeploymentStepAsync(deployment.Id, 8, $"App pool '{application.AppPoolName}' started", StepStatus.Succeeded);
                result.Steps.Add("App pool started");
            }
            else
            {
                await AddDeploymentStepAsync(deployment.Id, 8, "Skipped starting app pool (not configured)", StepStatus.Succeeded);
                result.Steps.Add("App pool start skipped");
            }

            // Step 10: Start IIS site (if configured)
            if (application.StartSiteAfterDeployment)
            {
                var siteStarted = await _siteService.StartSiteAsync(application.IISSiteName);
                if (!siteStarted)
                {
                    throw new Exception($"Failed to start IIS site: {application.IISSiteName}");
                }
                await AddDeploymentStepAsync(deployment.Id, 9, $"IIS site '{application.IISSiteName}' started", StepStatus.Succeeded);
                result.Steps.Add("IIS site started");
                
                // Wait for site to warm up
                await Task.Delay(3000);
            }
            else
            {
                await AddDeploymentStepAsync(deployment.Id, 9, "Skipped starting IIS site (not configured)", StepStatus.Succeeded);
                result.Steps.Add("IIS site start skipped");
            }

            // Step 11: Health check (if configured)
            if (application.RunHealthCheck && !string.IsNullOrEmpty(application.HealthCheckUrl))
            {
                var healthCheck = await _healthCheckService.CheckHealthAsync(application.HealthCheckUrl);
                if (!healthCheck.IsHealthy)
                {
                    _logger.LogWarning("Health check failed but deployment will continue: {Message}", healthCheck.Message);
                    await AddDeploymentStepAsync(deployment.Id, 10, $"Health check warning: {healthCheck.Message}", StepStatus.Succeeded);
                }
                else
                {
                    await AddDeploymentStepAsync(deployment.Id, 10, "Health check passed", StepStatus.Succeeded);
                }
                result.Steps.Add("Health check completed");
            }
            else if (!application.RunHealthCheck)
            {
                await AddDeploymentStepAsync(deployment.Id, 10, "Health check skipped (not configured)", StepStatus.Succeeded);
                result.Steps.Add("Health check skipped");
            }

            // Step 12: Mark deployment as succeeded
            deployment.Status = DeploymentStatus.Succeeded;
            deployment.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            result.Success = true;
            result.Message = "Deployment completed successfully";
            _logger.LogInformation("Deployment {DeploymentId} completed successfully", deployment.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deployment failed for application {ApplicationId}", applicationId);
            result.Success = false;
            result.Message = "Deployment failed";
            result.ErrorDetails = ex.Message;

            if (deployment != null)
            {
                deployment.Status = DeploymentStatus.Failed;
                deployment.ErrorMessage = ex.Message;
                deployment.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await AddDeploymentStepAsync(deployment.Id, 99, $"Deployment failed: {ex.Message}", StepStatus.Failed, ex.ToString());
            }

            // Attempt to restart services if they were stopped
            if (deployment != null)
            {
                var application = await _context.Applications.FindAsync(applicationId);
                if (application != null)
                {
                    if (appPoolWasStopped)
                    {
                        await _appPoolService.StartAppPoolAsync(application.AppPoolName);
                    }
                    if (siteWasStopped)
                    {
                        await _siteService.StartSiteAsync(application.IISSiteName);
                    }
                }
            }
        }
        finally
        {
            // Cleanup temp directory
            if (tempExtractPath != null && Directory.Exists(tempExtractPath))
            {
                try
                {
                    Directory.Delete(tempExtractPath, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup temp directory: {TempPath}", tempExtractPath);
                }
            }
        }

        return result;
    }

    public async Task<IEnumerable<Models.Deployment>> GetDeploymentHistoryAsync(int? applicationId = null)
    {
        var query = _context.Deployments
            .Include(d => d.Application)
            .AsQueryable();

        if (applicationId.HasValue)
        {
            query = query.Where(d => d.ApplicationId == applicationId.Value);
        }

        return await query
            .OrderByDescending(d => d.StartedAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task<Models.Deployment?> GetDeploymentByIdAsync(int deploymentId)
    {
        return await _context.Deployments
            .Include(d => d.Application)
            .Include(d => d.Steps)
            .FirstOrDefaultAsync(d => d.Id == deploymentId);
    }

    private async Task ReplaceFilesAsync(string sourcePath, string destinationPath)
    {
        // Ensure destination exists
        if (!Directory.Exists(destinationPath))
        {
            Directory.CreateDirectory(destinationPath);
        }

        // Smart folder detection: Check if ZIP has nested structure
        // e.g., App.zip/App/App/<ActualFiles>
        var actualSourcePath = FindActualContentFolder(sourcePath);
        
        _logger.LogInformation("Deploying from: {SourcePath}", actualSourcePath);

        // Copy all files and directories
        await CopyDirectoryAsync(actualSourcePath, destinationPath);
    }

    private string FindActualContentFolder(string extractedPath)
    {
        // Check if the extracted folder contains actual web files
        // Common web files: web.config, appsettings.json, index.html, bin/, wwwroot/, etc.
        var commonWebFiles = new[] { "web.config", "appsettings.json", "index.html", "default.aspx", "global.asax" };
        var commonWebFolders = new[] { "bin", "wwwroot", "content", "scripts", "app_data" };

        // Check current folder
        if (HasWebContent(extractedPath, commonWebFiles, commonWebFolders))
        {
            return extractedPath;
        }

        // Check one level deep (e.g., App.zip/App/)
        var subDirs = Directory.GetDirectories(extractedPath);
        if (subDirs.Length == 1)
        {
            var firstSubDir = subDirs[0];
            if (HasWebContent(firstSubDir, commonWebFiles, commonWebFolders))
            {
                _logger.LogInformation("Detected nested structure, using: {Path}", firstSubDir);
                return firstSubDir;
            }

            // Check two levels deep (e.g., App.zip/App/App/)
            var secondLevelDirs = Directory.GetDirectories(firstSubDir);
            if (secondLevelDirs.Length == 1)
            {
                var secondSubDir = secondLevelDirs[0];
                if (HasWebContent(secondSubDir, commonWebFiles, commonWebFolders))
                {
                    _logger.LogInformation("Detected double-nested structure, using: {Path}", secondSubDir);
                    return secondSubDir;
                }
            }
        }

        // If no web content detected, use the original path
        _logger.LogWarning("No web content detected, using root extraction path");
        return extractedPath;
    }

    private bool HasWebContent(string path, string[] webFiles, string[] webFolders)
    {
        // Check for common web files
        foreach (var file in webFiles)
        {
            if (File.Exists(Path.Combine(path, file)))
            {
                return true;
            }
        }

        // Check for common web folders
        foreach (var folder in webFolders)
        {
            if (Directory.Exists(Path.Combine(path, folder)))
            {
                return true;
            }
        }

        return false;
    }

    private async Task CopyDirectoryAsync(string sourceDir, string destDir)
    {
        await Task.Run(() =>
        {
            // Get the subdirectories for the specified directory
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist: {sourceDir}");
            }

            var dirs = dir.GetDirectories();

            // Create the destination directory if it doesn't exist
            Directory.CreateDirectory(destDir);

            // Get the files in the directory and copy them to the new location
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var tempPath = Path.Combine(destDir, file.Name);
                file.CopyTo(tempPath, true);
            }

            // Copy subdirectories and their contents
            foreach (var subdir in dirs)
            {
                var tempPath = Path.Combine(destDir, subdir.Name);
                CopyDirectoryAsync(subdir.FullName, tempPath).Wait();
            }
        });
    }

    private async Task AddDeploymentStepAsync(int deploymentId, int stepNumber, string stepName, StepStatus status, string? errorDetails = null)
    {
        var step = new DeploymentStep
        {
            DeploymentId = deploymentId,
            StepNumber = stepNumber,
            StepName = stepName,
            Status = status,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            ErrorDetails = errorDetails
        };

        await _context.DeploymentSteps.AddAsync(step);
        await _context.SaveChangesAsync();
    }
}
