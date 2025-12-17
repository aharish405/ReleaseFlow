namespace ReleaseFlow.Models;

public class AppSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEncrypted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public static class SettingKeys
{
    public const string DeploymentBasePath = "DeploymentBasePath";
    public const string BackupBasePath = "BackupBasePath";
    public const string BackupRetentionDays = "BackupRetentionDays";
    public const string MaxUploadSizeMB = "MaxUploadSizeMB";
    public const string HealthCheckTimeoutSeconds = "HealthCheckTimeoutSeconds";
}
