using System.IO.Compression;

namespace ReleaseFlow.Services.Deployment;

public class BackupService : IBackupService
{
    private readonly ILogger<BackupService> _logger;
    private readonly string _backupBasePath;

    public BackupService(ILogger<BackupService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _backupBasePath = configuration["BackupBasePath"] ?? @"C:\ReleaseFlow\Backups";
        
        // Ensure backup directory exists
        if (!Directory.Exists(_backupBasePath))
        {
            Directory.CreateDirectory(_backupBasePath);
        }
    }

    public async Task<string> CreateBackupAsync(string sourcePath, string applicationName, string version)
    {
        try
        {
            // If source path doesn't exist, this is the first deployment - skip backup
            if (!Directory.Exists(sourcePath))
            {
                _logger.LogInformation("Source path does not exist (first deployment), skipping backup: {SourcePath}", sourcePath);
                return string.Empty; // Return empty string to indicate no backup was created
            }

            // Check if source directory is empty
            if (!Directory.EnumerateFileSystemEntries(sourcePath).Any())
            {
                _logger.LogInformation("Source path is empty (first deployment), skipping backup: {SourcePath}", sourcePath);
                return string.Empty;
            }

            // Sanitize application name and version for file system
            var safeAppName = SanitizeFileName(applicationName);
            var safeVersion = SanitizeFileName(version);
            
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"{safeAppName}_{safeVersion}_{timestamp}.zip";
            var backupPath = Path.Combine(_backupBasePath, safeAppName);
            
            // Create application-specific backup directory
            if (!Directory.Exists(backupPath))
            {
                Directory.CreateDirectory(backupPath);
            }

            var fullBackupPath = Path.Combine(backupPath, backupFileName);

            // Create ZIP backup
            await Task.Run(() => ZipFile.CreateFromDirectory(sourcePath, fullBackupPath, CompressionLevel.Fastest, false));

            _logger.LogInformation("Backup created successfully: {BackupPath}", fullBackupPath);
            return fullBackupPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup for {ApplicationName}", applicationName);
            throw;
        }
    }

    public async Task<bool> RestoreBackupAsync(string backupPath, string destinationPath)
    {
        try
        {
            if (!File.Exists(backupPath))
            {
                _logger.LogError("Backup file not found: {BackupPath}", backupPath);
                return false;
            }

            // Clear destination directory (except for certain files/folders)
            if (Directory.Exists(destinationPath))
            {
                await ClearDirectoryAsync(destinationPath);
            }
            else
            {
                Directory.CreateDirectory(destinationPath);
            }

            // Extract backup
            await Task.Run(() => ZipFile.ExtractToDirectory(backupPath, destinationPath));

            _logger.LogInformation("Backup restored successfully from {BackupPath} to {DestinationPath}", 
                backupPath, destinationPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore backup from {BackupPath}", backupPath);
            return false;
        }
    }

    public async Task<IEnumerable<BackupInfo>> GetBackupsAsync(string applicationName)
    {
        var backups = new List<BackupInfo>();
        var appBackupPath = Path.Combine(_backupBasePath, applicationName);

        if (!Directory.Exists(appBackupPath))
        {
            return backups;
        }

        await Task.Run(() =>
        {
            var backupFiles = Directory.GetFiles(appBackupPath, "*.zip");
            
            foreach (var file in backupFiles)
            {
                var fileInfo = new FileInfo(file);
                var fileName = Path.GetFileNameWithoutExtension(file);
                var parts = fileName.Split('_');

                backups.Add(new BackupInfo
                {
                    Path = file,
                    ApplicationName = applicationName,
                    Version = parts.Length > 1 ? parts[1] : "Unknown",
                    CreatedAt = fileInfo.CreationTimeUtc,
                    SizeBytes = fileInfo.Length
                });
            }
        });

        return backups.OrderByDescending(b => b.CreatedAt);
    }

    public async Task CleanupOldBackupsAsync(int retentionDays)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            var deletedCount = 0;

            await Task.Run(() =>
            {
                if (!Directory.Exists(_backupBasePath))
                {
                    return;
                }

                var appDirectories = Directory.GetDirectories(_backupBasePath);
                
                foreach (var appDir in appDirectories)
                {
                    var backupFiles = Directory.GetFiles(appDir, "*.zip");
                    
                    foreach (var file in backupFiles)
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTimeUtc < cutoffDate)
                        {
                            File.Delete(file);
                            deletedCount++;
                            _logger.LogInformation("Deleted old backup: {BackupFile}", file);
                        }
                    }
                }
            });

            _logger.LogInformation("Cleanup completed. Deleted {Count} old backups", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old backups");
        }
    }

    private async Task ClearDirectoryAsync(string path)
    {
        await Task.Run(() =>
        {
            var directory = new DirectoryInfo(path);
            
            foreach (var file in directory.GetFiles())
            {
                file.Delete();
            }
            
            foreach (var dir in directory.GetDirectories())
            {
                dir.Delete(true);
            }
        });
    }

    private string SanitizeFileName(string fileName)
    {
        // Remove invalid characters and trim whitespace
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName
            .Where(c => !invalidChars.Contains(c))
            .ToArray())
            .Trim();

        // Replace spaces with underscores for better compatibility
        sanitized = sanitized.Replace(' ', '_');

        // Ensure we have a valid name
        return string.IsNullOrWhiteSpace(sanitized) ? "backup" : sanitized;
    }
}
