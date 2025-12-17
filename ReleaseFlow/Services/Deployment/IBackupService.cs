namespace ReleaseFlow.Services.Deployment;

public interface IBackupService
{
    Task<string> CreateBackupAsync(string sourcePath, string applicationName, string version);
    Task<bool> RestoreBackupAsync(string backupPath, string destinationPath);
    Task<IEnumerable<BackupInfo>> GetBackupsAsync(string applicationName);
    Task CleanupOldBackupsAsync(int retentionDays);
}

public class BackupInfo
{
    public string Path { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long SizeBytes { get; set; }
}
