namespace ReleaseFlow.Models;

public class Deployment
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public Application Application { get; set; } = null!;
    public string DeployedByUsername { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string ZipFileName { get; set; } = string.Empty;
    public long ZipFileSize { get; set; }
    public DeploymentStatus Status { get; set; } = DeploymentStatus.Pending;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? BackupPath { get; set; }
    public bool CanRollback { get; set; } = false;

    // Navigation properties
    public ICollection<DeploymentStep> Steps { get; set; } = new List<DeploymentStep>();
}

public enum DeploymentStatus
{
    Pending,
    InProgress,
    Succeeded,
    Failed,
    FailedRolledBack,
    RolledBack
}
